using UnityEngine;

public class MatchPO : MonoBehaviour
{
    public Transform target;

    private Vector3 positionOffset;
    private Quaternion rotationOffset;

    void Start()
    {
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
            transform.position = target.position + positionOffset;
            transform.rotation = target.rotation * rotationOffset;
        }
    }
}
