using UnityEngine;
using UnityEngine.UI; // For working with UI elements
using TMPro; // Namespace for TextMeshPro

public class Display2UI : MonoBehaviour
{
    public EnvironmentSwitcher switcher; // Assign in the inspector
    public Button previousButton;
    public Button nextButton;
    public TMP_Text currentObjectText; // Use TMP_Text instead of UnityEngine.UI.Text

    private void Start()
    {
        // You might want to also update the button text if they're dynamically set. 
        // If so, ensure your buttons' text components are TextMeshPro components as well.

        // Subscribe to button click events
        previousButton.onClick.AddListener(Previous);
        nextButton.onClick.AddListener(Next);

        UpdateUI(); // Initial UI update
    }

    private void Previous()
    {
        switcher.SetActiveObject(Mathf.Max(0, switcher.currentIndex - 1));
        UpdateUI();
    }

    private void Next()
    {
        switcher.SetActiveObject(Mathf.Min(switcher.gameObjects.Length - 1, switcher.currentIndex + 1));
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Update the current object text using TMP_Text
        if (switcher.gameObjects != null && switcher.gameObjects.Length > 0 && switcher.currentIndex < switcher.gameObjects.Length)
        {
            GameObject currentGameObject = switcher.gameObjects[switcher.currentIndex];
            currentObjectText.text = "Current Object: " + (currentGameObject != null ? currentGameObject.name : "None selected");
        }
        else
        {
            currentObjectText.text = "Current Object: None available";
        }
    }
}
