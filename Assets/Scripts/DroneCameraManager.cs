using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DroneCameraManager : MonoBehaviour
{
    [Header("Drone References")]
    public DroneControl droneControl;

    [Header("Cameras")]
    public Camera frontCamera;
    public Camera backCamera;
    public Camera leftCamera;
    public Camera rightCamera;

    [Header("Render Texture")]
    public RenderTexture renderTexture;

    [Header("Input References")]
    public InputActionReference rightStickInput;

    private Camera currentActiveCamera;

    private void Start()
    {
        if (droneControl == null)
        {
            Debug.LogError("DroneControl reference is missing!");
            return;
        }

        // Initialize cameras to disable target texture
        frontCamera.targetTexture = null;
        backCamera.targetTexture = null;
        leftCamera.targetTexture = null;
        rightCamera.targetTexture = null;

        // Enable input actions
        rightStickInput.action.Enable();

        // Set initial active camera
        SetActiveCamera(frontCamera);
    }

    private void Update()
    {
        Vector2 rightStickInputValue = rightStickInput.action.ReadValue<Vector2>();
        UpdateActiveCamera(rightStickInputValue);
    }

    private void UpdateActiveCamera(Vector2 rightStickInput)
    {
        // Determine the dominant direction of the joystick input
        Camera newActiveCamera = currentActiveCamera;

        if (rightStickInput.magnitude > 0.1f)
        {
            if (Mathf.Abs(rightStickInput.x) > Mathf.Abs(rightStickInput.y))
            {
                // Horizontal movement
                if (rightStickInput.x > 0)
                {
                    newActiveCamera = rightCamera;
                }
                else
                {
                    newActiveCamera = leftCamera;
                }
            }
            else
            {
                // Vertical movement
                if (rightStickInput.y > 0)
                {
                    newActiveCamera = frontCamera;
                }
                else
                {
                    newActiveCamera = backCamera;
                }
            }
        }

        // Update the active camera if it has changed
        if (newActiveCamera != currentActiveCamera)
        {
            SetActiveCamera(newActiveCamera);
        }
    }

    private void SetActiveCamera(Camera activeCamera)
    {
        if (currentActiveCamera != null)
        {
            currentActiveCamera.targetTexture = null;
        }

        currentActiveCamera = activeCamera;
        currentActiveCamera.targetTexture = renderTexture;
    }
}
