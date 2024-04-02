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
        }

        if (GUILayout.Button("Next"))
        {
            switcher.SetActiveObject(Mathf.Min(switcher.gameObjects.Length - 1, switcher.currentIndex + 1));
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
    }
}
#endif
