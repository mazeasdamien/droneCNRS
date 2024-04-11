using UnityEngine;

public class HeatmapGenerator : MonoBehaviour
{
    [Header("Mesh and Texture Settings")]
    public Renderer meshRenderer; // Assign the mesh's renderer with the heatmap texture
    public float meshScale = 1.591182f; // Adjustable scale of the mesh
    private Texture2D heatmapTexture;
    private int textureSize = 256; // Example size, adjust as needed

    [Header("Heatmap Colors")]
    public Color originalColor = Color.black; // Starting color of the heatmap
    public Color targetColor = Color.red; // Color representing maximum "heat"

    void Start()
    {
        // Initialize the heatmap texture
        heatmapTexture = new Texture2D(textureSize, textureSize);
        ClearHeatmap();
        meshRenderer.material.mainTexture = heatmapTexture;
    }

    void Update()
    {
        // Example: Convert the GameObject's position to texture coordinates
        Vector2 pos = new Vector2(transform.position.x, transform.position.z); // Adjust based on your setup
        Vector2 textureCoord = PositionToTextureCoord(pos);

        // Update the heatmap texture at this position
        IncreaseHeatAt(textureCoord);
    }

    void ClearHeatmap()
    {
        for (int x = 0; x < heatmapTexture.width; x++)
        {
            for (int y = 0; y < heatmapTexture.height; y++)
            {
                heatmapTexture.SetPixel(x, y, originalColor); // Use the original color for the clear state
            }
        }
        heatmapTexture.Apply();
    }

    Vector2 PositionToTextureCoord(Vector2 position)
    {
        float meshSizeWorldUnits = 10.0f * meshScale; // Adjust based on your actual mesh size

        float relativeX = (position.x / meshSizeWorldUnits) + 0.5f;
        float relativeY = (position.y / meshSizeWorldUnits) + 0.5f;

        int textureX = (int)(relativeX * heatmapTexture.width);
        int textureY = (int)(relativeY * heatmapTexture.height);

        return new Vector2(Mathf.Clamp(textureX, 0, heatmapTexture.width - 1),
                           Mathf.Clamp(textureY, 0, heatmapTexture.height - 1));
    }

    void IncreaseHeatAt(Vector2 coord)
    {
        int x = (int)coord.x;
        int y = (int)coord.y;

        if (x >= 0 && y >= 0 && x < heatmapTexture.width && y < heatmapTexture.height)
        {
            Color currentColor = heatmapTexture.GetPixel(x, y);
            // Lerp towards the target color based on some condition, e.g., over time, or based on proximity
            Color newColor = Color.Lerp(currentColor, targetColor, 0.01f); // Adjust the lerp factor as needed
            heatmapTexture.SetPixel(x, y, newColor);
            heatmapTexture.Apply();
        }
    }
}
