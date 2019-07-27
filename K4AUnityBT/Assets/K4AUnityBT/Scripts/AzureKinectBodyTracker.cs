using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace AzureKinect.Unity.BodyTracker
{
    public static class AzureKinectBodyTracker
    {
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


        [DllImport("K4AUnityBTPlugin")]
        private static extern bool K4ABT_GetSkeleton(IntPtr buffer, uint bufferSize);
        public static Vector3[] GetSkeleton()
        {
            var result = new Vector3[25];
            if (IsValidPlatform())
            {
                var buffer = new float[3 * 25];
                var allocatedMemory = Marshal.AllocHGlobal(sizeof(float) * buffer.Length);
                K4ABT_GetSkeleton(allocatedMemory, (uint)buffer.Length);
                Marshal.Copy(allocatedMemory, buffer, 0, buffer.Length);
                Marshal.FreeHGlobal(allocatedMemory);

                for (var i = 0; i < 25; i++)
                {
                    result[i] = new Vector3(buffer[i * 3 + 0], buffer[i * 3 + 1], buffer[i * 3 + 2]);
                }
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
