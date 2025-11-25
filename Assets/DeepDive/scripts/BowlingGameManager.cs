using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// BowlingGameManager - coordinates ball, pins and scoring.
/// Attach to a manager GameObject and assign references in the inspector.
/// </summary>

public class BowlingGameManager : MonoBehaviour
{
    [Header("References")]
    public PinManager pinManager;                 // assign PinManager
    public ScoringManager scoringManager;         // assign ScoringManager
    public Rigidbody ballRb;                      // the player's ball rigidbody
    public Transform ballSpawnTransform;          // where the ball should be reset to
    public float ballResetWaitAfterEnd = 1.0f;    // wait before resetting ball

    [Header("Shot detection thresholds")]
    public float shotStartSpeedThreshold = 1.0f;  // velocity to consider the shot started
    public float shotStopSpeedThreshold = 0.2f;   // velocity under which ball considered stopped
    public float ballStopTimeRequired = 1.0f;     // seconds below stop threshold to consider "stopped"

    [Header("Pin settle")]
    public float waitAfterShotBeforeCounting = 2.0f; // wait to let pins settle before counting
    public float maxWaitForPins = 8.0f;              // safety timeout

    [Header("Debug / Events")]
    public bool autoDetectShot = true;            // whether to auto-detect shot using ballRb
    public bool debugLogs = true;
    public UnityEvent onShotStarted;
    public UnityEvent onShotEnded;
    public UnityEvent onFrameCompleted;
    public UnityEvent onGameOver;

    // internal state
    private bool inShot = false;
    private Coroutine monitorShotCoroutine = null;
    private Coroutine pinSettleCoroutine = null;
    private int pinsDownAtStartOfRoll = 0;
    private int lastDetectedPins = 0;

    private void Start()
    {
        if (pinManager == null) Debug.LogWarning("PinManager not assigned on BowlingGameManager.");
        if (scoringManager == null) Debug.LogWarning("ScoringManager not assigned on BowlingGameManager.");
        if (ballRb == null) Debug.LogWarning("ballRb not assigned on BowlingGameManager.");

        // initialize lastDetectedPins
        if (pinManager != null) lastDetectedPins = pinManager.GetPinsDown();
    }

    private void Update()
    {
        if (!autoDetectShot || ballRb == null) return;

        // auto-detect shot start
        if (!inShot && ballRb.linearVelocity.magnitude >= shotStartSpeedThreshold)
        {
            StartShot();
        }

        // auto-detect shot end by ball stopping
        if (inShot && monitorShotCoroutine == null)
        {
            // start monitor that detects end by low speed for some time
            monitorShotCoroutine = StartCoroutine(MonitorBallStopForEnd());
        }
    }

    /// <summary>
    /// Public manual notification (for launchers or UI) that a shot has started.
    /// </summary>
    public void StartShot()
    {
        if (inShot) return;
        inShot = true;
        if (debugLogs) Debug.Log("[BowlingGameManager] Shot started.");
        onShotStarted?.Invoke();

        // record pins at start of roll
        pinsDownAtStartOfRoll = pinManager != null ? pinManager.GetPinsDown() : 0;

        // ensure any existing pin settle coroutine is stopped
        if (pinSettleCoroutine != null)
        {
            StopCoroutine(pinSettleCoroutine);
            pinSettleCoroutine = null;
        }
    }

    /// <summary>
    /// Ends the current shot, counting pins after they settle, registering roll and handling resets.
    /// Use this for gutter or lane-end events, or it will be called automatically when ball stops.
    /// </summary>
    /// <param name="reason">Optional debug reason</param>
    public void EndShot(string reason = "Auto")
    {
        if (!inShot)
        {
            if (debugLogs) Debug.Log("[BowlingGameManager] EndShot called but not in a shot.");
            return;
        }

        if (debugLogs) Debug.Log($"[BowlingGameManager] Shot ended. Reason: {reason}");
        inShot = false;
        monitorShotCoroutine = null;

        // start coroutine to wait for pins to settle then register roll and handle resets
        if (pinSettleCoroutine != null) StopCoroutine(pinSettleCoroutine);
        pinSettleCoroutine = StartCoroutine(WaitForPinsThenRegister());
        onShotEnded?.Invoke();
    }

    /// <summary>
    /// External trigger: ball entered gutter.
    /// </summary>
    public void OnBallEnteredGutter()
    {
        if (debugLogs) Debug.Log("[BowlingGameManager] Ball entered gutter.");
        EndShot("Gutter");
    }

    /// <summary>
    /// External trigger: ball left lane end (hit end / knocked away).
    /// </summary>
    public void OnBallLeftLaneEnd()
    {
        if (debugLogs) Debug.Log("[BowlingGameManager] Ball left lane end.");
        EndShot("LaneEnd");
    }

    /// <summary>
    /// Coroutine: monitor ball velocity and detect a sustained stop to end shot automatically.
    /// </summary>
    private IEnumerator MonitorBallStopForEnd()
    {
        float elapsed = 0f;
        while (inShot)
        {
            if (ballRb.linearVelocity.magnitude <= shotStopSpeedThreshold)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= ballStopTimeRequired)
                {
                    EndShot("BallStopped");
                    yield break;
                }
            }
            else
            {
                elapsed = 0f;
            }

            yield return null;
        }

        monitorShotCoroutine = null;
    }

    /// <summary>
    /// Waits for pins to settle (simple time-based approach) then registers the roll.
    /// </summary>
    private IEnumerator WaitForPinsThenRegister()
    {
        float waited = 0f;
        // initial small wait to allow immediate movement
        yield return new WaitForSeconds(waitAfterShotBeforeCounting);

        // Optionally, we could poll pin rigidbodies for motion to be below threshold; for simplicity use time-based with timeout.
        while (waited < maxWaitForPins)
        {
            // allow a few frames to pass for physics to settle
            yield return new WaitForSeconds(0.25f);
            waited += 0.25f;
            // a future improvement could inspect pin rigidbodies velocities here.
            // For now rely on wait and tilt detection present in PinController.
            break; // exit immediately after the minimal wait loop iteration
        }

        RegisterRollAndHandleResets();

        pinSettleCoroutine = null;
    }

    /// <summary>
    /// Determine how many pins were knocked this roll and register with scoring manager.
    /// Also performs appropriate pin/ball resets based on bowling rules (including 10th frame).
    /// </summary>
    private void RegisterRollAndHandleResets()
    {
        if (pinManager == null || scoringManager == null)
        {
            Debug.LogError("[BowlingGameManager] Missing PinManager or ScoringManager reference.");
            return;
        }

        int pinsNowDown = pinManager.GetPinsDown();
        int pinsKnockedThisRoll = pinsNowDown - pinsDownAtStartOfRoll;
        // clamp non-negative
        if (pinsKnockedThisRoll < 0) pinsKnockedThisRoll = 0;

        if (debugLogs) Debug.Log($"[BowlingGameManager] Pins at start: {pinsDownAtStartOfRoll}, now: {pinsNowDown}, knocked this roll: {pinsKnockedThisRoll}");

        // Register roll
        scoringManager.RegisterRoll(pinsKnockedThisRoll);

        // update lastDetectedPins for next roll
        lastDetectedPins = pinsNowDown;

        // handle resets & frame flow
        int frameIndex = scoringManager.GetCurrentFrameIndex();
        bool frameComplete = scoringManager.IsFrameComplete();
        bool gameOver = scoringManager.IsGameOver();

        if (debugLogs) Debug.Log($"[BowlingGameManager] Frame index: {frameIndex}, frameComplete: {frameComplete}, gameOver: {gameOver}");

        // 10th frame special handling
        if (frameIndex == 9)
        {
            // If the frame is complete and gameOver -> signal end
            if (frameComplete)
            {
                if (debugLogs) Debug.Log("[BowlingGameManager] Tenth frame complete.");
                onFrameCompleted?.Invoke();
                if (gameOver) onGameOver?.Invoke();
                StartCoroutine(ResetBallAfterDelay());
                return;
            }
            else
            {
                // Not complete: if strike on first roll, or spare achieved -> reset pins for bonus rolls
                Frame tenthFrame = scoringManager.frames[9];
                if (tenthFrame.rolls.Count == 1 && tenthFrame.rolls[0] == 10)
                {
                    // strike on first roll -> reset pins for next bonus roll
                    if (debugLogs) Debug.Log("[BowlingGameManager] Tenth frame strike: resetting pins for bonus roll.");
                    pinManager.ResetPins();
                }
                else if (tenthFrame.rolls.Count == 2 && (tenthFrame.rolls[0] + tenthFrame.rolls[1] == 10))
                {
                    // spare -> reset pins for bonus roll
                    if (debugLogs) Debug.Log("[BowlingGameManager] Tenth frame spare: resetting pins for bonus roll.");
                    pinManager.ResetPins();
                }
                StartCoroutine(ResetBallAfterDelay());
                return;
            }
        }

        // Non-10th frames
        // If first roll resulted in strike (10 pins knocked) and it's not 10th frame -> reset pins for next frame
        Frame currentFrame = scoringManager.frames[frameIndex];
        if (currentFrame.rolls.Count == 1 && currentFrame.rolls[0] == 10)
        {
            // Strike on first roll (except if it's 10th handled above)
            if (debugLogs) Debug.Log("[BowlingGameManager] Strike! Resetting pins for next frame.");
            pinManager.ResetPins();
            // advance ball reset
            StartCoroutine(ResetBallAfterDelay());
            // notify frame completed
            onFrameCompleted?.Invoke();
            return;
        }

        // If frame is complete after this roll -> reset pins for next frame
        if (frameComplete)
        {
            if (debugLogs) Debug.Log("[BowlingGameManager] Frame completed. Resetting pins for next frame.");
            pinManager.ResetPins();
            onFrameCompleted?.Invoke();
            StartCoroutine(ResetBallAfterDelay());
            return;
        }
        else
        {
            // Frame not complete -> prepare for second roll: do not reset pins (fallen ones remain down)
            if (debugLogs) Debug.Log("[BowlingGameManager] Prepare for second roll (keep fallen pins down).");
            pinManager.PrepareForSecondRoll();
            StartCoroutine(ResetBallAfterDelay());
            return;
        }
    }

    /// <summary>
    /// Reset ball to its spawn position after a short delay so the player can see the result.
    /// </summary>
    private IEnumerator ResetBallAfterDelay()
    {
        yield return new WaitForSeconds(ballResetWaitAfterEnd);

        ResetBallToSpawn();
    }

    /// <summary>
    /// Resets ball's transform/velocity to spawn.
    /// </summary>
    public void ResetBallToSpawn()
    {
        if (ballRb == null || ballSpawnTransform == null)
        {
            Debug.LogWarning("[BowlingGameManager] Missing ballRb or ballSpawnTransform for ResetBallToSpawn.");
            return;
        }

        // Reset physics
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        // Move ball to spawn
        ballRb.transform.SetPositionAndRotation(ballSpawnTransform.position, ballSpawnTransform.rotation);

        if (debugLogs) Debug.Log("[BowlingGameManager] Ball reset to spawn.");
    }

    /// <summary>
    /// Public helper to reset the entire lane (pins + ball) - useful for testing or new game.
    /// </summary>
    public void ResetLaneFull()
    {
        if (pinManager != null) pinManager.ResetPins();
        ResetBallToSpawn();
        // reset scoring
        if (scoringManager != null)
        {
            scoringManager.frames = new List<Frame>();
            for (int i = 0; i < 10; i++) scoringManager.frames.Add(new Frame());
            // reset scoringManager internal frame index via reflection alternative: best to add a Reset method to scoring manager in future.
            // As a practical approach, reload the scene or add a Reset() to ScoringManager.
            // We'll try to reset private currentFrame via property if available. If not available, user may reinitialize scoringManager in inspector.
        }
        if (debugLogs) Debug.Log("[BowlingGameManager] Lane fully reset (pins + ball).");
    }

    #region Debug test helpers
    // Helper to simulate a manual "strike" for testing
    [ContextMenu("Debug: Simulate Strike (Register 10)")]
    private void DebugSimulateStrike()
    {
        if (debugLogs) Debug.Log("[BowlingGameManager] Debug simulate strike.");
        StartShot();
        // simulate immediate end
        pinsDownAtStartOfRoll = 0;
        if (pinSettleCoroutine != null) StopCoroutine(pinSettleCoroutine);
        pinSettleCoroutine = StartCoroutine(DelayedRegisterDebug(10));
    }

    private IEnumerator DelayedRegisterDebug(int pins)
    {
        yield return new WaitForSeconds(0.5f);
        if (scoringManager != null) scoringManager.RegisterRoll(pins);
        StartCoroutine(ResetBallAfterDelay());
    }
    #endregion
}
