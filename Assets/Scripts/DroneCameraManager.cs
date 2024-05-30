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

    [Header("UI Arrows")]
    public RawImage frontArrow;
    public RawImage backArrow;
    public RawImage leftArrow;
    public RawImage rightArrow;

    [Header("Joystick Indicator")]
    public GameObject joystickIndicatorParent; // Parent GameObject
    public RectTransform joystickIndicator; // Child RawImage RectTransform
    public float maxYScale = -0.62f; // Maximum Y scale value
    public float minYScale = -0.02841436f; // Minimum Y scale value

    private Camera currentActiveCamera;
    private Color defaultColor = Color.white;
    private Color activeColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color joystickIndicatorColor = new Color(0.0f, 1.0f, 0.0f, 1.0f); // Fluorescent green

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

        // Set the color of the joystick indicator
        joystickIndicator.GetComponent<RawImage>().color = joystickIndicatorColor;
    }

    private void Update()
    {
        Vector2 rightStickInputValue = rightStickInput.action.ReadValue<Vector2>();
        UpdateActiveCamera(rightStickInputValue);
        UpdateJoystickIndicator(rightStickInputValue);
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

        UpdateArrowColors();
    }

    private void UpdateArrowColors()
    {
        // Reset all arrows to default color
        frontArrow.color = defaultColor;
        backArrow.color = defaultColor;
        leftArrow.color = defaultColor;
        rightArrow.color = defaultColor;

        // Set the active camera's arrow to the active color
        if (currentActiveCamera == frontCamera)
        {
            frontArrow.color = activeColor;
        }
        else if (currentActiveCamera == backCamera)
        {
            backArrow.color = activeColor;
        }
        else if (currentActiveCamera == leftCamera)
        {
            leftArrow.color = activeColor;
        }
        else if (currentActiveCamera == rightCamera)
        {
            rightArrow.color = activeColor;
        }
    }

    private void UpdateJoystickIndicator(Vector2 joystickInput)
    {
        // Calculate the Y scale based on the joystick input magnitude
        float inputMagnitude = joystickInput.magnitude;
        float yScale = Mathf.Lerp(minYScale, maxYScale, inputMagnitude);

        // Adjust the Y scale of the joystick indicator's parent
        Vector3 newScale = joystickIndicatorParent.transform.localScale;
        newScale.y = yScale;
        joystickIndicatorParent.transform.localScale = newScale;

        // Adjust the rotation of the parent GameObject
        float angle = Mathf.Atan2(joystickInput.y, joystickInput.x) * Mathf.Rad2Deg;
        joystickIndicatorParent.transform.localRotation = Quaternion.Euler(0, 0, angle - 90);
    }
}
