using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DatasetObjectInfo))]
public class DatasetObjectInfoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DatasetObjectInfo script = (DatasetObjectInfo)target;


        if (GUILayout.Button("Clear test points"))
        {
            script.testPoints = null;
        }
        if (GUILayout.Button("Regenerate uniform points"))
        {
            script.GenerateTestPointsUniform(script.testPointsNum);
            EditorUtility.DisplayDialog("Generation", "Done regenerating test points", "0k");
        }
        if (GUILayout.Button("Regenerate non-uniform points"))
        {
            script.GenerateTestPointsNonUniform(script.testPointsNum);
            EditorUtility.DisplayDialog("Generation", "Done regenerating test points", "0k");
        }
        if (script.testPoints == null || script.testPoints.Count == 0)
            EditorGUILayout.HelpBox("Test points should be generated", MessageType.Info);
    }
}
