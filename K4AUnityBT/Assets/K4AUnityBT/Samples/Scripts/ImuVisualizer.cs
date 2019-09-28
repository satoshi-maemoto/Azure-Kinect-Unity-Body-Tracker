using AzureKinect.Unity.BodyTracker;
using UnityEngine;
using UnityEngine.UI;

public class ImuVisualizer : MonoBehaviour
{
    public GameObject deviceModel;
    public Text imuDataText;

    public void Apply(ImuSample value)
    {
        var accValue = value.accSample;
        this.imuDataText.text = $"IMU: TMP={value.temperature:0.0000} " +
            $"ACC=({accValue.x:0.0000},{accValue.y:0.0000},{accValue.z:0.0000}) " +
            $"GYRO=({value.gyroSample.x:0.0000},{value.gyroSample.y:0.0000},{value.gyroSample.z:0.0000})";

        var direction = new Vector3(-accValue.x, accValue.y, -accValue.z);
        this.deviceModel.transform.localRotation = Quaternion.FromToRotation(-this.transform.up, direction);

    }
}
