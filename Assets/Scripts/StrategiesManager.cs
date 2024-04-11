using UnityEngine;

[ExecuteInEditMode]
public class StrategiesManager : MonoBehaviour
{
    public GameObject LOW;
    public GameObject HIGH;

    public GameObject Littlebar;
    public GameObject BigBar;

    public GameObject masterDrone;
    public Camera targetCamera;

    [SerializeField]
    private int currentStrategy = 1;

    public int CurrentStrategy
    {
        get => currentStrategy;
        private set
        {
            currentStrategy = value;
            ApplyStrategy();
        }
    }

    [SerializeField]
    private float strategy2Width = 1f;

    private void OnEnable()
    {
        ApplyStrategy();
    }

    private void OnValidate()
    {
        if (Application.isEditor)
        {
            SetStrategy(currentStrategy); // Ensures ApplyStrategy is called upon Inspector changes
        }
    }

    public void SetStrategy(int strategyNumber)
    {
        if (strategyNumber < 1 || strategyNumber > 4)
        {
            Debug.LogError("Invalid strategy number. Please select a number between 1 and 4.");
            return;
        }
        CurrentStrategy = strategyNumber;
    }

    private void ApplyStrategy()
    {
        if (targetCamera == null || masterDrone == null || LOW == null || HIGH == null)
        {
            Debug.LogError("One or more GameObject references are not set in the StrategiesManager.");
            return;
        }

        switch (currentStrategy)
        {
            case 1:
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                masterDrone.SetActive(false);
                Littlebar.SetActive(false);
                BigBar.SetActive(false);
                break;
            case 2:
                targetCamera.rect = new Rect(0f, 0.5f, strategy2Width, 0.5f);
                masterDrone.SetActive(true);
                Littlebar.SetActive(false);
                BigBar.SetActive(true);
                break;
            case 3:
                targetCamera.rect = new Rect(-0.5f, 0f, 1f, 1f);
                masterDrone.SetActive(false);
                LOW.SetActive(false);
                HIGH.SetActive(true);
                Littlebar.SetActive(true);
                BigBar.SetActive(false);
                break;
            case 4:
                targetCamera.rect = new Rect(-0.5f, 0f, 1f, 1f);
                masterDrone.SetActive(false);
                LOW.SetActive(true);
                HIGH.SetActive(false);
                Littlebar.SetActive(true);
                BigBar.SetActive(false);
                break;
            default:
                Debug.LogError("Unhandled strategy number.");
                break;
        }
    }
}
