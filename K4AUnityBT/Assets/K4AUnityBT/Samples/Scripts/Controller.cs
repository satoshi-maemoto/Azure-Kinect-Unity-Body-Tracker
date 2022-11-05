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
        public ImuVisualizer imuVisualizer;
        public Toggle cpuOnly;
        public Dropdown modeDropdown;

        private Texture2D depthTexture;
        private Texture2D colorTexture;
        private Texture2D transformedDepthTexture;
        private SynchronizationContext syncContext;
        private int bodyFrameCount = 0;
        private float fpsMeasured = 0f;
        private bool isRunning = false;
        private Action processCompleted;
        private DepthMode currentDepthMode = DepthMode.NFovUnbinned;

        private static Controller self;

        static void PluginDebugLogCallBack(string message)
        {
            Debug.Log("K4ABT : " + message);
        }

        void Start()
        {
            self = this;
            this.syncContext = SynchronizationContext.Current;

            this.StartCoroutine(this.Process(DepthMode.NFovUnbinned, false));
        }

        private void BodyRecognizedCallback(UInt32 numBodies)
        {
            try
            {
                self.bodyFrameCount++;

                var bodies = AzureKinectBodyTracker.GetBody(numBodies);
                var imuData = AzureKinectBodyTracker.GetImuData();

                self.syncContext.Post((s) =>
                {
                    if (!self.isRunning)
                    {
                        return;
                    }
                    for (var i = 0; self.isRunning && (i < AzureKinectBodyTracker.MaxBody); i++)
                    {
                        self.bodyVisualizers[i].Apply((i < bodies.Length) ? bodies[i] : Body.Empty, i);
                    }
                    self.imuVisualizer.Apply(imuData);
                }, null);
            }
            catch (Exception e)
            {
                Debug.Log($"{e.GetType().Name}\n{e.Message}\n{e.StackTrace}");
            }
        }

        private AzureKinectBodyTracker.DebugLogDelegate debugDelegate = null;
        private AzureKinectBodyTracker.BodyRecognizedDelegate bodyRecognizedDelegate = null;

        private IEnumerator Process(DepthMode depthMode, bool cpuOnly)
        {
            this.debugDelegate = new AzureKinectBodyTracker.DebugLogDelegate(PluginDebugLogCallBack);
            var debagCallback = Marshal.GetFunctionPointerForDelegate(debugDelegate);
            AzureKinectBodyTracker.SetDebugLogCallback(debagCallback);

            this.bodyRecognizedDelegate = new AzureKinectBodyTracker.BodyRecognizedDelegate(this.BodyRecognizedCallback);
            var bodyRecognizedCallback = Marshal.GetFunctionPointerForDelegate(bodyRecognizedDelegate);
            AzureKinectBodyTracker.SetBodyRecognizedCallback(bodyRecognizedCallback);

            var depthTextureId = 1u;
            var depthWidth = (int)AzureKinectBodyTracker.DepthResolutions[depthMode].x;
            var depthHeight = (int)AzureKinectBodyTracker.DepthResolutions[depthMode].y;
            this.depthTexture = new Texture2D((depthWidth > 0) ? depthWidth : 1, (depthHeight > 0) ? depthHeight : 1, TextureFormat.R16, false);
            this.depthMaterial.mainTexture = this.depthTexture;
            var colorTextureId = 2u;
            this.colorTexture = new Texture2D(1920, 1080, TextureFormat.BGRA32, false);
            this.colorMaterial.mainTexture = this.colorTexture;
            var transformedDepthTextureId = 3u;
            this.transformedDepthTexture = new Texture2D(1920, 1080, TextureFormat.R16, false);
            this.transformedDepthMaterial.mainTexture = this.transformedDepthTexture;

            var callback = AzureKinectBodyTracker.GetTextureUpdateCallback();
            var commandBuffer = new CommandBuffer();
            commandBuffer.name = "AzureKinectImagesUpdeate";
            commandBuffer.IssuePluginCustomTextureUpdateV2(callback, this.depthTexture, depthTextureId);
            commandBuffer.IssuePluginCustomTextureUpdateV2(callback, this.colorTexture, colorTextureId);
            commandBuffer.IssuePluginCustomTextureUpdateV2(callback, this.transformedDepthTexture, transformedDepthTextureId);

            try
            {
                AzureKinectBodyTracker.Start(depthTextureId, colorTextureId, transformedDepthTextureId, depthMode, cpuOnly);
                this.currentDepthMode = depthMode;
            }
            catch (K4ABTException)
            {
                this.ProcessFinallize(false);
                yield break;
            }
            this.isRunning = true;
            while (this.isRunning)
            {
                Graphics.ExecuteCommandBuffer(commandBuffer);
                yield return null;
            }
            AzureKinectBodyTracker.End();
            this.ProcessFinallize();
        }

        private void ProcessFinallize(bool invokeCompletedAction = true)
        {
            if (this.depthTexture != null)
            {
                Destroy(this.depthTexture);
            }
            if (this.colorTexture != null)
            {
                Destroy(this.colorTexture);
            }
            if (this.transformedDepthTexture != null)
            {
                Destroy(this.transformedDepthTexture);
            }
            if (invokeCompletedAction)
            {
                this.processCompleted?.Invoke();
            }
            this.processCompleted = null;
        }

        private void StopProcess()
        {
            if (this.isRunning)
            {
                this.isRunning = false;
            }
            else
            {
                this.processCompleted?.Invoke();
                this.processCompleted = null;
            }
        }

        private void OnApplicationQuit()
        {
            this.StopProcess();
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

        public void DepthModeChanged(int index)
        {
            this.processCompleted = () =>
            {
                Debug.Log($"ProcessCompleted -> Start({(DepthMode)index}, CPU Only={this.cpuOnly.isOn})");
                this.StartCoroutine(this.Process((DepthMode)index, this.cpuOnly.isOn));
            };
            this.StopProcess();
        }

        public void CPUOnlyChanged(bool value)
        {
            this.processCompleted = () =>
            {
                Debug.Log($"ProcessCompleted -> Start({this.currentDepthMode}, CPU Only={value})");
                this.StartCoroutine(this.Process(this.currentDepthMode, value));
            };
            this.StopProcess();
        }
    }
}