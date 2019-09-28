using AzureKinect.Unity.BodyTracker;
using UnityEngine;
using UnityEngine.UI;

public class ImuVisualizer : MonoBehaviour
{
    public GameObject deviceModel;
    public Text imuDataText;

    private Vector3 integralGyro = Vector3.zero;
    private ulong prevGyroTimestampUsec = 0;

    public void Apply(ImuSample value)
    {
        if (this.prevGyroTimestampUsec == 0)
        {
            this.prevGyroTimestampUsec = value.gyroTimestampUsec;
            return;
        }

        var accValue = value.accSample;
        var gyroValue = value.gyroSample;
        this.imuDataText.text = $"IMU: TMP={value.temperature:0.000} " +
            $"ACC=({accValue.x:0.000},{accValue.y:0.000},{accValue.z:0.000}) " +
            $"GYRO=({gyroValue.x:0.00},{gyroValue.y:0.00},{gyroValue.z:0.00})";

        var direction = new Vector3(-accValue.x, accValue.y, -accValue.z);
        var accRotation = Quaternion.FromToRotation(-this.transform.up, direction);

        var timeDiff = (value.gyroTimestampUsec - this.prevGyroTimestampUsec) / 1000000f;
        this.integralGyro += gyroValue * timeDiff;
        gyroValue = this.integralGyro * Mathf.Rad2Deg;
        var gyroRotation = Quaternion.Euler(gyroValue.x, -gyroValue.y, -gyroValue.z);
        this.prevGyroTimestampUsec = value.gyroTimestampUsec;

        this.deviceModel.transform.localRotation = Quaternion.identity;
        //this.deviceModel.transform.Rotate(accRotation.eulerAngles, Space.Self);
        this.deviceModel.transform.Rotate(gyroRotation.eulerAngles, Space.Self);
    }
}
