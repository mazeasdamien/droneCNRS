using UnityEngine;
using UnityEngine.InputSystem;

public class DroneCollisionDetector : MonoBehaviour
{
    private int collisionCount = 0;
    private float totalCollisionTime = 0f;
    private const float CollisionIncrement = 1f; // Fixed time increment per collision
    private bool isColliding = false; // Track if a collision is ongoing

    public Timer timerScript; // Reference to Timer script

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            if (!isColliding)
            {
                collisionCount++;
                isColliding = true;
                VibrateGamepad();
                Debug.Log("Collision Enter Detected");
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            totalCollisionTime += CollisionIncrement * Time.deltaTime; // Increment based on time delta
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            isColliding = false;
        }
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
