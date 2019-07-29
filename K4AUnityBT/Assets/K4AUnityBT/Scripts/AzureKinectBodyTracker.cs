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
    public struct Body
    {
        public uint id;
        public Skeleton skeleton;

        public bool IsActive
        {
            get
            {
                return ((this.id > 0) && (this.skeleton.joints != null) && (this.skeleton.joints.Length > 0));
            }
        }
    };

    public static class AzureKinectBodyTracker
    {
        public const int MaxBody = 6;

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
        public delegate void DebugDelegate(string message);

        [DllImport("K4AUnityBTPlugin")]
        private static extern void K4ABT_SetDebugFunction(IntPtr fp);
        public static void SetDebugFunction(IntPtr fp)
        {
            if (IsValidPlatform())
            {
                K4ABT_SetDebugFunction(fp);
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
                if (!K4ABT_End())
                {
                    throw new K4ABTException(GetLastErrorMessage());
                }
            }
        }

        private static int bodyBufferSize = Marshal.SizeOf(typeof(Body));
        private static int bodiesBufferSize = bodyBufferSize * MaxBody;

        [DllImport("K4AUnityBTPlugin")]
        private static extern bool K4ABT_GetBodies(IntPtr buffer, uint bufferSize);
        public static Body[] GetBodies()
        {
            var result = new Body[MaxBody]
                {
                new Body() { skeleton = new Skeleton() { joints = new Joint[(int)JointIndex.EarRight + 1] } },
                new Body() { skeleton = new Skeleton() { joints = new Joint[(int)JointIndex.EarRight + 1] } },
                new Body() { skeleton = new Skeleton() { joints = new Joint[(int)JointIndex.EarRight + 1] } },
                new Body() { skeleton = new Skeleton() { joints = new Joint[(int)JointIndex.EarRight + 1] } },
                new Body() { skeleton = new Skeleton() { joints = new Joint[(int)JointIndex.EarRight + 1] } },
                new Body() { skeleton = new Skeleton() { joints = new Joint[(int)JointIndex.EarRight + 1] } },
            };
            if (IsValidPlatform())
            {
                var allocatedMemory = Marshal.AllocHGlobal(bodiesBufferSize);
                K4ABT_GetBodies(allocatedMemory, MaxBody);
                var p = allocatedMemory;
                for (int i = 0; i < MaxBody; i++)
                {
                    result[i] = (Body)Marshal.PtrToStructure(p, typeof(Body));
                    p += bodyBufferSize;
                }
                Marshal.FreeHGlobal(allocatedMemory);
            }
            return result;
        }
    }

    public class K4ABTException : Exception
    {
        public K4ABTException(string message) : base(message)
        {
        }
    }
}
