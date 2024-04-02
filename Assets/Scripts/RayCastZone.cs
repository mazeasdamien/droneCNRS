using UnityEngine;

public class RayCastZone : MonoBehaviour
{
    public float raycastDistance = 10f;
    public LayerMask raycastLayer;

    void Update()
    {
        // Cast a ray below the drone
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, raycastLayer))
        {
            // Activate the MeshRenderer component of the hit object
            ActivateMeshRenderer(hit.collider.gameObject);

        }
    }

    void ActivateMeshRenderer(GameObject obj)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }
    }

    // Draw the raycast visualization in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.down * raycastDistance);
    }
}
