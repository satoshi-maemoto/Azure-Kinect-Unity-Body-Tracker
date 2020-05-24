using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public GameObject target;
    public Vector3 initialCameraPosition = new Vector3(0, 0, -1000);

    public float rotateSpeed = 10f;
    public float translateSpeed = 50f;
    public float scaleSpeed = 10f;

    private float scale = 1f;

    void Start()
    {
        Camera.main.transform.localPosition = this.initialCameraPosition;
        Camera.main.transform.localRotation = this.transform.rotation;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            this.transform.localPosition += new Vector3(
                Input.GetAxis("Mouse X") * -this.translateSpeed,
                Input.GetAxis("Mouse Y") * -this.translateSpeed, 
                0);
        }

        if (Input.GetMouseButton(1))
        {
            this.target.transform.eulerAngles += new Vector3(
                Input.GetAxis("Mouse Y") * this.rotateSpeed,
                Input.GetAxis("Mouse X") * -this.rotateSpeed,
                0);
        }

        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            this.scale += scroll * this.scaleSpeed;
            if (this.scale < 0)
            {
                this.scale = 0f;
            }
            this.target.transform.localScale = new Vector3(this.scale, this.scale, this.scale);
        }
    }
}
