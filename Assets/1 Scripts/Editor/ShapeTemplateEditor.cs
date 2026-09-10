using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShapeTemplate)), CanEditMultipleObjects]
public class ShapeTemplateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty baseLayerProp = serializedObject.FindProperty("baseLayer");

        GUILayout.Space(5);
        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Shape Base Layer: " + baseLayerProp.intValue, EditorStyles.boldLabel, GUILayout.Width(130));

        if (GUILayout.Button("-", GUILayout.Width(40), GUILayout.Height(25)))
        {
            baseLayerProp.intValue--;
        }
        if (GUILayout.Button("+", GUILayout.Width(40), GUILayout.Height(25)))
        {
            baseLayerProp.intValue++;
        }
        GUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        DrawDefaultInspector();
    }
}
