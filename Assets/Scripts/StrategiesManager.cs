using UnityEngine;
using TMPro; // Import TextMesh Pro namespace
using Unity.VisualScripting;
using System.Collections.Generic;

[ExecuteInEditMode]
public class StrategiesManager : MonoBehaviour
{
    public List<GameObject> High;
    public List<GameObject> Low;

    public enum Strategy
    {
        Strategy1 = 1,
        Strategy2,
        Strategy3,
        Strategy4
    }

    public enum Environment
    {
        Environment1,
        Environment2,
        Environment3,
        Environment4
    }

    public Camera targetCamera;
    public GameObject drone; // Assign the drone GameObject
    public float pathInterval = 2f; // Distance interval to add path points
    private List<Vector3> pathPoints = new List<Vector3>();
    private LineRenderer lineRenderer;

    public GameObject S1;
    public GameObject S2;
    public GameObject S3;
    public GameObject S4;

    public List<GameObject> Environment1Objects;
    public List<GameObject> Environment2Objects;
    public List<GameObject> Environment3Objects;
    public List<GameObject> Environment4Objects;

    [SerializeField]
    private Strategy currentStrategy = Strategy.Strategy1;

    [SerializeField]
    private Environment currentEnvironment = Environment.Environment1;

    public TMP_Dropdown dropdown;
    public TMP_Text tmpText;

    public Vector3 strategy1Position;
    public Vector3 strategy2Position;
    public Vector3 strategy3Position;
    public Vector3 strategy4Position;

    private float lastPathPointTime;
    private Vector3 lastPathPosition;
    private bool timerRunning;
    private float timerDuration;
    private float timer;

    private void Start()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(DropdownValueChanged);
        }

        if (drone != null)
        {
            lineRenderer = drone.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogError("LineRenderer component missing on the drone.");
                return;
            }
            lineRenderer.positionCount = 0;
        }
        else
        {
            Debug.LogError("Drone GameObject is not assigned.");
        }

        ApplyStrategy();
        ApplyEnvironment();
    }

    public Strategy CurrentStrategy
    {
        get => currentStrategy;
        set
        {
            if (currentStrategy != value)
            {
                currentStrategy = value;
                ApplyStrategy();
                if (dropdown != null)
                {
                    dropdown.value = (int)currentStrategy - 1; // Update dropdown to reflect the current strategy
                }
            }
        }
    }

    public Environment CurrentEnvironment
    {
        get => currentEnvironment;
        set
        {
            if (currentEnvironment != value)
            {
                currentEnvironment = value;
                ApplyEnvironment();
            }
        }
    }

    private void OnEnable()
    {
        ApplyStrategy();
        ApplyEnvironment();
    }

    private void OnValidate()
    {
        if (Application.isEditor)
        {
            ApplyStrategy();
            ApplyEnvironment();
        }
    }

    private void ApplyStrategy()
    {
        switch (currentStrategy)
        {
            case Strategy.Strategy1:
                S1.SetActive(true);
                S2.SetActive(false);
                S3.SetActive(false);
                S4.SetActive(false);
                SetActiveGameObjects(High, false);
                SetActiveGameObjects(Low, false);
                if (tmpText != null)
                {
                    tmpText.rectTransform.localPosition = strategy1Position;
                }
                ResetPath();
                break;
            case Strategy.Strategy2:
                S1.SetActive(false);
                S2.SetActive(true);
                S3.SetActive(false);
                S4.SetActive(false);
                SetActiveGameObjects(High, false);
                SetActiveGameObjects(Low, false);
                if (tmpText != null)
                {
                    tmpText.rectTransform.localPosition = strategy2Position;
                }
                ResetPath();
                break;
            case Strategy.Strategy3:
                S1.SetActive(false);
                S2.SetActive(false);
                S3.SetActive(true);
                S4.SetActive(false);
                SetActiveGameObjects(High, false);
                SetActiveGameObjects(Low, true);
                if (tmpText != null)
                {
                    tmpText.rectTransform.localPosition = strategy3Position;
                }
                break;
            case Strategy.Strategy4:
                S1.SetActive(false);
                S2.SetActive(false);
                S3.SetActive(false);
                S4.SetActive(true);
                SetActiveGameObjects(High, true);
                SetActiveGameObjects(Low, false);
                if (tmpText != null)
                {
                    tmpText.rectTransform.localPosition = strategy1Position;
                }
                ResetPath();
                break;
            default:
                Debug.LogError("Unhandled strategy number.");
                break;
        }
    }

    private void ApplyEnvironment()
    {
        Debug.Log("Applying environment: " + currentEnvironment);

        SetActiveGameObjects(Environment1Objects, currentEnvironment == Environment.Environment1);
        SetActiveGameObjects(Environment2Objects, currentEnvironment == Environment.Environment2);
        SetActiveGameObjects(Environment3Objects, currentEnvironment == Environment.Environment3);
        SetActiveGameObjects(Environment4Objects, currentEnvironment == Environment.Environment4);

        Debug.Log("Environment1Objects active: " + (currentEnvironment == Environment.Environment1));
        Debug.Log("Environment2Objects active: " + (currentEnvironment == Environment.Environment2));
        Debug.Log("Environment3Objects active: " + (currentEnvironment == Environment.Environment3));
        Debug.Log("Environment4Objects active: " + (currentEnvironment == Environment.Environment4));
    }

    private void SetActiveGameObjects(List<GameObject> gameObjects, bool isActive)
    {
        foreach (GameObject obj in gameObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }
    }

    private void DropdownValueChanged(int index)
    {
        CurrentStrategy = (Strategy)(index + 1); // +1 to align dropdown index (0-based) with enum (1-based)
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            CurrentEnvironment = (Environment)(((int)currentEnvironment + 1) % System.Enum.GetValues(typeof(Environment)).Length);
            Debug.Log("Switched to environment: " + currentEnvironment);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            CurrentStrategy = (Strategy)(((int)currentStrategy % 4) + 1);
            Debug.Log("Switched to strategy: " + currentStrategy);
        }

        if (timerRunning)
        {
            timer += Time.deltaTime;
            if (timer >= timerDuration)
            {
                timerRunning = false;
                ResetPath();
            }
        }

        if (currentStrategy == Strategy.Strategy4)
        {
            TrackDronePath();
        }
    }

    private void TrackDronePath()
    {
        if (drone == null || lineRenderer == null)
        {
            return;
        }

        Vector3 currentPosition = drone.transform.position;
        if (Vector3.Distance(lastPathPosition, currentPosition) >= pathInterval)
        {
            pathPoints.Add(currentPosition);
            lineRenderer.positionCount = pathPoints.Count;
            lineRenderer.SetPositions(pathPoints.ToArray());
            lastPathPosition = currentPosition;
        }
    }

    public void StartTimer(float duration)
    {
        timerDuration = duration;
        timer = 0;
        timerRunning = true;
    }

    private void ResetPath()
    {
        if (drone == null || lineRenderer == null)
        {
            return;
        }

        pathPoints.Clear();
        lineRenderer.positionCount = 0;
        lastPathPosition = drone.transform.position;
    }
}
