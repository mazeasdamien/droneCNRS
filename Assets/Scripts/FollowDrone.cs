using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FollowDrone : MonoBehaviour
{
    public static FollowDrone Instance { get; private set; } // Singleton instance
    public Transform droneTransform;
    public RawImage rawImage;
    public Camera mainCamera;
    public LineRenderer lineRenderer;

    private List<Vector3> positions = new List<Vector3>();  // List to store positions

    void Awake()
    {
        Instance = this; // Assign the singleton instance
    }

    void Start()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        // Configure the line renderer to use the UI material and potentially adjust other settings
        lineRenderer.material = new Material(Shader.Find("UI/Default"));  // Make sure this shader works with UI if not find an appropriate one
        lineRenderer.useWorldSpace = false;  // Use local space which corresponds to the canvas space
    }

    void Update()
    {
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(droneTransform.position);
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rawImage.canvas.transform,
            screenPosition,
            rawImage.canvas.worldCamera,
            out canvasPosition
        );

        rawImage.rectTransform.anchoredPosition = canvasPosition;

        if (positions.Count == 0 || Vector3.Distance(positions[positions.Count - 1], canvasPosition) > 1f)
        {
            positions.Add(canvasPosition);  // Store canvas position instead of world position

            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ConvertAll(p => (Vector3)p).ToArray());
        }
    }
}
