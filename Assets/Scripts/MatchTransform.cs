using UnityEngine;

public class MatchTransformWithOffset : MonoBehaviour
{
    public Transform target; // Assign the target GameObject in the Inspector

    private Vector3 positionOffset;
    private Quaternion rotationOffset;

    void Start()
    {
        if (target != null)
        {
            // Calculate initial offsets
            positionOffset = transform.position - target.position;
            rotationOffset = Quaternion.Inverse(target.rotation) * transform.rotation;
        }
    }

    void Update()
    {
        if (target != null)
        {
            // Match position with offset
            transform.position = target.position + positionOffset;

            // Match rotation with offset
            transform.rotation = target.rotation * rotationOffset;
        }
    }
}
