using UnityEngine;

public class BowlingGameManager : MonoBehaviour
{
    public Rigidbody ballRb;
    public Transform ballSpawnPoint;
    public PinManager pinManager;
    public ScoringManager scoringManager;

    public void OnShotCompleted()
    {
        int pinsDown = pinManager.GetPinsDown();
        scoringManager.RegisterRoll(pinsDown);

        Debug.Log($"Roll: {pinsDown} pins — Total Score: {scoringManager.GetTotalScore()}");

        if (scoringManager.IsGameOver())
        {
            Debug.Log("🎳 GAME OVER!");
            return;
        }

        if (scoringManager.IsFrameComplete())
        {
            pinManager.ResetPins();
        }
        else
        {
            pinManager.PrepareForSecondRoll();
        }

        ResetBall();
    }

    private void ResetBall()
    {
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ballRb.transform.SetPositionAndRotation(ballSpawnPoint.position, ballSpawnPoint.rotation);
    }
}
