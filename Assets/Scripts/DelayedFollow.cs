using System.Collections.Generic;
using UnityEngine;

public class DelayedFollow : MonoBehaviour
{
    public Transform targetDrone;
    public Transform masterTargetPosition;
    public float delay = 1.0f;
    public float followSpeed = 2.0f;
    public float rotationSpeed = 0.5f;
    public float raycastDistance = 2.0f;
    public float ascendSpeed = 1.0f;

    private Queue<(Vector3 position, Quaternion rotation, float time)> history = new Queue<(Vector3, Quaternion, float)>();
    private bool isAvoidingObstacle = false;

    [Header("Noise Properties")]
    public float noiseStrength = 0.1f;
    public Vector2 noiseFrequencyRange = new Vector2(1f, 3f);
    public float smoothTime = 0.3f;


    void Update()
    {
        Vector3 targetPosition = masterTargetPosition.position;
        history.Enqueue((targetPosition, targetDrone.rotation, Time.time));

        while (history.Count > 0 && history.Peek().time < Time.time - delay)
        {
            history.Dequeue();
        }

        if (history.Count > 0)
        {
            var (recordedPosition, recordedRotation, _) = history.Peek();
            if (isAvoidingObstacle)
            {
                float currentY = transform.position.y;
                Vector3 positionXZ = new Vector3(recordedPosition.x, currentY, recordedPosition.z);
                transform.position = Vector3.Lerp(transform.position, positionXZ, followSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, recordedPosition, followSpeed * Time.deltaTime);
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, recordedRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
