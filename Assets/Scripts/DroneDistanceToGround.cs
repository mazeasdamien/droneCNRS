using UnityEngine;
using TMPro;

public class DroneDistanceToGround : MonoBehaviour
{
    public TextMeshProUGUI distanceText;
    private Timer timerScript;

    void Start()
    {
        timerScript = FindObjectOfType<Timer>();
    }

    void Update()
    {
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = Vector3.down;
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit))
        {
            if (hit.collider.CompareTag("environment"))
            {
                float distance = hit.distance;
                distanceText.text = $"{distance:F2} meters";
                if (timerScript.IsTimerRunning())
                {
                    timerScript.RecordVisitedSquare(rayOrigin);
                }
            }
        }
        else
        {
            distanceText.text = "0000";
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 100f);
    }
}
