using UnityEngine;
using System;

public class Gutterbal : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Where the ball should respawn after falling into the gutter.")]
    public Transform respawnPoint;

    [Header("Ball Settings")]
    [Tooltip("The Rigidbody of the bowling ball.")]
    public Rigidbody ballRb;

    [Header("Gutter Settings")]
    [Tooltip("Tag the gutter colliders as 'Gutter' in Unity.")]
    public string gutterTag = "Gutter";

    // Event for score system: returns 0 pins.
    public static event Action<int> OnBallGuttered;

    private void ResetBall()
    {
        // Stop physics movement
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        // Teleport ball
        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;

        // Notify score system (0 pins)
        OnBallGuttered?.Invoke(0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(gutterTag))
        {
            ResetBall();
        }
    }
}
