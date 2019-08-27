using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace AzureKinect.Unity.BodyTracker.Sample
{
    public class Controller : MonoBehaviour
    {
        public BodyVisualizer[] bodyVisualizers;
        public Material depthMaterial;
        public Material colorMaterial;
        public Material transformedDepthMaterial;

        private Texture2D depthTexture;
        private Texture2D colorTexture;
        private Texture2D transformedDepthTexture;
        private CommandBuffer commandBuffer;
        private Body[] bodies = new Body[AzureKinectBodyTracker.MaxBody];


        static void PluginDebugLogCallBack(string message)
        {
            Debug.Log("K4ABT : " + message);
        }

        void Start()
        {
            Debug.Log($"Body Size : {AzureKinectBodyTracker.BodyBufferSize}");

            this.commandBuffer = new CommandBuffer();
            this.commandBuffer.name = "AzureKinectImagesUpdeate";

            var debugDelegate = new AzureKinectBodyTracker.DebugLogDelegate(PluginDebugLogCallBack);
            var debagCallback = Marshal.GetFunctionPointerForDelegate(debugDelegate);
            AzureKinectBodyTracker.SetDebugLogCallback(debagCallback);

            var bodyRecognizedDelegate = new AzureKinectBodyTracker.BodyRecognizedDelegate(this.BodyRecognizedCallback);
            var bodyRecognizedCallback = Marshal.GetFunctionPointerForDelegate(bodyRecognizedDelegate);
            AzureKinectBodyTracker.SetBodyRecognizedCallback(bodyRecognizedCallback);

            this.StartCoroutine(this.Process());
        }

        [DllImport("kernel32.dll")]
        static extern void CopyMemory(IntPtr dst, IntPtr src, int size);

        private void BodyRecognizedCallback(IntPtr buffer, uint bufferSize)
        {
            //Debug.Log($"bufferSize : {bufferSize}");
            //Body[] tmpBodies = new Body[AzureKinectBodyTracker.MaxBody];
            //GCHandle gch = GCHandle.Alloc(tmpBodies, GCHandleType.Pinned);
            //CopyMemory(gch.AddrOfPinnedObject(), buffer, (int)bufferSize);
            //this.bodies = (Body[])gch.Target;
            //gch.Free();

            //ar buffer_ptr = Marshal.AllocHGlobal(totalSize);
            var p = buffer;
            for (int i = 0; i < AzureKinectBodyTracker.MaxBody; i++)
            {
                if (p != IntPtr.Zero)
                {
                    this.bodies[i] = Marshal.PtrToStructure<Body>(p);
                }
                p += AzureKinectBodyTracker.BodyBufferSize;
            }
        }

        private IEnumerator Process()
        {
            var depthTextureId = 1u;
            this.depthTexture = new Texture2D(640, 576, TextureFormat.R16, false);
            this.depthMaterial.mainTexture = this.depthTexture;
            var colorTextureId = 2u;
            this.colorTexture = new Texture2D(1920, 1080, TextureFormat.BGRA32, false);
            this.colorMaterial.mainTexture = this.colorTexture;
            var transformedDepthTextureId = 3u;
            this.transformedDepthTexture = new Texture2D(1920, 1080, TextureFormat.R16, false);
            this.transformedDepthMaterial.mainTexture = this.transformedDepthTexture;

            var callback = AzureKinectBodyTracker.GetTextureUpdateCallback();
            this.commandBuffer.IssuePluginCustomTextureUpdateV2(callback, this.depthTexture, depthTextureId);
            this.commandBuffer.IssuePluginCustomTextureUpdateV2(callback, this.colorTexture, colorTextureId);
            this.commandBuffer.IssuePluginCustomTextureUpdateV2(callback, this.transformedDepthTexture, transformedDepthTextureId);

            AzureKinectBodyTracker.Start(depthTextureId, colorTextureId, transformedDepthTextureId);
            //var startedTime = Time.realtimeSinceStartup;
            while (true)
            {
                for (var i = 0; i < this.bodies.Length; i++)
                {
                    this.bodyVisualizers[i].Apply(bodies[i], i);
                }

                Graphics.ExecuteCommandBuffer(this.commandBuffer);
                //Debug.Log($"Update Timestamp: {(Time.realtimeSinceStartup - startedTime)}");

                yield return null;
            }
        }

        private void OnApplicationQuit()
        {
            this.StopAllCoroutines();
            AzureKinectBodyTracker.End();
        }
    }
}