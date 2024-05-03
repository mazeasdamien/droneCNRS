using UnityEngine;
using UnityEngine.UI;
using Tobii.Research.Unity;

public class GazeCursorController : MonoBehaviour
{
    public Canvas canvas;
    public RawImage left;
    public RawImage right;

    void Update()
    {
        EyeTracker eyeTracker = EyeTracker.Instance;
        MoveRawImage(left, eyeTracker.NextData.Left.GazePointOnDisplayArea);
        MoveRawImage(right, eyeTracker.NextData.Right.GazePointOnDisplayArea);
    }

    private void MoveRawImage(RawImage image, Vector2 normalizedPosition)
    {
        Vector2 screenPosition = new Vector2(normalizedPosition.x * Screen.width, (1 - normalizedPosition.y) * Screen.height);
        Vector2 canvasPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screenPosition, canvas.worldCamera, out canvasPosition))
        {
            image.rectTransform.anchoredPosition = canvasPosition;
        }
    }
}
