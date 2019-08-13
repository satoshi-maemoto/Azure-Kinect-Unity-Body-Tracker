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

        static void PluginDebugCallBack(string message)
        {
            Debug.Log("K4ABT : " + message);
        }

        void Start()
        {
            this.commandBuffer = new CommandBuffer();
            this.commandBuffer.name = "AzureKinectImagesUpdeate";

            var debugDelegate = new AzureKinectBodyTracker.DebugLogDelegate(PluginDebugCallBack);
            var functionPointer = Marshal.GetFunctionPointerForDelegate(debugDelegate);
            AzureKinectBodyTracker.SetDebugLogFunction(functionPointer);

            this.StartCoroutine(this.Process());
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
            var startedTime = Time.realtimeSinceStartup;
            while (true)
            {
                var bodies = AzureKinectBodyTracker.GetBodies();
                for (var i = 0; i < bodies.Length; i++)
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