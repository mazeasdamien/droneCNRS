using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DroneCollisionDetector : MonoBehaviour
{
    private bool isColliding = false;
    public Timer timerScript;
    public TextMeshProUGUI collisionText;
    public float totalCollisionTime;
    public int CollisionNumber;

    private float collisionStartTime;
    private float lastCollisionTime = -5f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            if (!isColliding && timerScript.IsTimerRunning())
            {
                isColliding = true;
                collisionStartTime = timerScript.GetTimeRemaining();
                VibrateGamepad();
                Debug.Log("Collision Enter Detected");
                if (collisionText != null)
                {
                    collisionText.text = "CRASH!";
                    collisionText.gameObject.SetActive(true);
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("environment"))
        {
            if (isColliding && timerScript.IsTimerRunning())
            {
                isColliding = false;
                float collisionEndTime = timerScript.GetTimeRemaining();
                totalCollisionTime += collisionStartTime - collisionEndTime;

                if (Time.time - lastCollisionTime >= 2f)
                {
                    CollisionNumber++;
                    lastCollisionTime = Time.time;
                }

                if (collisionText != null)
                {
                    collisionText.gameObject.SetActive(false);
                }
            }
        }
    }

    private void VibrateGamepad()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(0.5f, 0.5f);
            Invoke(nameof(StopVibration), 0.5f);
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
