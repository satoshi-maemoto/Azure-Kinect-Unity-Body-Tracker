using AzureKinect.Unity.BodyTracker;
using System.Collections.Generic;
using UnityEngine;

public class BodyVisualizer : MonoBehaviour
{
    public int bodyIndex = 0;
    public Material[] jointMaterials;
    public GameObject jointPrefab;
    public GameObject colorQuad;
    public GameObject calibratedJointPrefab;
    public bool IsActive { get; private set; } = true;
    public bool IsMirror = true;

    private IList<Renderer> jointRenderers;
    //private IList<Renderer> calibratedJointRenderers;

    void Start()
    {
        this.jointRenderers = new List<Renderer>();
        //this.calibratedJointRenderers = new List<Renderer>();
        for (var i = 0; i < (int)JointIndex.EarRight; i++)
        {
            var jointObject = GameObject.Instantiate(this.jointPrefab, Vector3.zero, Quaternion.identity, this.transform);
            var jointRenderer = jointObject.GetComponent<Renderer>();
            jointRenderer.material = this.jointMaterials[this.bodyIndex];
            this.jointRenderers.Add(jointRenderer);

            //var calibratedJointObject = GameObject.Instantiate(this.calibratedJointPrefab, Vector3.zero, Quaternion.identity, this.colorQuad.transform);
            //var calibratedJointRenderer = calibratedJointObject.GetComponent<Renderer>();
            //calibratedJointRenderer.material = this.jointMaterials[this.bodyIndex];
            //this.calibratedJointRenderers.Add(calibratedJointRenderer);
        }
    }

    public void Apply(Body body, int bodyIndex)
    {
        if (this.jointRenderers == null)
        {
            return;
        }

        var isActive = body.IsActive && (bodyIndex == this.bodyIndex);
        if (isActive != this.IsActive)
        {
            foreach (var renderer in this.jointRenderers)
            {
                renderer.enabled = isActive;
            }
            //foreach (var renderer in this.calibratedJointRenderers)
            //{
            //    renderer.enabled = isActive;
            //}
        }
        this.IsActive = isActive;

        if (this.IsActive)
        {
            for (var i = 0; i < this.jointRenderers.Count; i++)
            {
                var jointPosition = body.body.skeleton.joints[i].position;
                this.jointRenderers[i].transform.localPosition = new Vector3(jointPosition.x * (this.IsMirror ? -1 : 1), jointPosition.y * -1, jointPosition.z) / 1000f;

                //var calibratedJointPosition = body.calibratedJointPoints[i];
                //this.calibratedJointRenderers[i].transform.localPosition =
                //   new Vector3(-0.5f + (calibratedJointPosition.x / 1920f) * (this.IsMirror ? 1 : -1), -0.5f + (calibratedJointPosition.y / 1080f), -0.01f);
            }
        }
    }
}
