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
    public Camera targetCamera;

    public GameObject S1;
    public GameObject S2;
    public GameObject S3;
    public GameObject S4;

    [SerializeField]
    private Strategy currentStrategy = Strategy.Strategy1;

    public TMP_Dropdown dropdown; // Reference to the TMP Dropdown
    public TMP_Text tmpText; // Reference to the TMP_Text object

    public Vector3 strategy1Position; // Position for Strategy1
    public Vector3 strategy2Position; // Position for Strategy2
    public Vector3 strategy3Position; // Position for Strategy3
    public Vector3 strategy4Position; // Position for Strategy4

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
        switch (currentStrategy)
        {
            case Strategy.Strategy1:
                S1.SetActive(true);
                S2.SetActive(false);
                S3.SetActive(false);
                S4.SetActive(false);
                foreach (GameObject g in High)
                {
                    g.SetActive(false);
                }
                foreach (GameObject g in Low)
                {
                    g.SetActive(false);
                }
                if (tmpText != null)
                {
                    tmpText.rectTransform.localPosition = strategy1Position;
                }
                break;
            case Strategy.Strategy2:
                S1.SetActive(false);
                S2.SetActive(true);
                S3.SetActive(false);
                S4.SetActive(false);
                foreach (GameObject g in High)
                {
                    g.SetActive(false);
                }
                foreach (GameObject g in Low)
                {
                    g.SetActive(false);
                }
                if (tmpText != null)
                {
                    tmpText.rectTransform.localPosition = strategy2Position;
                }
                break;
            case Strategy.Strategy3:
                S1.SetActive(false);
                S2.SetActive(false);
                S3.SetActive(true);
                S4.SetActive(false);
                foreach (GameObject g in High)
                {
                    g.SetActive(false);
                }
                foreach (GameObject g in Low)
                {
                    g.SetActive(true);
                }
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
                foreach (GameObject g in High)
                {
                    g.SetActive(true);
                }
                foreach (GameObject g in Low)
                {
                    g.SetActive(false);
                }
                if (tmpText != null)
                {
                    tmpText.rectTransform.localPosition = strategy4Position;
                }
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
