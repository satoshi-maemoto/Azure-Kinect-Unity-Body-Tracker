using AzureKinect.Unity.BodyTracker;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BodyVisualizer : MonoBehaviour
{
    public int id = 1;
    public Material[] jointMaterials;
    public GameObject jointPrefab;
    public bool IsActive { get; private set; } = true;

    private IList<Renderer> jointRenderers;

    void Start()
    {
        this.jointRenderers = new List<Renderer>();
        for (var i = 0; i < (int)JointIndex.EarRight; i++)
        {
            var jointObject = GameObject.Instantiate(this.jointPrefab, Vector3.zero, Quaternion.identity, this.transform);
            var jointRenderer = jointObject.GetComponent<Renderer>();
            jointRenderer.material = this.jointMaterials[this.id - 1];
            this.jointRenderers.Add(jointRenderer);
        }
    }

    public void Apply(Body body)
    {
        if (this.jointRenderers == null)
        {
            return;
        }

        var isActive = body.IsActive && (body.id == this.id);
        if (isActive != this.IsActive)
        {
            foreach (var renderer in this.jointRenderers)
            {
                renderer.enabled = isActive;
            }
        }
        this.IsActive = isActive;

        if (this.IsActive)
        {
            for (var i = 0; i < this.jointRenderers.Count; i++)
            {
                this.jointRenderers[i].transform.localPosition = body.skeleton.joints[i].position / 1000f;
            }
        }
    }
}
