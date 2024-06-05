using UnityEngine;

public class ResetTransform : MonoBehaviour
{// Reference to the GameObject that holds the initial transform
    public GameObject referenceObject;

    public void ResetPositionAndRotation()
    {
        transform.position = referenceObject.transform.position;
        transform.rotation = referenceObject.transform.rotation;
    }
}
