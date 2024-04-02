using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CamFollow : MonoBehaviour
{
    public Transform target;
    public RawImage[] rawImages;

    public float sensitivity = 0.01f;
    private Vector3 targetOffset; // Offset from the target's position
    private float distanceToTarget; // Distance to the target

    void Start()
    {
        if (target != null)
        {
            // Calculate the initial distance and offset from the target
            distanceToTarget = Vector3.Distance(transform.position, target.position);
            targetOffset = transform.position - target.position;
        }
    }

    void LateUpdate() // Use LateUpdate for camera movements
    {
        if (target != null)
        {
            // Update camera position based on the target's movement
            Vector3 desiredPosition = target.position + targetOffset;
            transform.position = Vector3.MoveTowards(transform.position, desiredPosition, distanceToTarget);

            // Adjust for mouse scroll input to rotate around the target
            if (Input.mousePosition.x < Screen.width / 2 && Input.mousePosition.y > Screen.height / 2)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0)
                {
                    // Calculate rotation around the target
                    Quaternion rotation = Quaternion.AngleAxis(scroll * sensitivity * 1000, Vector3.up);
                    targetOffset = rotation * targetOffset; // Apply rotation to the offset

                    ChangeRawImageColor(scroll > 0 ? Color.blue : Color.blue); // Change color based on scroll direction
                    StopAllCoroutines();
                    StartCoroutine(ResetRawImageColorAfterDelay(0.5f));
                }
            }

            // Ensure the camera maintains its orientation towards the target
            transform.LookAt(target);
        }
    }

    IEnumerator ResetRawImageColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangeRawImageColor(Color.white);
    }

    void ChangeRawImageColor(Color color)
    {
        foreach (var rawImage in rawImages)
        {
            rawImage.color = color;
        }
    }
}
