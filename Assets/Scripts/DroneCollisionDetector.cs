using UnityEngine;
using UnityEngine.InputSystem;

public class DroneCollisionDetector : MonoBehaviour
{
    private int collisionCount = 0;
    private float totalCollisionTime = 0f;
    private const float CollisionIncrement = 1f; // Fixed time increment per collision

    public Timer timerScript; // Reference to Timer script

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            collisionCount++;
            totalCollisionTime += CollisionIncrement; // Increment the total collision time by a fixed amount
            VibrateGamepad();
            Debug.Log("Collision Enter Detected");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Optionally, you can handle any specific logic when collision exits, if necessary.
        Debug.Log("Collision Exit Detected");
    }

    private void OnDestroy()
    {
        UpdateCollisionSummary(); // Ensure this gets called reliably, consider moving to a different method if needed
    }

    // Change this method's access level to public
    public void UpdateCollisionSummary()
    {
        if (timerScript != null)
        {
            float collisionPercentage = (totalCollisionTime / timerScript.countdownTime) * 100f;
            timerScript.UpdateCollisionSummary(collisionCount, collisionPercentage);
        }
    }

    private void VibrateGamepad()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(0.5f, 0.5f); // Vibrate at half intensity for both motors
            Invoke(nameof(StopVibration), 0.5f); // Stop after 0.5 seconds
        }
    }

    private void StopVibration()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(0f, 0f);
        }
    }
}
