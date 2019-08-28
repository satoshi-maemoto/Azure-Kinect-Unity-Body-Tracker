using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AzureKinect.Unity.BodyTracker.Sample
{
    public class Controller : MonoBehaviour
    {
        public BodyVisualizer[] bodyVisualizers;
        public Material depthMaterial;
        public Material colorMaterial;
        public Material transformedDepthMaterial;
        public Text bodyFps;

        private Texture2D depthTexture;
        private Texture2D colorTexture;
        private Texture2D transformedDepthTexture;
        private CommandBuffer commandBuffer;
        private SynchronizationContext syncContext;
        private int bodyFrameCount = 0;
        private float fpsMeasured = 0f;


        private static Controller self;

        static void PluginDebugLogCallBack(string message)
        {
            Debug.Log("K4ABT : " + message);
        }

        void Start()
        {
            self = this;
            this.syncContext = SynchronizationContext.Current;

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

        private void BodyRecognizedCallback(int numBodies)
        {
            try
            {
                self.bodyFrameCount++;

                var bodies = AzureKinectBodyTracker.GetBody(numBodies);

                self.syncContext.Post((s) =>
                {
                    for (var i = 0; i < AzureKinectBodyTracker.MaxBody; i++)
                    {
                        self.bodyVisualizers[i].Apply((i < bodies.Length) ? bodies[i] : Body.Empty, i);
                    }
                }, null);
            }
            catch (Exception e)
            {
                Debug.Log($"{e.GetType().Name}\n{e.Message}\n{e.StackTrace}");
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
            while (true)
            {
                Graphics.ExecuteCommandBuffer(this.commandBuffer);
                yield return null;
            }
        }

        private void OnApplicationQuit()
        {
            this.StopAllCoroutines();
            AzureKinectBodyTracker.End();
        }

        private void Update()
        {
            this.fpsMeasured += Time.deltaTime;
            if (this.fpsMeasured >= 1.0f)
            {
                var fps = this.bodyFrameCount / this.fpsMeasured;
                this.bodyFps.text = $"Body FPS : {fps}";
                this.fpsMeasured = 0;
                this.bodyFrameCount = 0;
            }
        }

        public void CalibratedJointPointToggleValueChanged(bool value)
        {
            AzureKinectBodyTracker.SetCalibratedJointPointAvailability(value);
        }
    }
}