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

        if (GUILayout.Button("Clear test points") && EditorUtility.DisplayDialog("Confirmation", "Do you want to clear test points?", "Clear", "Cancel"))
        {
            script.testPoints = null;
        }

        if (GUILayout.Button("Regenerate uniform points") && EditorUtility.DisplayDialog("Confirmation", "Do you want to regenerate?", "Regenerate", "Cancel"))
        {
            script.GenerateTestPointsUniform(script.testPointsNum);
            EditorUtility.DisplayDialog("Generation", "Done regenerating test points", "0k");
        }

        if (GUILayout.Button("Regenerate non-uniform points") && EditorUtility.DisplayDialog("Confirmation", "Do you want to regenerate?", "Regenerate", "Cancel"))
        {
            script.GenerateTestPointsNonUniform(script.testPointsNum);
            EditorUtility.DisplayDialog("Generation", "Done regenerating test points", "0k");
        }

        if (script.testPoints == null || script.testPoints.Count == 0)
            EditorGUILayout.HelpBox("Test points should be generated", MessageType.Info);

        int triangles = 0;
        foreach (MeshFilter meshFilter in script.gameObject.GetComponentsInChildren<MeshFilter>())
            triangles += meshFilter.sharedMesh.triangles.Length;
        if (triangles > 9000)
        EditorGUILayout.HelpBox("Triangles count over 9000: " + triangles + "\nPregeneration is essential!" , MessageType.Warning);
    }
}
