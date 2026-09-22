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
        SerializedProperty typeProp = serializedObject.FindProperty("type");
        SerializedProperty extraTypeProp = serializedObject.FindProperty("extraType");
        SerializedProperty obstacleProp = serializedObject.FindProperty("obstacle");
        SerializedProperty showFaceProp = serializedObject.FindProperty("showFaceDetails");

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

        // Suit Property with +/-
        DrawEnumProp("Suit", suitProp);

        // Rank Property with +/-
        DrawEnumProp("Rank", rankProp);

        // Type Property with +/-
        DrawEnumProp("Type", typeProp);

        if (typeProp.enumValueIndex == (int)CardType.Extra)
        {
            DrawEnumProp("Extra Type", extraTypeProp);
        }

        // Obstacle Property with +/-
        DrawEnumProp("Obstacle", obstacleProp);

        serializedObject.ApplyModifiedProperties();

        DrawTutorialStepSection();

        GUILayout.Space(10);
        DrawDefaultInspector();
    }

    private void DrawTutorialStepSection()
    {
        CardGizmo firstGizmo = (CardGizmo)target;
        if (firstGizmo == null) return;

        TutorialDrawPile pile = firstGizmo.GetComponentInParent<TutorialDrawPile>();
        if (pile != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox($"This card is inside Draw Pile ({firstGizmo.gameObject.name}). Changes to its Suit/Rank/Type will automatically save to DrawPile data. To trigger this in tutorial steps, use '+ Add Draw Pile' in Tutorial Map Tool.", MessageType.Info);
            if (GUILayout.Button("Select Draw Pile Parent"))
            {
                Selection.activeGameObject = pile.gameObject;
            }
            return;
        }

        TutorialCheckCard checkCard = firstGizmo.GetComponentInParent<TutorialCheckCard>();
        if (checkCard != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("This card is the Check Card (Foundation starting card). Changes will automatically save to CheckCard data.", MessageType.Info);
            if (GUILayout.Button("Select Check Card Parent"))
            {
                Selection.activeGameObject = checkCard.gameObject;
            }
            return;
        }

        TutorialMapTool tool = firstGizmo.GetTutorialMapTool();
        if (tool == null) return;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Tutorial Step Configuration:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");

        if (targets.Length == 1)
        {
            if (firstGizmo.tutorialStep > 0)
            {
                EditorGUILayout.HelpBox($"This card is Step #{firstGizmo.tutorialStep} of {tool.tutorialSteps.Count} in Tutorial Sequence", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = firstGizmo.tutorialStep > 1;
                if (GUILayout.Button("▲ Step Earlier", GUILayout.Height(26)))
                {
                    int curIdx = firstGizmo.tutorialStep - 1;
                    tool.MoveStep(curIdx, curIdx - 1);
                }
                GUI.enabled = firstGizmo.tutorialStep < tool.tutorialSteps.Count;
                if (GUILayout.Button("▼ Step Later", GUILayout.Height(26)))
                {
                    int curIdx = firstGizmo.tutorialStep - 1;
                    tool.MoveStep(curIdx, curIdx + 1);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3);
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Remove From Tutorial Steps", GUILayout.Height(26)))
                {
                    tool.RemoveCardStep(firstGizmo);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                int nextStep = tool.GetNextStepNumber();
                GUI.backgroundColor = new Color(0.6f, 0.95f, 0.6f);
                if (GUILayout.Button($"+ Set as Next Step (Step #{nextStep})", GUILayout.Height(34)))
                {
                    tool.AddCardStep(firstGizmo);
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            int newStep = EditorGUILayout.IntField("Set Specific Step #", firstGizmo.tutorialStep);
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                if (newStep <= 0)
                    tool.RemoveCardStep(firstGizmo);
                else
                    tool.SetCardStep(firstGizmo, newStep);
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.LabelField($"Selected {targets.Length} cards", EditorStyles.miniBoldLabel);

            GUI.backgroundColor = new Color(0.6f, 0.95f, 0.6f);
            if (GUILayout.Button($"+ Assign Steps to Selected Cards (in order)", GUILayout.Height(34)))
            {
                foreach (var obj in targets)
                {
                    CardGizmo g = (CardGizmo)obj;
                    if (g != null)
                    {
                        tool.AddCardStep(g);
                    }
                }
            }

            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("Remove Selected Cards from Steps", GUILayout.Height(26)))
            {
                foreach (var obj in targets)
                {
                    CardGizmo g = (CardGizmo)obj;
                    if (g != null)
                    {
                        tool.RemoveCardStep(g);
                    }
                }
            }
            GUI.backgroundColor = Color.white;
        }

        GUILayout.Space(6);
        if (GUILayout.Button("⧉ Open Tutorial Map Tab (Dockable)", GUILayout.Height(24)))
        {
            TutorialMapToolWindow.ShowWindow();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEnumProp(string label, SerializedProperty prop)
    {
        GUILayout.BeginHorizontal("box");
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(80));

        if (GUILayout.Button("-", GUILayout.Width(35), GUILayout.Height(20)))
        {
            if (prop.enumValueIndex > 0)
            {
                prop.enumValueIndex--;
            }
            else
            {
                prop.enumValueIndex = prop.enumNames.Length - 1;
            }
        }

        EditorGUILayout.PropertyField(prop, GUIContent.none);

        if (GUILayout.Button("+", GUILayout.Width(35), GUILayout.Height(20)))
        {
            if (prop.enumValueIndex < prop.enumNames.Length - 1)
            {
                prop.enumValueIndex++;
            }
            else
            {
                prop.enumValueIndex = 0;
            }
        }
        GUILayout.EndHorizontal();
    }
}
