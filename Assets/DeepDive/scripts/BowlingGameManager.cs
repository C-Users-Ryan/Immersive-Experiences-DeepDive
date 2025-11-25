using UnityEngine;

public class BowlingGameManager : MonoBehaviour
{
    public static BowlingGameManager Instance;

    [Header("Ball Setup")]
    public Rigidbody ballRb;
    public Transform ballSpawnPoint;
    public float minThrowVelocity = 0.4f;   // When the ball is considered “thrown”

    [Header("Shot Logic")]
    public bool shotInProgress = false;
    public bool ballThrown = false;

    [Header("Failsafe")]
    public float ballStopThreshold = 0.2f;  // If ball slows below this, end shot
    public float stuckTime = 2f;
    private float stuckTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ResetBall();
    }

    private void Update()
    {
        if (!ballRb) return;

        // Detect when the ball is thrown
        if (!ballThrown && ballRb.linearVelocity.magnitude > minThrowVelocity)
        {
            StartShot();
        }

        // Failsafe: ball gets stuck mid-lane
        if (shotInProgress && ballThrown)
        {
            if (ballRb.linearVelocity.magnitude < ballStopThreshold)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= stuckTime)
                {
                    Debug.Log("Ball stuck → ending shot.");
                    CompleteShot(0); // assume no pins for now
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
    }

    // -------------------------------
    // Shot Flow
    // -------------------------------

    public void StartShot()
    {
        shotInProgress = true;
        ballThrown = true;
        stuckTimer = 0f;

        Debug.Log("SHOT STARTED.");
    }

    public void CompleteShot(int pinsHit)
    {
        Debug.Log("SHOT COMPLETE → Pins Hit: " + pinsHit);

        shotInProgress = false;
        ballThrown = false;
        stuckTimer = 0f;

        // Later this will feed the full scoring system:
        HandleScoring(pinsHit);

        ResetBall();
    }

    // -------------------------------
    // Reset Ball
    // -------------------------------

    public void ResetBall()
    {
        Debug.Log("Resetting ball...");

        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        ballRb.transform.position = ballSpawnPoint.position;
        ballRb.transform.rotation = ballSpawnPoint.rotation;

        shotInProgress = false;
        ballThrown = false;
    }

    // -------------------------------
    // TEMP Scoring Stub
    // -------------------------------

    private void HandleScoring(int pinsHit)
    {
        // Placeholder for future scoring system
        Debug.Log("[SCORING] Logged pins: " + pinsHit);
    }

    // -------------------------------
    // External Lane Events
    // Called by triggers on the lane
    // -------------------------------

    public void OnBallEnteredGutter()
    {
        if (!shotInProgress) return;

        Debug.Log("Ball entered GUTTER.");
        CompleteShot(0); // gutter = zero pins
    }

    public void OnBallReachedLaneEnd()
    {
        if (!shotInProgress) return;

        Debug.Log("Ball reached end of lane.");
        CompleteShot(0); // For v1 we always return 0 (no pin system yet)
    }
}
