using UnityEngine;

public class DroneBehaviours : MonoBehaviour
{
    public float noiseStrength = 0.1f; // Strength of the positional noise
    public float noiseFrequency = 2f; // How often to apply noise (times per second)
    private Vector3 velocity = Vector3.zero; // Current velocity, modified by SmoothDamp
    public float smoothTime = 0.3f; // Approximate time to reach the target. The smaller, the faster.

    private Vector3 targetPosition; // Target position after applying noise
    private float nextNoiseTime = 0f; // Internal timer to track when to apply noise

    private void Start()
    {
        targetPosition = transform.position; // Initialize target position
    }

    void Update()
    {
        // Ensure Y-axis is always pointing up by adjusting rotation
        AdjustRotation();

        // Apply positional noise at specified intervals
        if (Time.time >= nextNoiseTime)
        {
            UpdateTargetPositionWithNoise();
            nextNoiseTime = Time.time + 1f / noiseFrequency;
        }

        // Smoothly move towards the target position with variable speed
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    void AdjustRotation()
    {
        Vector3 forwardFlat = Vector3.Cross(Vector3.up, Vector3.Cross(transform.forward, Vector3.up));
        transform.rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
    }

    void UpdateTargetPositionWithNoise()
    {
        float noiseX = Random.Range(-noiseStrength, noiseStrength);
        float noiseY = Random.Range(-noiseStrength, noiseStrength);
        float noiseZ = Random.Range(-noiseStrength, noiseStrength);

        targetPosition += new Vector3(noiseX, noiseY, noiseZ);
    }
}