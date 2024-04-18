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
    public GameObject Littlebar;
    public GameObject BigBar;
    public GameObject masterDrone;
    public Camera targetCamera;
    public GameObject cameraMAP;
    public GameObject map;
    public GameObject backgroundS2;
    public GameObject backgroundS34;

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
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                masterDrone.SetActive(false);
                Littlebar.SetActive(false);
                BigBar.SetActive(false);
                cameraMAP.SetActive(false);
                backgroundS2.SetActive(false);
                backgroundS34.SetActive(false);
                break;
            case Strategy.Strategy2:
                targetCamera.rect = new Rect(0.25f, 0f, 0.5f, 0.5f);
                masterDrone.SetActive(true);
                Littlebar.SetActive(false);
                BigBar.SetActive(true);
                cameraMAP.SetActive(false);
                map.SetActive(false);
                backgroundS2.SetActive(true);
                backgroundS34.SetActive(false);
                break;
            case Strategy.Strategy3:
                targetCamera.rect = new Rect(-0.76f, 0f, 1.26f, 0.5f);
                masterDrone.SetActive(false);
                LOW.SetActive(true);
                HIGH.SetActive(false);
                Littlebar.SetActive(true);
                BigBar.SetActive(false);
                cameraMAP.SetActive(true);
                map.SetActive(true);
                backgroundS2.SetActive(false);
                backgroundS34.SetActive(true);
                break;
            case Strategy.Strategy4:
                targetCamera.rect = new Rect(-0.76f, 0f, 1.26f, 0.5f);
                masterDrone.SetActive(false);
                LOW.SetActive(false);
                HIGH.SetActive(true);
                Littlebar.SetActive(true);
                BigBar.SetActive(false);
                cameraMAP.SetActive(true);
                map.SetActive(true);
                backgroundS2.SetActive(false);
                backgroundS34.SetActive(true);
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
