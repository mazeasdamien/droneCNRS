using UnityEngine;
using UnityEngine.InputSystem;

public class DroneController : MonoBehaviour
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
    private Vector3 velocity = Vector3.zero; // Used by SmoothDamp
    public float smoothTime = 0.3f;
    private Vector3 targetPosition;
    private float nextNoiseTime = 0f;

    [Header("Camera")]
    public Transform cam;

    private Vector2 _rightStickInput;
    private float _verticalMovement;
    private bool isDroneMoving = false;
    private float movementThreshold = 0.1f; // Threshold to check if drone is moving

    private void Start()
    {
        targetPosition = transform.position; // Initialize target position
        UpdateNoiseTime();
    }

    private void Update()
    {
        HandleInput();

        // Check if the drone is moving by assessing its velocity magnitude
        isDroneMoving = body.velocity.magnitude > movementThreshold;

        // Continuously update target position to current position if the drone is moving
        if (isDroneMoving)
        {
            targetPosition = transform.position;
        }

        // When the drone is not moving and it's time for the next noise adjustment
        if (!isDroneMoving && Time.time >= nextNoiseTime)
        {
            UpdateTargetPositionWithNoise();
            UpdateNoiseTime();
        }

        // If the drone is not moving, smoothly adjust position to add realism
        if (!isDroneMoving)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }

        AdjustRotation(); // Keep Y-axis up
    }

    private void FixedUpdate()
    {
        ApplyMovement();
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
