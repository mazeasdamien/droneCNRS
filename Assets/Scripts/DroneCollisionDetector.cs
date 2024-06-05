using UnityEngine;
using UnityEngine.InputSystem;

public class DroneCollisionDetector : MonoBehaviour
{
    private bool isColliding = false; // Track if a collision is ongoing
    public Timer timerScript; // Reference to Timer script

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            if (!isColliding)
            {
                isColliding = true;
                VibrateGamepad();
                Debug.Log("Collision Enter Detected");
                timerScript.FreezeCountdown(); // Freeze the countdown
                timerScript.DisplayCollisionWarning(true); // Show warning message
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            isColliding = false;
            timerScript.UnfreezeCountdown(); // Unfreeze the countdown
            timerScript.DisplayCollisionWarning(false); // Hide warning message
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
