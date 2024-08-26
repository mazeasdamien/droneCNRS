using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GridGenerator : MonoBehaviour
{
    public int gridSize = 100;
    public float squareSize = 1.0f;
    public Color gridColor = Color.green;

    [HideInInspector]
    public int numberOfSquares;

    [Header("Grid Info")]
    public string gridSizeInfo;

    private void OnValidate()
    {
        numberOfSquares = (int)(gridSize / squareSize) * (int)(gridSize / squareSize);
        gridSizeInfo = $"Grid Size: {gridSize}x{gridSize}, Square Size: {squareSize}, Number of Squares: {numberOfSquares}";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gridColor;
        Vector3 origin = transform.position;

        int numberOfLines = Mathf.CeilToInt(gridSize / squareSize);

        for (int x = 0; x <= numberOfLines; x++)
        {
            Vector3 startX = origin + new Vector3(x * squareSize, 0, 0);
            Vector3 endX = origin + new Vector3(x * squareSize, 0, gridSize);
            Gizmos.DrawLine(startX, endX);
        }

        for (int z = 0; z <= numberOfLines; z++)
        {
            Vector3 startZ = origin + new Vector3(0, 0, z * squareSize);
            Vector3 endZ = origin + new Vector3(gridSize, 0, z * squareSize);
            Gizmos.DrawLine(startZ, endZ);
        }
    }

    public Vector2Int GetGridSquare(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / squareSize);
        int z = Mathf.FloorToInt(position.z / squareSize);
        return new Vector2Int(x, z);
    }
}
