using AzureKinect.Unity.BodyTracker;
using UnityEngine;
using UnityEngine.UI;

public class ImuVisualizer : MonoBehaviour
{
    public enum Targets
    {
        Acceleration,
        Gyro,
    }
    public GameObject deviceModel;
    public Text imuDataText;
    public Targets target = Targets.Gyro;

    private static Vector3 Gravity = new Vector3(0f, 0f, -9.8f);

    public void Apply(ImuSample value)
    {
        var accValue = value.accSample;
        var gyroValue = value.gyroSample;
        this.imuDataText.text = $"IMU: TMP={value.temperature:0.000} " +
            $"ACC=({accValue.x:0.000},{accValue.y:0.000},{accValue.z:0.000}) " +
            $"GYRO=({gyroValue.x:0.00},{gyroValue.y:0.00},{gyroValue.z:0.00})";

        var direction = new Vector3(-accValue.x, accValue.y, -accValue.z) - Gravity;
        var accRotation = Quaternion.FromToRotation(-this.transform.right, direction);

        gyroValue = value.integralGyro * Mathf.Rad2Deg;
        var gyroRotation = Quaternion.Euler(gyroValue.x, -gyroValue.y, -gyroValue.z);

        this.deviceModel.transform.localRotation = Quaternion.identity;
        switch (this.target)
        {
            case Targets.Acceleration:
                this.deviceModel.transform.Rotate(accRotation.eulerAngles, Space.Self);
                break;
            case Targets.Gyro:
                this.deviceModel.transform.Rotate(gyroRotation.eulerAngles, Space.Self);
                break;
        }
    }
}
