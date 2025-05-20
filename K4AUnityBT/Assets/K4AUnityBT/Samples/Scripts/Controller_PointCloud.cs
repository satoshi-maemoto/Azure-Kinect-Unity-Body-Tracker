using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace AzureKinect.Unity.BodyTracker.Sample
{
    public class Controller_PointCloud : MonoBehaviour
    {
        public Text bodyFps;
        public GameObject pointCloud;
        public Material[] pointCloudMaterials;

        private Texture2D depthTexture;
        private Texture2D colorTexture;
        private Texture2D transformedDepthTexture;
        private SynchronizationContext syncContext;
        private int frameCount = 0;
        private float fpsMeasured = 0f;
        private bool isRunning = false;
        private Action processCompleted;
        private DepthMode currentDepthMode = DepthMode.NFovUnbinned;
        private Mesh mesh = null;
        private int pointCloudMaterialIndex = 0;

        private static Controller_PointCloud self;

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

        private void DepthImageToPointCloudCallback(IntPtr buffer, int size)
        {
            byte[] managedBuffer = new byte[size];
            Marshal.Copy(buffer, managedBuffer, 0, size);
            const int STRIDE = 6;
            const float SCALAR = 1f;
            Vector3[] vertices = new Vector3[size / STRIDE];
            int index = 0;
            for (int i = 0; i < size; i += STRIDE)
            {
                if (index < size)
                {
                    vertices[index] = new Vector3(BitConverter.ToInt16(managedBuffer, i) * -SCALAR,
                        BitConverter.ToInt16(managedBuffer, i + 2) * -SCALAR, BitConverter.ToInt16(managedBuffer, i + 4) * SCALAR);
                    index++;
                }
            }

            self.syncContext.Post((s) =>
            {
                if (!self.isRunning)
                {
                    return;
                }
                else
                {
                    self.frameCount++;

                    if (self.mesh != null)
                    {
                        self.mesh.vertices = vertices;
                        self.mesh.RecalculateBounds();
                    }
                    else
                    {
                        self.InitMesh(vertices);
                    }
                }
            }, null);
        }

        private void ColorImageToDepthSpaceCallback(IntPtr buffer, int size)
        {
            byte[] managedBuffer = new byte[size];
            Marshal.Copy(buffer, managedBuffer, 0, size);
            const int STRIDE = 4;
            Color32[] colors = new Color32[size / STRIDE];

            int index = 0;
            for (int i = 0; i < size; i += STRIDE)
            {
                colors[index] = new Color32(managedBuffer[i + 2], managedBuffer[i + 1], managedBuffer[i], managedBuffer[i + 3]);
                index++;
            }

            self.syncContext.Post((s) =>
            {
                if (!self.isRunning)
                {
                    return;
                }
                else
                {
                    if (self.mesh != null)
                    {
                        self.mesh.colors32 = colors;
                    }
                }
            }, null);
        }

        private void InitMesh(Vector3[] vertices)
        {
            this.mesh = new Mesh();
            this.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            int[] indices = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                indices[i] = i;
            }
            this.mesh.vertices = vertices;
            this.mesh.SetIndices(indices, MeshTopology.Points, 0);
            this.pointCloud.GetComponent<MeshFilter>().mesh = this.mesh;
        }

        private IEnumerator Process(DepthMode depthMode, bool cpuOnly)
        {
            var debugDelegate = new AzureKinectBodyTracker.DebugLogDelegate(PluginDebugLogCallBack);
            var debagCallback = Marshal.GetFunctionPointerForDelegate(debugDelegate);
            AzureKinectBodyTracker.SetDebugLogCallback(debagCallback);

            try
            {
                AzureKinectBodyTracker.Start(0, 0, 0, depthMode, cpuOnly);
                this.currentDepthMode = depthMode;
            }
            catch (K4ABTException)
            {
                this.ProcessFinallize(false);
                yield break;
            }

            var depthImageToPointCloudDelegate = new AzureKinectBodyTracker.DepthImageToPointCloudDelegate(this.DepthImageToPointCloudCallback);
            var depthImageToPointCloudCallback = Marshal.GetFunctionPointerForDelegate(depthImageToPointCloudDelegate);
            AzureKinectBodyTracker.SetDepthImageToPointCloudCallback(depthImageToPointCloudCallback);

            var colorImageToDepthSpaceDelegate = new AzureKinectBodyTracker.ColorImageToDepthSpaceDelegate(this.ColorImageToDepthSpaceCallback);
            var colorImageToDepthSpaceCallback = Marshal.GetFunctionPointerForDelegate(colorImageToDepthSpaceDelegate);
            AzureKinectBodyTracker.SetColorImageToDepthSpaceCallback(colorImageToDepthSpaceCallback);

            this.isRunning = true;
            while (this.isRunning)
            {
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
                var fps = this.frameCount / this.fpsMeasured;
                this.bodyFps.text = $"FPS : {fps}";
                this.fpsMeasured = 0;
                this.frameCount = 0;
            }
        }

        public void DepthModeChanged(int index)
        {
            this.processCompleted = () =>
            {
                Debug.Log($"ProcessCompleted -> Start({(DepthMode)index})");
                this.StartCoroutine(this.Process((DepthMode)index, false));
            };
            this.StopProcess();
        }

        public void SwitchMaterial()
        {
            this.pointCloudMaterialIndex = (this.pointCloudMaterialIndex == 0) ? 1 : 0;
            this.pointCloud.GetComponent<Renderer>().material = this.pointCloudMaterials[this.pointCloudMaterialIndex];
        }
    }
}
