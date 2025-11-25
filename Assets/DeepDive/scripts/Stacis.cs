using UnityEngine;

public class Stacis : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero; // optional: freeze movement
            Debug.Log(other.name + " entered gravity zone → Gravity OFF");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            rb.useGravity = true;
            Debug.Log(other.name + " exited gravity zone → Gravity ON");
        }
    }
}
