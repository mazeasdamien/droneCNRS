using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StrategiesManager))]
public class StrategiesManagerEditor : Editor
{
    private const string StrategyPrefKey = "StrategiesManager_CurrentStrategy";

    void OnEnable()
    {
        // Load the current strategy from EditorPrefs when the editor script is enabled/loaded
        int savedStrategy = EditorPrefs.GetInt(StrategyPrefKey, 1); // Default to 1
        StrategiesManager manager = (StrategiesManager)target;
        manager.SetStrategy(savedStrategy);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        StrategiesManager manager = (StrategiesManager)target;

        GUILayout.Label($"Current Strategy: {manager.CurrentStrategyName}");

        if (GUILayout.Button("Previous") || GUILayout.Button("Next"))
        {
            Undo.RecordObject(manager, "Change Strategy");

            if (GUILayout.Button("Previous"))
            {
                manager.SetStrategy(manager.CurrentStrategy - 1 < 1 ? 3 : manager.CurrentStrategy - 1);
            }
            else // Next button was pressed
            {
                manager.SetStrategy(manager.CurrentStrategy + 1 > 3 ? 1 : manager.CurrentStrategy + 1);
            }

            EditorPrefs.SetInt(StrategyPrefKey, manager.CurrentStrategy);
            EditorUtility.SetDirty(manager);
        }
    }
}
