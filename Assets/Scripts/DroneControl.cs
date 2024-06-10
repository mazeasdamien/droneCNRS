using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneControl : MonoBehaviour
{
    public static DroneControl Instance { get; private set; }

    [Header("Movement Properties")]
    public Rigidbody body;
    public float flightSpeed = 30f;
    public float verticalSpeed = 10f;
    public float rotationSpeed = 10f;
    public float cameraRotationSpeed = 30f;

    [Header("Input References")]
    public InputActionReference rightstick;
    public InputActionReference leftstick;
    public InputActionReference dpadup;
    public InputActionReference dpaddown;
    public InputActionReference southbutton;

    [Header("Noise Properties")]
    public float noiseStrength = 0.1f;
    public Vector2 noiseFrequencyRange = new(1f, 3f);
    private Vector3 velocity = Vector3.zero;
    public float smoothTime = 0.3f;
    private Vector3 targetPosition;
    private float nextNoiseTime = 0f;

    [Header("Camera")]
    public Transform cam;

    [Header("Drone Path Recording")]
    public GameObject originObject; // Reference to the origin GameObject

    [Header("UI References")]
    public RectTransform uiElement; // Reference to the RectTransform that you want to control

    private Vector2 _rightStickInput;
    private Vector2 _leftStickInput;
    private float _verticalMovement;
    private bool isDroneMoving = false;
    private float movementThreshold = 0.1f;

    private Vector3 lastRecordedPosition;
    private float distanceThreshold = 1f; // Distance to move before recording position & rotation

    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private Queue<Quaternion> rotationHistory = new Queue<Quaternion>();

    private StreamWriter inputLogger; // StreamWriter for logging inputs
    private bool isLogging = false; // Flag to control logging

    private float currentCountdown = 0f; // Variable to store the current countdown value

    [Header("Control Sensitivity")]
    public float verticalDeadZone = 0.2f; // Define the dead zone size for vertical movement

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        targetPosition = transform.position; // Initialize target position
        lastRecordedPosition = transform.position; // Initialize last recorded position
        UpdateNoiseTime();

        // Initialize the DronePathRecorder
        if (DronePathRecorder.Instance != null)
        {
            DronePathRecorder.Instance.Initialize(originObject, transform);
        }

        // Enable input actions
        rightstick?.action?.Enable();
        leftstick?.action?.Enable();
        dpadup?.action?.Enable();
        dpaddown?.action?.Enable();
        southbutton?.action?.Enable();
    }

    private void Update()
    {
        HandleInput();

        isDroneMoving = body.velocity.magnitude > movementThreshold;

        // Check if the drone is moving to update its target position and potentially record its flight path
        if (isDroneMoving)
        {
            targetPosition = transform.position; // Update target position continuously while moving

            // If the drone has moved a significant distance since the last record, update its recorded position and rotation
            if (Vector3.Distance(transform.position, lastRecordedPosition) > distanceThreshold)
            {
                RecordPositionAndRotation();
                lastRecordedPosition = transform.position; // Update the last recorded position
            }
        }

        // Logic for handling noise and stationary behavior remains unchanged
        if (!isDroneMoving && Time.time >= nextNoiseTime)
        {
            UpdateTargetPositionWithNoise();
            UpdateNoiseTime();
        }

        if (!isDroneMoving)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }

        AdjustRotation();

        // Update UI Element rotation to match camera X rotation
        if (uiElement != null)
        {
            Vector3 newRotation = uiElement.localEulerAngles;
            newRotation.z = cam.localEulerAngles.x;
            uiElement.localEulerAngles = newRotation;
        }

        // Log inputs if logging is enabled
        if (isLogging)
        {
            LogInput();
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void RecordPositionAndRotation()
    {
        // Debug.Log($"Recording Position: {transform.position}, Rotation: {transform.rotation}");

        positionHistory.Enqueue(transform.position);
        rotationHistory.Enqueue(transform.rotation);

        if (positionHistory.Count > 3)
        {
            positionHistory.Dequeue();
            rotationHistory.Dequeue();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow; // Making the Gizmos more visible
        Vector3 previousPosition = Vector3.zero;
        bool isFirstIteration = true;

        foreach (var position in positionHistory)
        {
            Gizmos.DrawSphere(position, 0.2f); // Increased size for better visibility
            if (!isFirstIteration)
            {
                Gizmos.DrawLine(previousPosition, position);
            }
            previousPosition = position;
            isFirstIteration = false;
        }
    }

    private void HandleInput()
    {
        // Handle gamepad input
        _leftStickInput = leftstick?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        _rightStickInput = rightstick?.action?.ReadValue<Vector2>() ?? Vector2.zero;

        // Apply dead zone to vertical movement
        _verticalMovement = Mathf.Abs(_leftStickInput.y) < verticalDeadZone ? 0 : Mathf.Sign(_leftStickInput.y);

        // Check if the joystick is in the upper or lower part
        bool isInUpperOrLowerPart = IsInUpperOrLowerPart(_leftStickInput);

        // Apply rotation only if the joystick is not in the upper or lower part
        if (!isInUpperOrLowerPart)
        {
            float rotation = _leftStickInput.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotation, 0, Space.World);
        }

        float dpadVertical = (dpadup?.action?.ReadValue<float>() ?? 0f) - (dpaddown?.action?.ReadValue<float>() ?? 0f);
        float cameraRotationX = cameraRotationSpeed * Time.deltaTime * dpadVertical;
        float newRotationX = Mathf.Clamp(cam.localEulerAngles.x - cameraRotationX, 0, 90);
        cam.localEulerAngles = new Vector3(newRotationX, cam.localEulerAngles.y, cam.localEulerAngles.z);

        // Handle keyboard input only if gamepad input is not detected
        if (_leftStickInput == Vector2.zero && _rightStickInput == Vector2.zero)
        {
            _rightStickInput.y = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            _rightStickInput.x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);

            _verticalMovement = (Keyboard.current.rKey.isPressed ? 1f : 0f) - (Keyboard.current.fKey.isPressed ? 1f : 0f);

            float keyboardRotation = (Keyboard.current.qKey.isPressed ? -1f : 0f) + (Keyboard.current.eKey.isPressed ? 1f : 0f);
            keyboardRotation *= rotationSpeed * Time.deltaTime;
            transform.Rotate(0, keyboardRotation, 0, Space.World);

            // Handle D-pad up/down using arrow keys
            dpadVertical += (Keyboard.current.upArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.downArrowKey.isPressed ? 1f : 0f);
            cameraRotationX = cameraRotationSpeed * Time.deltaTime * dpadVertical;
            newRotationX = Mathf.Clamp(cam.localEulerAngles.x - cameraRotationX, 0, 90);
            cam.localEulerAngles = new Vector3(newRotationX, cam.localEulerAngles.y, cam.localEulerAngles.z);
        }
    }

    private bool IsInUpperOrLowerPart(Vector2 stickInput)
    {
        float angle = Vector2.SignedAngle(Vector2.up, stickInput);
        return Mathf.Abs(angle) <= 45f || Mathf.Abs(angle) >= 135f;
    }

    private void ApplyMovement()
    {
        // Get the intended movement direction based on input
        Vector3 localForwardBackward = transform.forward * _rightStickInput.y * flightSpeed * Time.fixedDeltaTime;
        Vector3 localLeftRight = transform.right * _rightStickInput.x * flightSpeed * Time.fixedDeltaTime;
        Vector3 verticalMove = Vector3.up * _verticalMovement * verticalSpeed * Time.fixedDeltaTime;

        // Calculate total desired movement without noise
        Vector3 desiredMovement = localForwardBackward + localLeftRight + verticalMove;

        // Only apply noise if no input is detected
        Vector3 finalVelocity;
        if (_rightStickInput == Vector2.zero && _leftStickInput == Vector2.zero && _verticalMovement == 0)
        {
            // Determine the magnitude of the desired movement to scale the noise accordingly
            float movementMagnitude = desiredMovement.magnitude;

            // Generate noise scaled by the magnitude of the movement. This makes the noise effect more pronounced
            // during faster movements and more subtle during slower movements.
            Vector3 noise = new Vector3(
                UnityEngine.Random.Range(-noiseStrength, noiseStrength) * movementMagnitude,
                UnityEngine.Random.Range(-noiseStrength, noiseStrength) * movementMagnitude,
                UnityEngine.Random.Range(-noiseStrength, noiseStrength) * movementMagnitude
            );

            finalVelocity = desiredMovement + noise;
        }
        else
        {
            finalVelocity = desiredMovement;
        }
        body.velocity = finalVelocity;
    }

    private void UpdateTargetPositionWithNoise()
    {
        float noiseX = UnityEngine.Random.Range(-noiseStrength, noiseStrength);
        float noiseY = UnityEngine.Random.Range(-noiseStrength, noiseStrength);
        float noiseZ = UnityEngine.Random.Range(-noiseStrength, noiseStrength);

        targetPosition += new Vector3(noiseX, noiseY, noiseZ);
    }

    private void AdjustRotation()
    {
        Vector3 forwardFlat = Vector3.Cross(Vector3.up, Vector3.Cross(transform.forward, Vector3.up));
        transform.rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
    }

    private void UpdateNoiseTime()
    {
        nextNoiseTime = Time.time + UnityEngine.Random.Range(noiseFrequencyRange.x, noiseFrequencyRange.y);
    }

    public void StartInputLogging(string filePath)
    {
        inputLogger = new StreamWriter(filePath);
        inputLogger.WriteLine("Timestamp,DateTime,Countdown,RightStickX,RightStickY,LeftStickX,LeftStickY,DpadUp,DpadDown,SouthButton");
        isLogging = true;
    }

    public void StopInputLogging()
    {
        isLogging = false;
        if (inputLogger != null)
        {
            inputLogger.Close();
            inputLogger = null;
        }
    }

    private void LogInput()
    {
        DateTime dateTime = DateTime.UtcNow;
        string timestamp = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        Vector2 rightStickValues = rightstick?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector2 leftStickValues = leftstick?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        float dpadUpValue = dpadup?.action?.ReadValue<float>() ?? 0f;
        float dpadDownValue = dpaddown?.action?.ReadValue<float>() ?? 0f;
        bool southButtonState = southbutton?.action?.ReadValue<float>() > 0f;

        inputLogger.WriteLine($"{timestamp},{dateTime:yyyy-MM-dd HH:mm:ss.fff},{currentCountdown},{rightStickValues.x},{rightStickValues.y},{leftStickValues.x},{leftStickValues.y},{dpadUpValue},{dpadDownValue},{southButtonState}");
    }

    public void UpdateCountdownValue(float countdownValue)
    {
        currentCountdown = countdownValue;
    }
}
