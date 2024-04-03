using UnityEngine;
using UnityEngine.InputSystem; // Make sure to include this for the Input System

public class DroneCollisionDetector : MonoBehaviour
{
    // This method is called when the drone continues to collide with another object.
    private void OnCollisionStay(Collision collision)
    {
        // Check if the object we collided with has the tag 'environment'.
        if (collision.gameObject.tag == "environment")
        {
            Debug.Log("Drone has touched environment!");

            // Trigger gamepad vibration
            VibrateGamepad();
        }
    }

    private void VibrateGamepad()
    {
        // Assuming a single gamepad is connected and used.
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            // Set vibration (leftMotor, rightMotor). Adjust intensity as needed.
            gamepad.SetMotorSpeeds(0.5f, 0.5f); // Vibrate at half intensity for both motors

            // Optional: Stop vibration after a short delay, if desired.
            Invoke("StopVibration", 0.5f); // Stop after 0.5 seconds
        }
    }

    private void StopVibration()
    {
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            // Stop vibration
            gamepad.SetMotorSpeeds(0f, 0f);
        }
    }
}
