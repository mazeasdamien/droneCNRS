using UnityEngine;

public class ResetTransform : MonoBehaviour
{
    public GameObject referenceObject;

    public void ResetPositionAndRotation()
    {
        transform.position = referenceObject.transform.position;
        transform.rotation = referenceObject.transform.rotation;
    }
}
