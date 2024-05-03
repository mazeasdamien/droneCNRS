using UnityEngine;
using UnityEngine.InputSystem;

public class DroneCollisionDetector : MonoBehaviour
{
    private int collisionCount = 0;
    private float totalCollisionTime = 0f;
    private Timer timerScript; // Reference to Timer script
    private float countdownTime = 0f;

    private void Start()
    {
        timerScript = FindObjectOfType<Timer>(); // Find Timer script in the scene
        if (timerScript != null)
        {
            countdownTime = timerScript.countdownTime; // Initialize countdownTime
            timerScript.UpdateCountdownTime(countdownTime); // Update countdownTime in case it changes
        }
    }

    private void Update()
    {
        if (timerScript != null && countdownTime != timerScript.countdownTime)
        {
            countdownTime = timerScript.countdownTime; // Update countdownTime if it changes
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            collisionCount++;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            totalCollisionTime += Time.deltaTime;
        }
    }

    private void OnDestroy()
    {
        // Calculate the percentage of time spent colliding during the countdown
        float collisionPercentage = (totalCollisionTime / countdownTime) * 100f;

        // Access the Timer script and update the collision data in the summary
        if (timerScript != null)
        {
            timerScript.UpdateCollisionSummary(collisionCount, collisionPercentage);
        }
    }
}
