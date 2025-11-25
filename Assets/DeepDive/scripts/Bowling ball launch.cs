using UnityEngine;
using UnityEngine.InputSystem; // New Input System namespace

public class Bowlingballlaunch : MonoBehaviour
{
    [Header("Velocity Settings")]
    public Vector3 direction = Vector3.forward;
    public float force = 10f;

    [Header("Input Settings")]
    public Key launchKey = Key.Q; // Uses the new InputSystem Key enum

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Uses the new Input System API
        if (Keyboard.current[launchKey].wasPressedThisFrame)
        {
            ApplyVelocity();
        }
    }

    public void ApplyVelocity()
    {
        Vector3 velocity = direction.normalized * force;
        rb.linearVelocity = velocity;
    }
}
