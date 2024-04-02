using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for UI components like RawImage

public class CameraManager : MonoBehaviour
{
    public Camera targetCamera; // The camera to move
    public float scrollSpeed = 10f; // Speed at which the camera moves on scroll
    public float minY = 20f; // Minimum Y position threshold
    public float maxY = 100f; // Maximum Y position threshold
    public RawImage[] rawImages; // Array to hold the RawImage components

    private bool isScrolling = false; // Track if the user is scrolling
    private Coroutine colorChangeCoroutine; // To keep track of the coroutine
    public GameObject movableGameObject; // Reference to the other GameObject to move

    // Define the Y position range for the movableGameObject
    private float minMovableY = -0.07f;
    private float maxMovableY = 0.041f;

    void Update()
    {
        if (Input.mousePosition.x > Screen.width / 2 && Input.mousePosition.y < Screen.height / 2)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0)
            {
                if (!isScrolling)
                {
                    isScrolling = true;
                    UpdateRawImageColors(Color.green);
                    if (colorChangeCoroutine != null)
                    {
                        StopCoroutine(colorChangeCoroutine);
                    }
                }

                float proposedY = targetCamera.transform.localPosition.y + (scroll * scrollSpeed);
                proposedY = Mathf.Clamp(proposedY, minY, maxY);

                Vector3 newPosition = new Vector3(targetCamera.transform.localPosition.x, proposedY, targetCamera.transform.localPosition.z);
                targetCamera.transform.localPosition = newPosition;

                // Calculate and apply the new Y position for the movableGameObject based on the camera's Y position
                UpdateMovableGameObjectYPosition(proposedY);
            }
            else if (isScrolling)
            {
                isScrolling = false;
                colorChangeCoroutine = StartCoroutine(WaitAndChangeColor(Color.white));
            }
        }
        else if (isScrolling)
        {
            isScrolling = false;
            if (colorChangeCoroutine != null)
            {
                StopCoroutine(colorChangeCoroutine);
            }
            colorChangeCoroutine = StartCoroutine(WaitAndChangeColor(Color.white));
        }
    }

    void UpdateMovableGameObjectYPosition(float cameraYPosition)
    {
        // Calculate the proportion of the camera's Y position within its range
        float proportion = (cameraYPosition - minY) / (maxY - minY);
        // Use that proportion to find the corresponding Y position for the movableGameObject within its range
        float newY = Mathf.Lerp(minMovableY, maxMovableY, proportion);
        // Apply the new Y position to the movableGameObject
        movableGameObject.transform.localPosition = new Vector3(movableGameObject.transform.localPosition.x, newY, movableGameObject.transform.localPosition.z);
    }

    IEnumerator WaitAndChangeColor(Color color)
    {
        yield return new WaitForSeconds(0.5f);
        UpdateRawImageColors(color);
    }

    void UpdateRawImageColors(Color color)
    {
        foreach (var rawImage in rawImages)
        {
            rawImage.color = color;
        }
    }
}
