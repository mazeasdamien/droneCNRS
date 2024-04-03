using UnityEngine;

public class StrategiesManager : MonoBehaviour
{
    public GameObject masterDrone;
    public Camera targetCamera;
    [SerializeField]
    private int currentStrategy = 1; // Serialized to ensure Unity saves changes.

    // Strategy names for display purposes
    private string[] strategyNames = { "Strategy 1", "Strategy 2", "Strategy 3" };

    // Public getter for read-only access outside, private setter for internal use
    public int CurrentStrategy
    {
        get => currentStrategy;
        private set
        {
            currentStrategy = value;
            ApplyStrategy();
        }
    }

    // Missing 'strategy2Width' field added for completeness
    [SerializeField]
    private float strategy2Width = 1f; // 'A' - Adjustable width for Strategy 2 in the Inspector

    public string CurrentStrategyName => strategyNames[currentStrategy - 1];

    private void OnEnable()
    {
        ApplyStrategy();
    }

    public void SetStrategy(int strategyNumber)
    {
        if (strategyNumber < 1 || strategyNumber > 3)
        {
            Debug.LogError("Invalid strategy number. Please select a number between 1 and 3.");
            return;
        }

        CurrentStrategy = strategyNumber;
    }

    private void ApplyStrategy()
    {
        switch (CurrentStrategy)
        {
            case 1:
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                masterDrone.SetActive(false);
                break;
            case 2:
                targetCamera.rect = new Rect(0f, 0.5f, strategy2Width, 0.5f);
                masterDrone.SetActive(true);
                break;
            case 3:
                targetCamera.rect = new Rect(-0.5f, 0f, 1f, 1f);
                masterDrone.SetActive(false);
                break;
            default:
                Debug.LogError("Unhandled strategy number.");
                break;
        }
    }

}
