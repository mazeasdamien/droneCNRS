using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneControl : MonoBehaviour
{
    // Singleton instance
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
    public Vector2 noiseFrequencyRange = new Vector2(1f, 3f);
    private Vector3 velocity = Vector3.zero;
    public float smoothTime = 0.3f;
    private Vector3 targetPosition;
    private float nextNoiseTime = 0f;

    [Header("Camera")]
    public Transform cam;

    [Header("Drone Path Recording")]
    public GameObject originObject; // Reference to the origin GameObject

    private Vector2 _rightStickInput;
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
        _verticalMovement = leftstick?.action?.ReadValue<Vector2>().y ?? 0f;
        float rotation = leftstick?.action?.ReadValue<Vector2>().x ?? 0f;
        rotation *= rotationSpeed * Time.deltaTime;

        transform.Rotate(0, rotation, 0, Space.World);
        // Right stick functionality for other purposes goes here
        // For example, if right stick controls movement as well, it should be processed here.
        _rightStickInput = rightstick?.action?.ReadValue<Vector2>() ?? Vector2.zero; // This allows for right stick input processing for movement or other actions

        float dpadVertical = (dpadup?.action?.ReadValue<float>() ?? 0f) - (dpaddown?.action?.ReadValue<float>() ?? 0f);

        float cameraRotationX = cameraRotationSpeed * Time.deltaTime * dpadVertical;
        float newRotationX = Mathf.Clamp(cam.localEulerAngles.x - cameraRotationX, 0, 90);
        cam.localEulerAngles = new Vector3(newRotationX, cam.localEulerAngles.y, cam.localEulerAngles.z);
    }

    private void ApplyMovement()
    {
        // Get the intended movement direction based on input
        Vector3 localForwardBackward = transform.forward * _rightStickInput.y * flightSpeed * Time.fixedDeltaTime;
        Vector3 localLeftRight = transform.right * _rightStickInput.x * flightSpeed * Time.fixedDeltaTime;
        Vector3 verticalMove = Vector3.up * _verticalMovement * verticalSpeed * Time.fixedDeltaTime;

        // Calculate total desired movement without noise
        Vector3 desiredMovement = localForwardBackward + localLeftRight + verticalMove;

        // Determine the magnitude of the desired movement to scale the noise accordingly
        float movementMagnitude = desiredMovement.magnitude;

        // Generate noise scaled by the magnitude of the movement. This makes the noise effect more pronounced
        // during faster movements and more subtle during slower movements.
        Vector3 noise = new Vector3(
            UnityEngine.Random.Range(-noiseStrength, noiseStrength) * movementMagnitude,
            UnityEngine.Random.Range(-noiseStrength, noiseStrength) * movementMagnitude,
            UnityEngine.Random.Range(-noiseStrength, noiseStrength) * movementMagnitude
        );

        // Apply the scaled noise to the velocity, adding it to the desired movement
        body.velocity = desiredMovement + noise;
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
