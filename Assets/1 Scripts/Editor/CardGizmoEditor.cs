using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CardGizmo)), CanEditMultipleObjects]
public class CardGizmoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty layerProp = serializedObject.FindProperty("layer");

        GUILayout.Space(5);
        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Card Layer: " + layerProp.intValue, EditorStyles.boldLabel, GUILayout.Width(100));

        if (GUILayout.Button("-", GUILayout.Width(40), GUILayout.Height(25)))
        {
            layerProp.intValue--;
        }
        if (GUILayout.Button("+", GUILayout.Width(40), GUILayout.Height(25)))
        {
            layerProp.intValue++;
        }
        GUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        DrawDefaultInspector();
    }
}
