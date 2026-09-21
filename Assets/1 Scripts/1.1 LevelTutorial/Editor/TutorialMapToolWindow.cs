using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class TutorialMapToolWindow : EditorWindow
{
    private TutorialMapTool activeTool;
    private Vector2 mainScrollPos;
    private Vector2 levelsScrollPos;
    private Vector2 stepScrollPos;

    private bool showLevelsFoldout = true;
    private bool showSettingsFoldout = true;
    private bool showActionsFoldout = true;
    private bool showStepsFoldout = true;

    [MenuItem("Tools/Tutorial Map Tool Window")]
    public static void ShowWindow()
    {
        TutorialMapToolWindow window = GetWindow<TutorialMapToolWindow>("Tutorial Map");
        window.minSize = new Vector2(340, 500);
        window.Show();
    }

    private void OnEnable()
    {
        FindToolInScene();
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        Repaint();
    }

    private void FindToolInScene()
    {
        if (activeTool == null)
        {
            activeTool = FindObjectOfType<TutorialMapTool>();
        }
    }

    private void OnGUI()
    {
        // Top Toolbar / Status
        DrawTopToolbar();

        if (activeTool == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No 'TutorialMapTool' found in the current active scene.\nPlease open a tutorial scene (e.g. 'LevelTutorial.unity') or assign the tool above.", MessageType.Warning);
            if (GUILayout.Button("Retry Find in Scene", GUILayout.Height(30)))
            {
                FindToolInScene();
            }
            return;
        }

        mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

        // Contextual Selected Card Banner / Pick Mode Banner
        DrawSelectedCardBanner();

        // 1. Scene Canvas Actions (Add Card, Center, Clear, Save JSON)
        DrawSceneActionsSection();

        // 2. Tutorial Steps Sequence
        DrawTutorialStepsSection();

        // 3. Level JSON & Loaded Levels
        DrawLevelConfigSection();

        // 4. Target & Settings (At the very bottom)
        DrawAdvancedSettingsSection();

        EditorGUILayout.Space(20);
        EditorGUILayout.EndScrollView();
    }

    private void DrawTopToolbar()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        GUI.color = activeTool != null ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.7f, 0.7f);
        GUILayout.Label(activeTool != null ? "● Connected" : "○ Disconnected", EditorStyles.boldLabel, GUILayout.Width(95));
        GUI.color = Color.white;

        TutorialMapTool prev = activeTool;
        activeTool = (TutorialMapTool)EditorGUILayout.ObjectField(activeTool, typeof(TutorialMapTool), true);
        if (prev != activeTool && activeTool != null)
        {
            Repaint();
        }

        if (activeTool != null)
        {
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeGameObject = activeTool.gameObject;
            }
        }
        else
        {
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                FindToolInScene();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedCardBanner()
    {
        if (TutorialStepPicker.isPicking)
        {
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
            EditorGUILayout.HelpBox("🎯 PICK CARD MODE ACTIVE:\nClick any card on Scene View to add as next step.\nClick empty space (outside) or press [ESC] to cancel.", MessageType.Warning);
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("❌ Cancel Picking Mode [ESC]", GUILayout.Height(24)))
            {
                TutorialStepPicker.StopPicking();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        CardGizmo selCard = null;
        if (Selection.activeGameObject != null)
        {
            selCard = Selection.activeGameObject.GetComponent<CardGizmo>();
        }

        if (selCard == null) return;

        bool isCanvasCard = selCard.transform.IsChildOf(activeTool.transform) &&
            (activeTool.checkCardObj == null || !selCard.transform.IsChildOf(activeTool.checkCardObj.transform)) &&
            (activeTool.drawPileObj == null || !selCard.transform.IsChildOf(activeTool.drawPileObj.transform));

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.BeginHorizontal();

        GUIStyle bold = new GUIStyle(EditorStyles.boldLabel);
        bold.normal.textColor = new Color(0.2f, 0.8f, 1f);
        GUILayout.Label($"Selected: {selCard.name}", bold);

        GUILayout.Label($"Suit: {selCard.suit} | Rank: {selCard.rank}", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();

        if (isCanvasCard)
        {
            EditorGUILayout.BeginHorizontal();
            if (selCard.tutorialStep > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.9f, 0.5f);
                GUILayout.Label($"Step #{selCard.tutorialStep}", EditorStyles.boldLabel, GUILayout.Width(70));
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Remove Step", GUILayout.Height(22)))
                {
                    activeTool.RemoveCardStep(selCard);
                }
            }
            else
            {
                int nextStep = activeTool.GetNextStepNumber();
                GUI.backgroundColor = new Color(0.5f, 0.9f, 0.6f);
                if (GUILayout.Button($"+ Assign as Next Step (#{nextStep})", GUILayout.Height(22)))
                {
                    activeTool.AddCardStep(selCard);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Card belongs to DrawPile or CheckCard.", MessageType.None);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawSceneActionsSection()
    {
        showActionsFoldout = EditorGUILayout.Foldout(showActionsFoldout, "Current Scene Canvas Actions", true, EditorStyles.foldoutHeader);
        if (!showActionsFoldout) return;

        EditorGUILayout.BeginVertical("helpbox");

        // Add New Card
        GUI.backgroundColor = new Color(0.7f, 0.88f, 1f);
        if (GUILayout.Button("+ Add New Card into Canvas", GUILayout.Height(32)))
        {
            TutorialMapToolEditor.AddNewCard(activeTool);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Center Canvas (0,0)", GUILayout.Height(26)))
        {
            TutorialMapToolEditor.CenterMap(activeTool);
        }

        if (GUILayout.Button("Clear Canvas (Empty)", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog("Clear Canvas", "Clear all cards from canvas?", "Yes", "No"))
            {
                TutorialMapToolEditor.ClearChildren(activeTool.transform);
                activeTool.targetLevelId = "";
                activeTool.UpdateLevelText();
                EditorUtility.SetDirty(activeTool);
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);

        // Save Button
        GUI.backgroundColor = new Color(0.55f, 0.92f, 0.55f);
        string btnText = string.IsNullOrEmpty(activeTool.targetLevelId) ?
            "💾 Save To JSON (Auto-Increment ID)" :
            $"💾 Save Overwrite (Level {activeTool.targetLevelId}) To JSON";

        if (GUILayout.Button(btnText, GUILayout.Height(38)))
        {
            TutorialMapToolEditor.SaveTutorialMapToJSON(activeTool);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawTutorialStepsSection()
    {
        showStepsFoldout = EditorGUILayout.Foldout(showStepsFoldout, $"Tutorial Steps ({activeTool.tutorialSteps.Count})", true, EditorStyles.foldoutHeader);
        if (!showStepsFoldout) return;

        TutorialMapToolEditor.DrawTutorialStepsGUI(activeTool, ref stepScrollPos);
        EditorGUILayout.Space(10);
    }

    private void DrawLevelConfigSection()
    {
        showLevelsFoldout = EditorGUILayout.Foldout(showLevelsFoldout, "Level JSON & Loaded Levels", true, EditorStyles.foldoutHeader);
        if (!showLevelsFoldout) return;

        EditorGUILayout.BeginVertical("helpbox");

        // JSON Path
        EditorGUILayout.BeginHorizontal();
        activeTool.jsonPath = EditorGUILayout.TextField("JSON Path", activeTool.jsonPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string dir = "";
            if (!string.IsNullOrEmpty(activeTool.jsonPath) && File.Exists(activeTool.jsonPath))
                dir = Path.GetDirectoryName(activeTool.jsonPath);

            string path = EditorUtility.OpenFilePanel("Select Tutorial JSON", dir, "json");
            if (!string.IsNullOrEmpty(path))
            {
                Undo.RecordObject(activeTool, "Change JSON Path");
                activeTool.jsonPath = path;
                EditorUtility.SetDirty(activeTool);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Load JSON", GUILayout.Height(28)))
        {
            TutorialMapToolEditor.LoadJSONs(activeTool);
        }

        // Loaded Levels List
        if (activeTool.loadedLevels != null && activeTool.loadedLevels.Length > 0)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField($"Loaded {activeTool.loadedLevels.Length} Levels:", EditorStyles.boldLabel);

            levelsScrollPos = EditorGUILayout.BeginScrollView(levelsScrollPos, GUILayout.Height(180));
            foreach (var level in activeTool.loadedLevels)
            {
                EditorGUILayout.BeginHorizontal("box");

                GUILayout.Label($"ID: {level.id}", GUILayout.Width(45));

                string[] itemTypes = { "manual", "tutorial" };
                int tIdx = Mathf.Max(0, System.Array.IndexOf(itemTypes, string.IsNullOrEmpty(level.type) ? "tutorial" : level.type));
                int newTIdx = EditorGUILayout.Popup(tIdx, itemTypes, GUILayout.Width(75));

                string[] itemDiffs = { "easy", "medium", "hard", "super_hard" };
                int dIdx = Mathf.Max(0, System.Array.IndexOf(itemDiffs, string.IsNullOrEmpty(level.difficulty) ? "easy" : level.difficulty));
                int newDIdx = EditorGUILayout.Popup(dIdx, itemDiffs, GUILayout.Width(90));

                if (newTIdx != tIdx || newDIdx != dIdx)
                {
                    level.type = itemTypes[newTIdx];
                    level.difficulty = itemDiffs[newDIdx];
                    if (level.type == "tutorial" && (level.tutorialConfig == null || level.tutorialConfig.instructions == null))
                    {
                        level.tutorialConfig = new TutorialConfig { instructions = new List<TutorialInstruction>() };
                    }
                    else if (level.type != "tutorial")
                    {
                        level.tutorialConfig = new TutorialConfig { instructions = new List<TutorialInstruction>() };
                    }
                    TutorialMapToolEditor.SaveJSONs(activeTool);
                    GUIUtility.ExitGUI();
                }

                GUILayout.Space(3);
                if (GUILayout.Button("Generate", GUILayout.Width(70)))
                {
                    TutorialMapToolEditor.GenerateLevelInScene(activeTool, level);
                }

                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Level", $"Delete level '{level.id}'?", "Yes", "No"))
                    {
                        var list = new List<TutorialLevelData>(activeTool.loadedLevels);
                        list.Remove(level);
                        activeTool.loadedLevels = list.ToArray();
                        TutorialMapToolEditor.SaveJSONs(activeTool);
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawAdvancedSettingsSection()
    {
        showSettingsFoldout = EditorGUILayout.Foldout(showSettingsFoldout, "Target & Settings", true, EditorStyles.foldoutHeader);
        if (!showSettingsFoldout) return;

        EditorGUILayout.BeginVertical("helpbox");

        EditorGUI.BeginChangeCheck();

        activeTool.targetLevelId = EditorGUILayout.TextField("Target Level ID", activeTool.targetLevelId);

        string[] types = { "manual", "tutorial" };
        int selectedType = Mathf.Max(0, System.Array.IndexOf(types, activeTool.targetType));
        selectedType = EditorGUILayout.Popup("Target Type", selectedType, types);
        activeTool.targetType = types[selectedType];

        string[] diffs = { "easy", "medium", "hard", "super_hard" };
        int selectedDiff = Mathf.Max(0, System.Array.IndexOf(diffs, activeTool.targetDifficulty));
        selectedDiff = EditorGUILayout.Popup("Target Difficulty", selectedDiff, diffs);
        activeTool.targetDifficulty = diffs[selectedDiff];

        string[] modes = { "classic" };
        int selectedMode = Mathf.Max(0, System.Array.IndexOf(modes, activeTool.targetMode));
        selectedMode = EditorGUILayout.Popup("Target Mode", selectedMode, modes);
        activeTool.targetMode = modes[selectedMode];

        activeTool.positionMultiplier = EditorGUILayout.FloatField("Position Multiplier", activeTool.positionMultiplier);
        activeTool.invertY = EditorGUILayout.Toggle("Invert Y", activeTool.invertY);
        activeTool.invertAngle = EditorGUILayout.Toggle("Invert Angle", activeTool.invertAngle);
        activeTool.autoCenterOnSave = EditorGUILayout.Toggle("Auto Center On Save", activeTool.autoCenterOnSave);

        activeTool.cardPrefab = (GameObject)EditorGUILayout.ObjectField("Card Prefab", activeTool.cardPrefab, typeof(GameObject), false);
        activeTool.cardSpriteData = (CardSpriteData)EditorGUILayout.ObjectField("Card Sprite Data", activeTool.cardSpriteData, typeof(CardSpriteData), false);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(activeTool, "Update Tool Settings");
            activeTool.UpdateLevelText();
            EditorUtility.SetDirty(activeTool);
        }

        EditorGUILayout.EndVertical();
    }
}
