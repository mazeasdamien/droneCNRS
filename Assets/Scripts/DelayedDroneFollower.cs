using System.Collections.Generic;
using UnityEngine;

public class DelayedFollow : MonoBehaviour
{
    public Transform targetDrone; // The target drone to follow
    public float delay = 1.0f; // Delay in seconds to follow the target drone's past position
    public float followSpeed = 2.0f; // Speed at which the drone follows the target
    public float rotationSpeed = 0.5f; // Speed at which the drone aligns its rotation to the target
    public float verticalOffset = 5.0f; // Vertical offset from the target drone

    private Queue<(Vector3 position, Quaternion rotation, float time)> history = new Queue<(Vector3, Quaternion, float)>();

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
            var (position, rotation, _) = history.Peek();
            transform.position = Vector3.Lerp(transform.position, position, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
        }
    }
}
