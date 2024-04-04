using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneMovement : MonoBehaviour
{
    public Rigidbody body;
    public float flightSpeed = 30f; // Speed of movement in the horizontal plane
    public float verticalSpeed = 10f; // Speed of vertical movement
    public float rotationSpeed = 10f; // Speed of rotation around the Y-axis
    public float cameraRotationSpeed = 30f; // Speed of camera rotation around its local X-axis

    public InputActionReference rightstick;
    public InputActionReference leftstick;
    public InputActionReference dpadup;
    public InputActionReference dpaddown;

    public Transform camera;

    private float _verticalMovement; // For vertical movement
    private Vector2 _rightStickInput; // For local forward/backward and left/right movement

    void Update()
    {
        // Use left stick for vertical movement and rotation
        _verticalMovement = leftstick.action.ReadValue<Vector2>().y;
        float rotation = leftstick.action.ReadValue<Vector2>().x * rotationSpeed * Time.deltaTime;
        transform.Rotate(0, rotation, 0, Space.World);

        // Use right stick for local forward/backward and left/right movement
        _rightStickInput = rightstick.action.ReadValue<Vector2>();

        // Camera rotation logic, inverted up and down
        float dpadVertical = dpadup.action.ReadValue<float>() - dpaddown.action.ReadValue<float>();
        float cameraRotationX = cameraRotationSpeed * Time.deltaTime * dpadVertical;
        float newRotationX = Mathf.Clamp(camera.localEulerAngles.x - cameraRotationX, 0, 90); // Invert direction here
        camera.localEulerAngles = new Vector3(newRotationX, camera.localEulerAngles.y, camera.localEulerAngles.z);
    }

    private void FixedUpdate()
    {
        Vector3 localForwardBackward = transform.forward * _rightStickInput.y * flightSpeed * Time.fixedDeltaTime;
        Vector3 localLeftRight = transform.right * _rightStickInput.x * flightSpeed * Time.fixedDeltaTime;
        Vector3 verticalMove = Vector3.up * _verticalMovement * verticalSpeed * Time.fixedDeltaTime;

        // Combine movements
        Vector3 movement = localForwardBackward + localLeftRight + verticalMove;

        // Apply movement to the Transform
        transform.position += movement;
    }
}