using UnityEngine;

public class PinController : MonoBehaviour
{
    public bool isDown = false;
    public float fallAngle = 45f;

    private void Update()
    {
        if (isDown) return;

        float tilt = Vector3.Angle(transform.up, Vector3.up);

        if (tilt > fallAngle)
        {
            isDown = true;
        }
    }

    public void ResetPin(Transform resetTransform)
    {
        isDown = false;
        transform.SetPositionAndRotation(resetTransform.position, resetTransform.rotation);
    }
}
