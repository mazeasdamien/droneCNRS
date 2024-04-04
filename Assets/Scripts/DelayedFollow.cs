using System.Collections.Generic;
using UnityEngine;

public class DelayedFollow : MonoBehaviour
{
    public Transform targetDrone;
    public float delay = 1.0f;
    public float followSpeed = 2.0f;
    public float rotationSpeed = 0.5f;
    public float verticalOffset = 5.0f;
    public float raycastDistance = 2.0f; // Distance to check for obstacles
    public float ascendSpeed = 1.0f; // Speed at which the drone ascends to avoid obstacles

    private Queue<(Vector3 position, Quaternion rotation, float time)> history = new Queue<(Vector3, Quaternion, float)>();
    private bool isAvoidingObstacle = false; // Track whether the drone is in avoidance mode


    [Header("Noise Properties")]
    public float noiseStrength = 0.1f;
    public Vector2 noiseFrequencyRange = new Vector2(1f, 3f);
    private Vector3 velocity = Vector3.zero; // Used by SmoothDamp
    public float smoothTime = 0.3f;
    private Vector3 targetPosition;
    private float nextNoiseTime = 0f;
    void Update()
    {
        // Record the current position, rotation, and time of the target drone
        history.Enqueue((targetDrone.position + Vector3.up * verticalOffset, targetDrone.rotation, Time.time));

        // Remove old records while the timestamp is out of the delay range
        while (history.Count > 0 && history.Peek().time < Time.time - delay)
        {
            history.Dequeue();
        }

        // If there are records in the history, move and rotate towards the oldest record (from ~1 second ago)
        if (history.Count > 0)
        {
            var (targetPosition, targetRotation, _) = history.Peek();

            // When avoiding obstacles, ignore the Y-axis position from the target
            if (isAvoidingObstacle)
            {
                // Keep the current Y position
                float currentY = transform.position.y;
                // Lerp only X and Z position
                Vector3 positionXZ = new Vector3(targetPosition.x, currentY, targetPosition.z);
                transform.position = Vector3.Lerp(transform.position, positionXZ, followSpeed * Time.deltaTime);
            }
            else
            {
                // Follow target position including Y-axis
                transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
