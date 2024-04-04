using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneControl : MonoBehaviour
{
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

    [Header("Noise Properties")]
    public float noiseStrength = 0.1f;
    public Vector2 noiseFrequencyRange = new Vector2(1f, 3f);
    private Vector3 velocity = Vector3.zero;
    public float smoothTime = 0.3f;
    private Vector3 targetPosition;
    private float nextNoiseTime = 0f;

    [Header("Camera")]
    public Transform cam;

    private Vector2 _rightStickInput;
    private float _verticalMovement;
    private bool isDroneMoving = false;
    private float movementThreshold = 0.1f;

    private Vector3 lastRecordedPosition;
    private float distanceThreshold = 1f; // Distance to move before recording position & rotation

    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private Queue<Quaternion> rotationHistory = new Queue<Quaternion>();


    private void Start()
    {
        targetPosition = transform.position; // Initialize target position
        lastRecordedPosition = transform.position; // Initialize last recorded position
        UpdateNoiseTime();
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
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }


    private void RecordPositionAndRotation()
    {
        //Debug.Log($"Recording Position: {transform.position}, Rotation: {transform.rotation}");

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
        _verticalMovement = leftstick.action.ReadValue<Vector2>().y;
        float rotation = leftstick.action.ReadValue<Vector2>().x * rotationSpeed * Time.deltaTime;
        transform.Rotate(0, rotation, 0, Space.World);

        _rightStickInput = rightstick.action.ReadValue<Vector2>();

        float dpadVertical = dpadup.action.ReadValue<float>() - dpaddown.action.ReadValue<float>();
        float cameraRotationX = cameraRotationSpeed * Time.deltaTime * dpadVertical;
        float newRotationX = Mathf.Clamp(cam.localEulerAngles.x - cameraRotationX, 0, 90);
        cam.localEulerAngles = new Vector3(newRotationX, cam.localEulerAngles.y, cam.localEulerAngles.z);
    }

    private void ApplyMovement()
    {
        Vector3 localForwardBackward = transform.forward * _rightStickInput.y * flightSpeed * Time.fixedDeltaTime;
        Vector3 localLeftRight = transform.right * _rightStickInput.x * flightSpeed * Time.fixedDeltaTime;
        Vector3 verticalMove = Vector3.up * _verticalMovement * verticalSpeed * Time.fixedDeltaTime;

        body.velocity = localForwardBackward + localLeftRight + verticalMove;
    }

    private void UpdateTargetPositionWithNoise()
    {
        float noiseX = Random.Range(-noiseStrength, noiseStrength);
        float noiseY = Random.Range(-noiseStrength, noiseStrength);
        float noiseZ = Random.Range(-noiseStrength, noiseStrength);

        targetPosition += new Vector3(noiseX, noiseY, noiseZ);
    }

    private void AdjustRotation()
    {
        Vector3 forwardFlat = Vector3.Cross(Vector3.up, Vector3.Cross(transform.forward, Vector3.up));
        transform.rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
    }

    private void UpdateNoiseTime()
    {
        nextNoiseTime = Time.time + Random.Range(noiseFrequencyRange.x, noiseFrequencyRange.y);
    }
}
