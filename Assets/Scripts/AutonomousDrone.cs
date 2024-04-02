using UnityEngine;

public class AutonomousDrone : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float spacing = 2f; // Spacing between points

    [Header("Area Parameters")]
    public float areaWidth = 10f;
    public float areaHeight = 10f;
    public Vector3 areaCenter = Vector3.zero;

    public GameObject endPositionObject; // Drag the GameObject for the end position here

    private Vector3[] pathPoints;
    private int currentPointIndex;
    private bool movingForward = true;

    void OnEnable()
    {
        GeneratePath();
    }

    void GeneratePath()
    {
        // Generate a path covering the specified area with spacing
        int numPointsX = Mathf.CeilToInt(areaWidth / spacing) + 1;
        int numPointsZ = Mathf.CeilToInt(areaHeight / spacing) + 1;

        pathPoints = new Vector3[numPointsX * numPointsZ * 2];

        int index = 0;
        for (float x = -areaWidth / 2f; x <= areaWidth / 2f; x += spacing)
        {
            float zStart = movingForward ? -areaHeight / 2f : areaHeight / 2f;
            float zEnd = movingForward ? areaHeight / 2f : -areaHeight / 2f;

            for (float z = zStart; (movingForward && z <= zEnd) || (!movingForward && z >= zEnd); z += spacing * Mathf.Sign(zEnd - zStart))
            {
                pathPoints[index] = new Vector3(x + areaCenter.x, transform.position.y + areaCenter.y, z + areaCenter.z);
                index++;
            }

            movingForward = !movingForward;
        }

        // Find the position of the end GameObject and set it as the last point of the path
        if (endPositionObject != null)
        {
            index = Mathf.Min(index, pathPoints.Length - 1);
            pathPoints[index] = endPositionObject.transform.position;
        }

        // Duplicate the path in reverse order to create a return path
        for (int i = pathPoints.Length - 1; i >= 0; i--)
        {
            index++;
            index = Mathf.Min(index, pathPoints.Length - 1);
            pathPoints[index] = pathPoints[i];
        }
    }

    void Update()
    {
        MoveOnPath();
    }

    void MoveOnPath()
    {
        // Move the drone along the generated path
        if (pathPoints != null && pathPoints.Length > 0)
        {
            Vector3 targetPosition = pathPoints[currentPointIndex];
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Check if the drone has reached the current target point
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                // Move to the next point in the path
                currentPointIndex = (currentPointIndex + 1) % pathPoints.Length;
            }
        }
    }

    // Draw the drone's path in the Scene view
    void OnDrawGizmos()
    {
        if (pathPoints != null)
        {
            Gizmos.color = Color.blue;

            // Draw lines connecting the path points
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
            }
        }
    }

    // Draw the area wireframe in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        // Draw a wireframe rectangle representing the area
        Vector3 center = areaCenter;
        Vector3 size = new Vector3(areaWidth, 0.1f, areaHeight);
        Gizmos.DrawWireCube(center, size);
    }
}
