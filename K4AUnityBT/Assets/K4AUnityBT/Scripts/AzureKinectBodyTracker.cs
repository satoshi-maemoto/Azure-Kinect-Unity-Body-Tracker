using System;
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
        SholderLeft,
        ElbowLeft,
        WristLeft,
        ClavicleRight,
        SholderRight,
        ElbowRight,
        WristRight,
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

    [StructLayout(LayoutKind.Sequential)]
    public struct Joint
    {
        public Vector3 position;
        public Quaternion orientation;
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
    };

    public static class AzureKinectBodyTracker
    {
        public const int MaxBody = 6;
        public static int BodyBufferSize = Marshal.SizeOf(typeof(Body));

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
            throw new K4ABTException("This plugin spport Windows x86_64 environment only.");
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
        private static extern bool K4ABT_Start(uint depthTextureId, uint colorTextureId, uint transformedDepthTextureId);
        public static void Start(uint depthTextureId, uint colorTextureId, uint transformedDepthTextureId)
        {
            if (IsValidPlatform())
            {
                if (!K4ABT_Start(depthTextureId, colorTextureId, transformedDepthTextureId))
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
                SetDebugLogCallback(IntPtr.Zero);
                if (!K4ABT_End())
                {
                    throw new K4ABTException(GetLastErrorMessage());
                }
            }
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void BodyRecognizedDelegate(IntPtr buffer, uint bufferSize);

        [DllImport("K4AUnityBTPlugin")]
        private static extern void K4ABT_SetBodyRecognizedCallback(IntPtr callback);
        public static void SetBodyRecognizedCallback(IntPtr callback)
        {
            if (IsValidPlatform())
            {
                K4ABT_SetBodyRecognizedCallback(callback);
            }
        }

        public static void DefaultBodyRecognizedCallback(IntPtr buffer, uint bufferSize)
        {
            var result = new Body[MaxBody];
            if (IsValidPlatform())
            {
                var p = buffer;
                for (int i = 0; i < MaxBody; i++)
                {
                    result[i] = (Body)Marshal.PtrToStructure(p, typeof(Body));
                    p += BodyBufferSize;
                }
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
