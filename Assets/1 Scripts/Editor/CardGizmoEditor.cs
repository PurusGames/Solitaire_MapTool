using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CardGizmo)), CanEditMultipleObjects]
public class CardGizmoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty layerProp = serializedObject.FindProperty("layer");
        SerializedProperty suitProp = serializedObject.FindProperty("suit");
        SerializedProperty rankProp = serializedObject.FindProperty("rank");
        SerializedProperty showFaceProp = serializedObject.FindProperty("showFaceDetails");

        GUILayout.Space(5);

        // show/hide rank & suit
        GUI.backgroundColor = showFaceProp.boolValue ? Color.green : Color.gray;
        if (GUILayout.Button(showFaceProp.boolValue ? "Show Rank & Suit" : "Hide Rank & Suit", GUILayout.Height(30)))
        {
            showFaceProp.boolValue = !showFaceProp.boolValue;
        }
        // return bg color
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        // Layer Property with +/-
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

        // Chỉ hiển thị các nút chọn Suit và Rank nếu đang bật chế độ Show Face (hoặc bạn có thể chọn luôn hiển thị)
        // Dưới đây cho phép luôn hiển thị các nút bất kể trạng thái ẩn/hiện để dễ config trước.
        
        // Suit Property with +/-
        DrawEnumProp("Suit", suitProp);

        // Rank Property with +/-
        DrawEnumProp("Rank", rankProp);

        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);
        DrawDefaultInspector();
    }

    private void DrawEnumProp(string label, SerializedProperty prop)
    {
        GUILayout.BeginHorizontal("box");
        
        string displayValue = "Mixed...";
        if (!prop.hasMultipleDifferentValues && prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumDisplayNames.Length)
        {
            displayValue = prop.enumDisplayNames[prop.enumValueIndex];
        }

        GUILayout.Label(label + ": " + displayValue, EditorStyles.boldLabel, GUILayout.Width(100));

        if (GUILayout.Button("-", GUILayout.Width(40), GUILayout.Height(25)))
        {
            int maxIndex = prop.enumNames.Length - 1;
            prop.enumValueIndex = prop.enumValueIndex <= 0 ? maxIndex : prop.enumValueIndex - 1;
        }
        if (GUILayout.Button("+", GUILayout.Width(40), GUILayout.Height(25)))
        {
            int maxIndex = prop.enumNames.Length - 1;
            prop.enumValueIndex = prop.enumValueIndex >= maxIndex ? 0 : prop.enumValueIndex + 1;
        }

        GUILayout.EndHorizontal();
    }
}
