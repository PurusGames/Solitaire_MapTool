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
        SerializedProperty faceUpProp = serializedObject.FindProperty("faceUp");

        GUILayout.Space(5);

        // Face Up Button & Toggle
        EditorGUILayout.BeginVertical("box");
        Color activeColor = new Color(0.2f, 0.88f, 0.4f);
        Color inactiveColor = new Color(0.85f, 0.85f, 0.85f);

        bool allFaceUp = true;
        bool anyFaceUp = false;
        foreach (var t in targets)
        {
            CardGizmo g = (CardGizmo)t;
            if (g != null)
            {
                if (g.faceUp) anyFaceUp = true;
                else allFaceUp = false;
            }
        }

        GUI.backgroundColor = anyFaceUp ? activeColor : inactiveColor;
        string faceUpBtnLabel = (targets.Length > 1)
            ? (allFaceUp ? "✔ All Selected: FACE UP" : (anyFaceUp ? "~ Mixed (Click: Turn All UP)" : "✖ All Selected: FACE DOWN"))
            : (faceUpProp.boolValue ? "✔ Card is FACE UP" : "✖ Card is FACE DOWN (Default)");

        if (GUILayout.Button(faceUpBtnLabel, GUILayout.Height(30)))
        {
            Undo.RecordObjects(targets, "Toggle Face Up");
            bool targetVal = !allFaceUp;
            foreach (var t in targets)
            {
                CardGizmo g = (CardGizmo)t;
                if (g != null)
                {
                    g.faceUp = targetVal;
                    EditorUtility.SetDirty(g);
                }
            }
            faceUpProp.boolValue = targetVal;
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(faceUpProp, new GUIContent("Face Up"));
        if (GUILayout.Button("Toggle", GUILayout.Width(70)))
        {
            Undo.RecordObjects(targets, "Toggle Face Up");
            bool targetVal = !allFaceUp;
            foreach (var t in targets)
            {
                CardGizmo g = (CardGizmo)t;
                if (g != null)
                {
                    g.faceUp = targetVal;
                    EditorUtility.SetDirty(g);
                }
            }
            faceUpProp.boolValue = targetVal;
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

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

    private void OnSceneGUI()
    {
        CardGizmo gizmo = (CardGizmo)target;
        if (gizmo == null) return;

        // Position a small, convenient Scene GUI button near the card
        Vector3 screenPos = HandleUtility.WorldToGUIPoint(gizmo.transform.position + gizmo.transform.up * 1.35f);

        Handles.BeginGUI();
        try
        {
            Rect btnRect = new Rect(screenPos.x - 45, screenPos.y - 12, 90, 24);
            GUI.backgroundColor = gizmo.faceUp ? new Color(0.2f, 0.9f, 0.4f, 0.95f) : new Color(0.2f, 0.2f, 0.2f, 0.85f);
            string label = gizmo.faceUp ? "✔ Face UP" : "Face DOWN";
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
            btnStyle.normal.textColor = gizmo.faceUp ? Color.black : Color.white;

            if (GUI.Button(btnRect, label, btnStyle))
            {
                Undo.RecordObjects(targets, "Toggle Face Up");
                bool newVal = !gizmo.faceUp;
                foreach (var t in targets)
                {
                    CardGizmo g = (CardGizmo)t;
                    if (g != null)
                    {
                        g.faceUp = newVal;
                        EditorUtility.SetDirty(g);
                    }
                }
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
        }
        finally
        {
            Handles.EndGUI();
        }
    }

    private void DrawTutorialStepSection()
    {
        CardGizmo firstGizmo = (CardGizmo)target;
        if (firstGizmo == null) return;

        TutorialDrawPile pile = firstGizmo.GetComponentInParent<TutorialDrawPile>();
        if (pile != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox($"This card is inside Draw Pile ({firstGizmo.gameObject.name}). Changes to its Suit/Rank/Type will automatically save to DrawPile data. To trigger this in tutorial steps, enable 'tap_drawpile' in Tutorial Map Tool.", MessageType.Info);
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
        EditorGUILayout.LabelField("Tutorial Step Configuration (tap_card):", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");

        if (targets.Length == 1)
        {
            if (firstGizmo.tutorialStep > 0)
            {
                EditorGUILayout.HelpBox($"This card is Step #{firstGizmo.tutorialStep} of {tool.tutorialSteps.Count} in tap_card sequence", MessageType.Info);

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
                if (GUILayout.Button("Remove From tap_card Steps", GUILayout.Height(26)))
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
            if (GUILayout.Button("+ Assign Steps to Selected Cards (in order)", GUILayout.Height(34)))
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
