using UnityEngine;
using TMPro; // Import TextMesh Pro namespace
using Unity.VisualScripting;

[ExecuteInEditMode]
public class StrategiesManager : MonoBehaviour
{
    public enum Strategy
    {
        Strategy1 = 1,
        Strategy2,
        Strategy3,
        Strategy4
    }

    public GameObject LOW;
    public GameObject HIGH;
    public GameObject masterDrone;
    public Camera targetCamera;

    public GameObject S1;
    public GameObject S2;
    public GameObject S3;
    public GameObject S4;

    [SerializeField]
    private Strategy currentStrategy = Strategy.Strategy1;

    public TMP_Dropdown dropdown; // Reference to the TMP Dropdown

    private void Start()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(DropdownValueChanged);
        }
        ApplyStrategy();
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
            }
        }
    }

    private void OnEnable()
    {
        ApplyStrategy();
    }

    private void OnValidate()
    {
        if (Application.isEditor)
        {
            ApplyStrategy();
        }
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
            case Strategy.Strategy1:
                masterDrone.SetActive(false);
                S1.SetActive(true);
                S2.SetActive(false);
                S3.SetActive(false);
                S4.SetActive(false);
                break;
            case Strategy.Strategy2:
                masterDrone.SetActive(true);
                S1.SetActive(false);
                S2.SetActive(true);
                S3.SetActive(false);
                S4.SetActive(false);
                break;
            case Strategy.Strategy3:
                masterDrone.SetActive(false);
                LOW.SetActive(true);
                HIGH.SetActive(false);
                S1.SetActive(false);
                S2.SetActive(false);
                S3.SetActive(true);
                S4.SetActive(false);
                break;
            case Strategy.Strategy4:
                masterDrone.SetActive(false);
                LOW.SetActive(false);
                HIGH.SetActive(true);
                S1.SetActive(false);
                S2.SetActive(false);
                S3.SetActive(false);
                S4.SetActive(true);
                break;
            default:
                Debug.LogError("Unhandled strategy number.");
                break;
        }
    }

    // Method called when the TMP Dropdown value changes
    private void DropdownValueChanged(int index)
    {
        CurrentStrategy = (Strategy)(index + 1); // +1 to align dropdown index (0-based) with enum (1-based)
    }
}
