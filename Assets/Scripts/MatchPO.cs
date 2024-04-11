using UnityEngine;

public class MatchPO : MonoBehaviour
{
    public Transform target; // Assign the first GameObject’s Transform in the Inspector

    private Vector3 positionOffset;
    private Quaternion rotationOffset;

    void Start()
    {
        // Calculate initial offset
        if (target != null)
        {
            positionOffset = transform.position - target.position;
            rotationOffset = Quaternion.Inverse(target.rotation) * transform.rotation;
        }
    }

    void Update()
    {
        if (target != null)
        {
            // Match position and rotation considering the initial offset
            transform.position = target.position + positionOffset;
            transform.rotation = target.rotation * rotationOffset;
        }
    }
}
