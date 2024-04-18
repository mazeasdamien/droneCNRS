using UnityEngine;
using TMPro; // Namespace for TextMesh Pro components

public class EnvironmentSwitcher : MonoBehaviour
{
    // Enum to represent the environments
    public enum Environment
    {
        Environment1,
        Environment2,
        Environment3,
        Environment4
    }

    public TMP_Dropdown dropdown; // Reference to the TMP Dropdown
    public GameObject[] gameObjects; // Array of environment GameObjects
    private int currentIndex = 0; // Internal variable to keep track of the current index

    void Start()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(dropdown); });
        }
        // Optionally set the default selection from the current environment
        dropdown.value = currentIndex;
        SetActiveObject(dropdown.value);
    }

    void SetActiveObject(int index)
    {
        currentIndex = index;
        UpdateActiveGameObject();
    }

    private void UpdateActiveGameObject()
    {
        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] != null)
            {
                gameObjects[i].SetActive(i == currentIndex);
            }
        }
    }

    // Handler for when the dropdown value changes
    void DropdownValueChanged(TMP_Dropdown change)
    {
        SetActiveObject(change.value);
    }
}
