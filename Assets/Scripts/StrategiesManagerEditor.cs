using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StrategiesManager))]
public class StrategiesManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        StrategiesManager manager = (StrategiesManager)target;

        GUILayout.Label($"Current Strategy: {manager.CurrentStrategy}");

        if (GUILayout.Button("Set Strategy 1"))
        {
            manager.SetStrategy(1);
        }
        if (GUILayout.Button("Set Strategy 2"))
        {
            manager.SetStrategy(2);
        }
        if (GUILayout.Button("Set Strategy 3"))
        {
            manager.SetStrategy(3);
        }
        if (GUILayout.Button("Set Strategy 4"))
        {
            manager.SetStrategy(4);
        }
    }
}
