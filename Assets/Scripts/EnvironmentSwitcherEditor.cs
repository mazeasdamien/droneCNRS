#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnvironmentSwitcher))]
public class EnvironmentSwitcherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EnvironmentSwitcher switcher = (EnvironmentSwitcher)target;

        if (GUILayout.Button("Previous"))
        {
            switcher.SetActiveObject(Mathf.Max(0, switcher.currentIndex - 1));
            SaveCurrentIndex(switcher, switcher.currentIndex);
        }

        if (GUILayout.Button("Next"))
        {
            switcher.SetActiveObject(Mathf.Min(switcher.gameObjects.Length - 1, switcher.currentIndex + 1));
            SaveCurrentIndex(switcher, switcher.currentIndex);
        }

        // Display the name of the currently active GameObject
        if (switcher.gameObjects != null && switcher.gameObjects.Length > 0 && switcher.currentIndex < switcher.gameObjects.Length)
        {
            GameObject currentGameObject = switcher.gameObjects[switcher.currentIndex];
            if (currentGameObject != null)
            {
                EditorGUILayout.LabelField("Current Object:", currentGameObject.name);
            }
            else
            {
                EditorGUILayout.LabelField("Current Object:", "None selected");
            }
        }
        else
        {
            EditorGUILayout.LabelField("Current Object:", "None available");
        }

        LoadAndApplyIndex(switcher);
    }

    private void SaveCurrentIndex(EnvironmentSwitcher switcher, int index)
    {
        EditorPrefs.SetInt(switcher.GetInstanceID() + "_currentIndex", index);
    }

    private void LoadAndApplyIndex(EnvironmentSwitcher switcher)
    {
        if (EditorPrefs.HasKey(switcher.GetInstanceID() + "_currentIndex"))
        {
            int savedIndex = EditorPrefs.GetInt(switcher.GetInstanceID() + "_currentIndex");
            switcher.SetActiveObject(savedIndex); // Make sure this doesn't cause issues by being called repeatedly
        }
    }
}
#endif
