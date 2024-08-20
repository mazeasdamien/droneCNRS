using UnityEngine;

[ExecuteInEditMode]
public class CubeGridVisualizer : MonoBehaviour
{
    public Vector3 environmentSize = new Vector3(300, 100, 300); // Size of the environment
    public Vector3 cubeSize = new Vector3(10, 10, 10); // Size of each cube
    public Color gridColor = Color.green; // Color of the grid visualization

    private void OnDrawGizmos()
    {
        // Calculate the number of cubes along each axis
        int cubesX = Mathf.FloorToInt(environmentSize.x / cubeSize.x);
        int cubesY = Mathf.FloorToInt(environmentSize.y / cubeSize.y);
        int cubesZ = Mathf.FloorToInt(environmentSize.z / cubeSize.z);

        // Iterate through each cube position in the grid
        for (int x = 0; x < cubesX; x++)
        {
            for (int y = 0; y < cubesY; y++)
            {
                for (int z = 0; z < cubesZ; z++)
                {
                    // Calculate the position of the current cube
                    Vector3 cubePosition = new Vector3(x * cubeSize.x, y * cubeSize.y, z * cubeSize.z) + cubeSize / 2;

                    // Offset the cube position to match the GameObject's position
                    cubePosition += transform.position;

                    // Draw the wireframe for this cube
                    Gizmos.color = gridColor;
                    Gizmos.DrawWireCube(cubePosition, cubeSize);
                }
            }
        }
    }
}
