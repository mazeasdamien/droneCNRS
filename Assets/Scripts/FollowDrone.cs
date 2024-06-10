using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FollowDrone : MonoBehaviour
{
    public static FollowDrone Instance { get; private set; }
    public Transform droneTransform;
    public RawImage rawImage;
    public Camera mainCamera;
    public LineRenderer lineRenderer;

    private List<Vector3> positions = new List<Vector3>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        lineRenderer.material = new Material(Shader.Find("UI/Default"));
        lineRenderer.useWorldSpace = false;
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
            positions.Add(canvasPosition);
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ConvertAll(p => (Vector3)p).ToArray());
        }
    }
}