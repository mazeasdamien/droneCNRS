using UnityEngine;
using TMPro;

public class DroneDistanceToGround : MonoBehaviour
{
    public TextMeshProUGUI distanceText;  // Assign your TextMeshProUGUI object in the inspector

    void Update()
    {
        // Define the starting point and direction of the raycast
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = Vector3.down;

        // Perform the raycast
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit))
        {
            // Check if the hit object has the tag "Environment"
            if (hit.collider.CompareTag("environment"))
            {
                // Get the distance to the ground
                float distance = hit.distance;

                // Update the TextMeshProUGUI with the distance
                distanceText.text = $"{distance:F2} meters";
            }
        }
        else
        {
            // If no hit is detected, you can display an appropriate message
            distanceText.text = "0000";
        }
    }

    // To visualize the ray in the Scene view (optional)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 100f);
    }
}
