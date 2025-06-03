using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using UnityEngine;

namespace AzureKinect.Unity.BodyTracker
{
    public enum JointIndex
    {
        Pelvis = 0,
        SpineNaval,
        SpineChest,
        Neck,
        ClavicleLeft,
        ShoulderLeft,
        ElbowLeft,
        WristLeft,
        HandLeft,
        HandTipLeft,
        ThumbLeft,
        ClavicleRight,
        ShoulderRight,
        ElbowRight,
        WristRight,
        HandRight,
        HandTipRight,
        ThumbRight,
        HipLeft,
        KneeLeft,
        AnkleLeft,
        FootLeft,
        HipRight,
        KneeRight,
        AnkleRight,
        FootRight,
        Head,
        Nose,
        EyeLeft,
        EarLeft,
        EyeRight,
        EarRight,
    };

    public enum DepthMode
    {
        Off = 0,
        NFov2X2Binned,
        NFovUnbinned,
        WFov2X2Binned,
        WFovUnbinned,
        PassiveIr,
    }

    public enum JointConfidenceLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct Joint
    {
        public Vector3 position;
        public Quaternion orientation;
        public JointConfidenceLevel confidenceLevel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Skeleton
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)JointIndex.EarRight + 1)]
        public Joint[] joints;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct RawBody
    {
        public uint id;
        public Skeleton skeleton;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct Body
    {
        public RawBody body;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)JointIndex.EarRight + 1)]
        public Vector2[] calibratedJointPoints;

        public bool IsActive
        {
            get
            {
                return ((this.body.id > 0) && (this.body.skeleton.joints != null) && (this.body.skeleton.joints.Length > 0));
            }
        }

        public static Body Empty = new Body();
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ImuSample
    {
        public float temperature;
        public Vector3 accSample;
        public ulong accTimestampUsec;
        public Vector3 gyroSample;
        public ulong gyroTimestampUsec;
        public Vector3 integralGyro;
    };

    public static class AzureKinectBodyTracker
    {
        public const int MaxBody = 6;

        public static readonly Dictionary<DepthMode, Vector2> DepthResolutions = new Dictionary<DepthMode, Vector2>()
        {
            {DepthMode.Off, new Vector2(0, 0)},
            {DepthMode.NFov2X2Binned, new Vector2(320, 288)},
            {DepthMode.NFovUnbinned, new Vector2(640, 576)},
            {DepthMode.WFov2X2Binned, new Vector2(512, 512)},
            {DepthMode.WFovUnbinned, new Vector2(1024, 1024)},
            {DepthMode.PassiveIr, new Vector2(1024, 1024)},
        };

        private static bool IsValidPlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return true;
                default:
                    break;
            }
            throw new K4ABTException("This plugin spports Windows x86_64 environment only.");
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugLogDelegate(string message);

        [DllImport("K4AUnityBTPlugin")]
        private static extern void K4ABT_SetDebugLogCallback(IntPtr callback);
        public static void SetDebugLogCallback(IntPtr callback)
        {
            if (IsValidPlatform())
            {
                K4ABT_SetDebugLogCallback(callback);
            }
        }

        [DllImport("K4AUnityBTPlugin")]
        public static extern IntPtr GetTextureUpdateCallback();

        [DllImport("K4AUnityBTPlugin", CharSet = CharSet.Unicode)]
        private static extern void K4ABT_GetLastErrorMessage(StringBuilder buffer, uint bufferSize);
        public static string GetLastErrorMessage()
        {
            if (IsValidPlatform())
            {
                StringBuilder buffer = new StringBuilder(512);
                K4ABT_GetLastErrorMessage(buffer, (uint)buffer.Capacity);
                return buffer.ToString();
            }
            return string.Empty;
        }

        [DllImport("K4AUnityBTPlugin")]
        private static extern bool K4ABT_Start(uint depthTextureId, uint colorTextureId, uint transformedDepthTextureId, int depthMode, bool cpuOnly);
        public static void Start(uint depthTextureId, uint colorTextureId, uint transformedDepthTextureId)
        {
            Start(depthTextureId, colorTextureId, transformedDepthTextureId, DepthMode.NFovUnbinned, false);
        }
        public static void Start(uint depthTextureId, uint colorTextureId, uint transformedDepthTextureId, DepthMode depthMode, bool cpuOnly)
        {
            if (IsValidPlatform())
            {
                if (!K4ABT_Start(depthTextureId, colorTextureId, transformedDepthTextureId, (int)depthMode, cpuOnly))
                {
                    throw new K4ABTException(GetLastErrorMessage());
                }
            }
        }

        [DllImport("K4AUnityBTPlugin")]
        private static extern bool K4ABT_End();
        public static void End()
        {
            if (IsValidPlatform())
            {
                SetBodyRecognizedCallback(IntPtr.Zero);
                if (!K4ABT_End())
                {
                    throw new K4ABTException(GetLastErrorMessage());
                }
                SetDebugLogCallback(IntPtr.Zero);
            }
        }

        private static int bodyBufferSize = Marshal.SizeOf(typeof(Body));

        [DllImport("K4AUnityBTPlugin")]
        private static extern bool K4ABT_GetBody(IntPtr buffer, UInt32 numBodies);
        public static Body[] GetBody(UInt32 numBodies)
        {
            var result = new Body[numBodies];
            if (numBodies == 0)
            {
                return result;
            }
            if (IsValidPlatform())
            {
                var allocatedMemory = Marshal.AllocHGlobal(bodyBufferSize * (int)numBodies);
                K4ABT_GetBody(allocatedMemory, numBodies);
                var p = allocatedMemory;
                for (int i = 0; i < numBodies; i++)
                {
                    result[i] = Marshal.PtrToStructure<Body>(p);
                    p += bodyBufferSize;
                }
                Marshal.FreeHGlobal(allocatedMemory);
            }
            return result;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void BodyRecognizedDelegate(UInt32 numBodies);

        [DllImport("K4AUnityBTPlugin")]
        private static extern void K4ABT_SetBodyRecognizedCallback(IntPtr callback);
        public static void SetBodyRecognizedCallback(IntPtr callback)
        {
            if (IsValidPlatform())
            {
                K4ABT_SetBodyRecognizedCallback(callback);
            }
        }

        [DllImport("K4AUnityBTPlugin")]
        private static extern void K4ABT_SetCalibratedJointPointAvailability(bool availability);
        public static void SetCalibratedJointPointAvailability(bool availability)
        {
            if (IsValidPlatform())
            {
                K4ABT_SetCalibratedJointPointAvailability(availability);
            }
        }

        private static int imuBufferSize = Marshal.SizeOf(typeof(ImuSample));

        [DllImport("K4AUnityBTPlugin")]
        private static extern bool K4ABT_GetImuData(IntPtr buffer);
        public static ImuSample GetImuData()
        {
            var result = new ImuSample();
            if (IsValidPlatform())
            {
                var allocatedMemory = Marshal.AllocHGlobal(imuBufferSize);
                K4ABT_GetImuData(allocatedMemory);
                result = Marshal.PtrToStructure<ImuSample>(allocatedMemory);
                Marshal.FreeHGlobal(allocatedMemory);
            }
            return result;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DepthImageToPointCloudDelegate(IntPtr buffer, int size);

        [DllImport("K4AUnityBTPlugin")]
        private static extern void K4ABT_SetDepthImageToPointCloudCallback(IntPtr callback);

        public static void SetDepthImageToPointCloudCallback(IntPtr callback)
        {
            if (IsValidPlatform())
            {
                K4ABT_SetDepthImageToPointCloudCallback(callback);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ColorImageToDepthSpaceDelegate(IntPtr buffer, int size);

        [DllImport("K4AUnityBTPlugin")]
        private static extern void K4ABT_SetColorImageToDepthSpaceCallback(IntPtr callback);

        public static void SetColorImageToDepthSpaceCallback(IntPtr callback)
        {
            if (IsValidPlatform())
            {
                K4ABT_SetColorImageToDepthSpaceCallback(callback);
            }
        }
    }

    public class K4ABTException : Exception
    {
        public K4ABTException(string message) : base(message)
        {
        }
    }
}
