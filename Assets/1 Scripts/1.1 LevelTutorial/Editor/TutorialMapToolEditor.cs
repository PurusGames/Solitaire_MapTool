using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(TutorialMapTool))]
public class TutorialMapToolEditor : Editor
{
    private Vector2 scrollPos;
    private Vector2 stepScrollPos;

    [System.Serializable]
    public class ArrayWrapper<T>
    {
        public T[] Items;
    }

    public override void OnInspectorGUI()
    {
        GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
        if (GUILayout.Button("⧉ Open Tutorial Map Tool Tab (Dockable Window)", GUILayout.Height(32)))
        {
            TutorialMapToolWindow.ShowWindow();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(8);

        serializedObject.Update();
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            if (prop.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
                continue;
            }

            // Hide raw tutorial fields from default inspector as we render a custom UI for it
            if (prop.name == "tutorialSteps" || prop.name == "tapDrawPile" || prop.name == "tapUndo" || prop.name == "tapJoker")
            {
                continue;
            }
            
            if (prop.name == "targetType")
            {
                string[] types = { "manual", "tutorial" };
                int selectedType = Mathf.Max(0, System.Array.IndexOf(types, prop.stringValue));
                selectedType = EditorGUILayout.Popup("Target Type", selectedType, types);
                prop.stringValue = types[selectedType];
                continue;
            }

            if (prop.name == "targetDifficulty")
            {
                string[] difficulties = { "easy", "medium", "hard", "super_hard" };
                int selectedDiff = Mathf.Max(0, System.Array.IndexOf(difficulties, prop.stringValue));
                selectedDiff = EditorGUILayout.Popup("Target Difficulty", selectedDiff, difficulties);
                prop.stringValue = difficulties[selectedDiff];
                continue;
            }

            if (prop.name == "targetMode")
            {
                string[] modes = { "classic" };
                int selectedMode = Mathf.Max(0, System.Array.IndexOf(modes, prop.stringValue));
                selectedMode = EditorGUILayout.Popup("Target Mode", selectedMode, modes);
                prop.stringValue = modes[selectedMode];
                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
            
            if (prop.name == "jsonPath")
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Browse...", GUILayout.Width(100)))
                {
                    string dir = "";
                    if (!string.IsNullOrEmpty(prop.stringValue) && File.Exists(prop.stringValue))
                        dir = Path.GetDirectoryName(prop.stringValue);
                    
                    string path = EditorUtility.OpenFilePanel("Select Tutorial JSON", dir, "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        prop.stringValue = path;
                        GUI.FocusControl(null);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
        }
        serializedObject.ApplyModifiedProperties();
        
        TutorialMapTool tool = (TutorialMapTool)target;

        GUILayout.Space(15);
        if (GUILayout.Button("Load JSON", GUILayout.Height(30)))
        {
            LoadJSONs(tool);
        }

        if (tool.loadedLevels != null && tool.loadedLevels.Length > 0)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Loaded {tool.loadedLevels.Length} Tutorial Levels:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("helpbox");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(220));
            TutorialLevelData levelToDelete = null;
            bool jsonNeedsSave = false;

            try
            {
                foreach (var level in tool.loadedLevels)
                {
                    GUILayout.BeginHorizontal();
                    
                    GUILayout.Label($"ID: {level.id}", GUILayout.Width(50));
                    
                    string[] itemTypes = { "manual", "tutorial" };
                    int tIdx = Mathf.Max(0, System.Array.IndexOf(itemTypes, string.IsNullOrEmpty(level.type) ? "tutorial" : level.type));
                    int newTIdx = EditorGUILayout.Popup(tIdx, itemTypes, GUILayout.Width(75));
                    
                    string[] itemDiffs = { "easy", "medium", "hard", "super_hard" };
                    int dIdx = Mathf.Max(0, System.Array.IndexOf(itemDiffs, string.IsNullOrEmpty(level.difficulty) ? "easy" : level.difficulty));
                    int newDIdx = EditorGUILayout.Popup(dIdx, itemDiffs, GUILayout.Width(95));

                    if (newTIdx != tIdx || newDIdx != dIdx)
                    {
                        level.type = itemTypes[newTIdx];
                        level.difficulty = itemDiffs[newDIdx];
                        if (level.type == "tutorial" && (level.tutorialConfig == null || level.tutorialConfig.tap_card == null)) 
                        {
                            level.tutorialConfig = new TutorialConfig 
                            {
                                tap_card = new List<int>(),
                                tap_drawpile = false,
                                tap_undo = false,
                                tap_joker = false
                            };
                        } 
                        else if (level.type != "tutorial") 
                        {
                            level.tutorialConfig = null;
                        }
                        jsonNeedsSave = true;
                    }
                    
                    GUILayout.Space(5);
                    if (GUILayout.Button("Generate", GUILayout.Width(80)))
                    {
                        GenerateLevelInScene(tool, level);
                    }
                    
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("Del", GUILayout.Width(35)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Level", $"Are you sure you want to delete level '{level.id}'?", "Yes", "No"))
                        {
                            levelToDelete = level;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            if (levelToDelete != null)
            {
                var list = new List<TutorialLevelData>(tool.loadedLevels);
                list.Remove(levelToDelete);
                tool.loadedLevels = list.ToArray();
                SaveJSONs(tool);
            }
            else if (jsonNeedsSave)
            {
                SaveJSONs(tool);
            }
        }

        // TUTORIAL CONFIG & STEPS SECTION
        DrawTutorialStepsGUI(tool, ref stepScrollPos, 380f);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Current Scene Tutorial Map Actions:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");
        
        if (GUILayout.Button("Create Empty Canvas (Clear)", GUILayout.Height(30)))
        {
            ClearChildren(tool.transform);
            tool.targetLevelId = "";
            tool.UpdateLevelText();
            EditorUtility.SetDirty(tool);
            SceneView.RepaintAll();
        }
        
        GUILayout.Space(5);
        GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
        if (GUILayout.Button("Add New Card into Canvas", GUILayout.Height(30)))
        {
            AddNewCard(tool);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);
        if (GUILayout.Button("Center Canvas to (0,0)"))
        {
            CenterMap(tool);
        }

        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        string btnText = string.IsNullOrEmpty(tool.targetLevelId) ? "Save To JSON (Auto-Increment ID)" : $"Save Overwrite ({tool.targetLevelId}) To JSON";
        if (GUILayout.Button(btnText, GUILayout.Height(40)))
        {
            SaveTutorialMapToJSON(tool);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    public static void DrawTutorialStepsGUI(TutorialMapTool tool, ref Vector2 scrollPos, float maxHeight = 400f)
    {
        if (tool == null) return;

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Tutorial Config (Instructions & Flags):", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");

        // 1. ACTION FLAGS (tap_drawpile, tap_undo, tap_joker)
        EditorGUILayout.LabelField("Action Flags (Click to Toggle):", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        // Tap DrawPile Button
        Color activeGreen = new Color(0.35f, 0.88f, 0.45f);
        Color inactiveGray = new Color(0.88f, 0.88f, 0.88f);

        GUI.backgroundColor = tool.tapDrawPile ? activeGreen : inactiveGray;
        string dpLabel = tool.tapDrawPile ? "✔ tap_drawpile: ON" : "✖ tap_drawpile: OFF";
        if (GUILayout.Button(dpLabel, GUILayout.Height(28)))
        {
            Undo.RecordObject(tool, "Toggle tap_drawpile");
            tool.ToggleTapDrawPile();
        }

        // Tap Undo Button
        GUI.backgroundColor = tool.tapUndo ? activeGreen : inactiveGray;
        string undoLabel = tool.tapUndo ? "✔ tap_undo: ON" : "✖ tap_undo: OFF";
        if (GUILayout.Button(undoLabel, GUILayout.Height(28)))
        {
            Undo.RecordObject(tool, "Toggle tap_undo");
            tool.ToggleTapUndo();
        }

        // Tap Joker Button
        GUI.backgroundColor = tool.tapJoker ? activeGreen : inactiveGray;
        string jokerLabel = tool.tapJoker ? "✔ tap_joker: ON" : "✖ tap_joker: OFF";
        if (GUILayout.Button(jokerLabel, GUILayout.Height(28)))
        {
            Undo.RecordObject(tool, "Toggle tap_joker");
            tool.ToggleTapJoker();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        bool newDp = EditorGUILayout.ToggleLeft("tap_drawpile", tool.tapDrawPile, GUILayout.Width(110));
        bool newUndo = EditorGUILayout.ToggleLeft("tap_undo", tool.tapUndo, GUILayout.Width(100));
        bool newJoker = EditorGUILayout.ToggleLeft("tap_joker", tool.tapJoker, GUILayout.Width(100));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(tool, "Change Tutorial Flags");
            tool.tapDrawPile = newDp;
            tool.tapUndo = newUndo;
            tool.tapJoker = newJoker;
            EditorUtility.SetDirty(tool);
            EditorApplication.delayCall += () =>
            {
                SceneView.RepaintAll();
            };
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 2. TAP CARD SEQUENCE (tap_card: [1, 2, 0])
        int cardStepCount = tool.tutorialSteps != null ? tool.tutorialSteps.Count : 0;
        EditorGUILayout.LabelField($"tap_card Sequence ({cardStepCount} cards):", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Order of cards the player must tap. Use ▲/▼ to reorder.", MessageType.None);

        if (tool.tutorialSteps == null || tool.tutorialSteps.Count == 0)
        {
            EditorGUILayout.LabelField("No card tap steps defined yet.", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            float calculatedHeight = Mathf.Clamp(tool.tutorialSteps.Count * 33f + 12f, 150f, maxHeight);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(calculatedHeight));

            int moveFrom = -1;
            int moveTo = -1;
            int removeIndex = -1;

            try
            {
                for (int i = 0; i < tool.tutorialSteps.Count; i++)
                {
                    CardGizmo card = tool.tutorialSteps[i];
                    EditorGUILayout.BeginHorizontal("box");

                    // Step number badge
                    GUILayout.Label($"#{i + 1}", EditorStyles.boldLabel, GUILayout.Width(28));

                    // Target card selector / display
                    CardGizmo prevCard = card;
                    CardGizmo newCard = (CardGizmo)EditorGUILayout.ObjectField(card, typeof(CardGizmo), true);
                    if (prevCard != newCard)
                    {
                        Undo.RecordObject(tool, "Change Tutorial Step Card");
                        tool.tutorialSteps[i] = newCard;
                        tool.SyncTutorialSteps();
                    }

                    if (card != null)
                    {
                        GUILayout.Label($"ID:{card.cardId} ({card.suit} {card.rank})", EditorStyles.miniLabel, GUILayout.Width(110));

                        if (GUILayout.Button("Select", GUILayout.Width(48)))
                        {
                            Selection.activeGameObject = card.gameObject;
                        }
                    }
                    else
                    {
                        GUILayout.Label("[Missing/Deleted]", EditorStyles.miniLabel, GUILayout.Width(110));
                    }

                    // Move Up
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("▲", GUILayout.Width(25)))
                    {
                        moveFrom = i;
                        moveTo = i - 1;
                    }

                    // Move Down
                    GUI.enabled = i < tool.tutorialSteps.Count - 1;
                    if (GUILayout.Button("▼", GUILayout.Width(25)))
                    {
                        moveFrom = i;
                        moveTo = i + 1;
                    }
                    GUI.enabled = true;

                    // Delete Step
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        removeIndex = i;
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            if (moveFrom != -1 && moveTo != -1)
            {
                tool.MoveStep(moveFrom, moveTo);
            }
            else if (removeIndex != -1)
            {
                tool.RemoveStepAt(removeIndex);
            }
        }

        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        // Quick add currently selected card
        CardGizmo selCard = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<CardGizmo>() : null;
        bool canAddSel = selCard != null && selCard.transform.IsChildOf(tool.transform) &&
            (tool.checkCardObj == null || !selCard.transform.IsChildOf(tool.checkCardObj.transform)) &&
            (tool.drawPileObj == null || !selCard.transform.IsChildOf(tool.drawPileObj.transform));

        GUI.enabled = canAddSel;
        GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
        if (GUILayout.Button("+ Add Selected", GUILayout.Height(26), GUILayout.Width(105)))
        {
            tool.AddCardStep(selCard);
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Sync & Auto-Count Steps", GUILayout.Height(24)))
        {
            tool.SyncTutorialSteps();
        }

        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("Clear All Steps", GUILayout.Height(24)))
        {
            if (EditorUtility.DisplayDialog("Clear Steps", "Are you sure you want to clear all tutorial steps and flags? ", "Yes", "No"))
            {
                tool.ClearTutorialSteps();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    public static void CenterMap(TutorialMapTool tool)
    {
        if (tool == null) return;
        CardGizmo[] gizmos = tool.GetComponentsInChildren<CardGizmo>();
        if (gizmos.Length == 0) return;

        Undo.RegisterFullObjectHierarchyUndo(tool.gameObject, "Center Canvas");

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        int validCount = 0;
        foreach (var g in gizmos)
        {
            if (tool.checkCardObj != null && g.transform.IsChildOf(tool.checkCardObj.transform)) continue;
            if (tool.drawPileObj != null && g.transform.IsChildOf(tool.drawPileObj.transform)) continue;
            if (g.GetComponent<TutorialCheckCard>() != null || g.gameObject.name == "CheckCardData") continue;

            validCount++;
            Vector3 pos = g.transform.position;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        if (validCount == 0) return;

        Vector2 center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        Vector3 offset = new Vector3(center.x - tool.transform.position.x, center.y - tool.transform.position.y, 0);

        foreach (var g in gizmos)
        {
            if (tool.checkCardObj != null && g.transform.IsChildOf(tool.checkCardObj.transform)) continue;
            if (tool.drawPileObj != null && g.transform.IsChildOf(tool.drawPileObj.transform)) continue;
            if (g.GetComponent<TutorialCheckCard>() != null || g.gameObject.name == "CheckCardData") continue;

            g.transform.position -= offset;
        }

        EditorUtility.SetDirty(tool);
    }

    public static void ClearChildren(Transform t)
    {
        if (t == null) return;
        Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Clear Canvas");
        TutorialMapTool tool = t.GetComponent<TutorialMapTool>();
        
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            GameObject childObj = t.GetChild(i).gameObject;
            if (tool != null)
            {
                if (tool.checkCardObj != null && childObj == tool.checkCardObj.gameObject) continue;
                if (tool.drawPileObj != null && childObj == tool.drawPileObj.gameObject) continue;
            }
            Undo.DestroyObjectImmediate(childObj);
        }
        
        // Reset tutorial steps and flags on canvas
        if (tool != null) 
        {
            tool.tutorialSteps.Clear();
            tool.tapDrawPile = false;
            tool.tapUndo = false;
            tool.tapJoker = false;
            tool.SyncTutorialSteps();
        }
    }

    public static void AddNewCard(TutorialMapTool tool)
    {
        if (tool == null) return;
        if (tool.cardPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Missing Card Prefab in TutorialMapTool!", "OK");
            return;
        }

        GameObject newCard = (GameObject)PrefabUtility.InstantiatePrefab(tool.cardPrefab);
        newCard.transform.SetParent(tool.transform, false);
        newCard.name = "card_" + (tool.transform.childCount);
        
        CardGizmo gizmo = newCard.GetComponent<CardGizmo>();
        if (gizmo != null)
        {
            if (tool.cardSpriteData != null) gizmo.spriteData = tool.cardSpriteData;
            gizmo.showFaceDetails = true;
            gizmo.tutorialStep = 0;
            gizmo.layer = 0;
            gizmo.UpdateVisuals();
            gizmo.UpdateSorting();
        }
        
        Selection.activeGameObject = newCard;
        Undo.RegisterCreatedObjectUndo(newCard, "Add Card");
    }

    public static void SaveTutorialMapToJSON(TutorialMapTool tool)
    {
        if (tool == null) return;
        if (tool.loadedLevels == null)
        {
            EditorUtility.DisplayDialog("Error", "Levels not loaded! Please load JSON first.", "OK");
            return;
        }

        if (tool.autoCenterOnSave)
        {
            CenterMap(tool);
        }

        int targetId = 1;
        if (string.IsNullOrEmpty(tool.targetLevelId))
        {
            int maxId = 0;
            foreach (var lvl in tool.loadedLevels)
            {
                if (lvl.id > maxId) maxId = lvl.id;
            }
            targetId = maxId + 1;
            tool.targetLevelId = targetId.ToString();
            tool.UpdateLevelText();
        }
        else
        {
            if (!int.TryParse(tool.targetLevelId, out targetId))
            {
                EditorUtility.DisplayDialog("Error", "Target Level ID must be an integer.", "OK");
                return;
            }
        }

        List<TutorialLevelData> levelList = new List<TutorialLevelData>(tool.loadedLevels);
        TutorialLevelData existingLevel = levelList.Find(l => l.id == targetId);

        if (existingLevel == null)
        {
            if (EditorUtility.DisplayDialog("New Level", $"Level ID '{targetId}' not found in JSON. Add as new?", "Yes", "No"))
            {
                existingLevel = new TutorialLevelData 
                { 
                    id = targetId,
                    type = tool.targetType,
                    mode = tool.targetMode,
                    difficulty = tool.targetDifficulty,
                    manualConfig = new ManualConfig { cards = new List<TutorialCardData>() },
                    checkCardData = new TutorialCheckCardData { id = "check-0", type = "normal", suit = "spade", rank = 4 },
                    drawPileData = new TutorialDrawPileData 
                    { 
                        count = 10,
                        fixedCards = new List<TutorialFixedCard>()
                    },
                    tutorialConfig = null
                };
                levelList.Add(existingLevel);
                tool.loadedLevels = levelList.ToArray();
            }
            else return;
        }
        else
        {
            existingLevel.type = tool.targetType;
            existingLevel.mode = tool.targetMode;
            existingLevel.difficulty = tool.targetDifficulty;

            if (existingLevel.manualConfig == null) 
            {
                existingLevel.manualConfig = new ManualConfig { cards = new List<TutorialCardData>() };
            }
        }

        // Export Canvas Cards
        List<TutorialCardData> cards = new List<TutorialCardData>();
        CardGizmo[] gizmos = tool.GetComponentsInChildren<CardGizmo>();

        int cardIndexCounter = 0;
        foreach (var gizmo in gizmos)
        {
            if (tool.checkCardObj != null && gizmo.transform.IsChildOf(tool.checkCardObj.transform)) continue;
            if (tool.drawPileObj != null && gizmo.transform.IsChildOf(tool.drawPileObj.transform)) continue;
            if (gizmo.GetComponent<TutorialCheckCard>() != null || gizmo.gameObject.name == "CheckCardData") continue;

            TutorialCardData cData = new TutorialCardData();
            cData.id = cardIndexCounter;
            gizmo.cardId = cardIndexCounter;
            cardIndexCounter++;

            cData.type = CardGizmo.TypeToString(gizmo.type);
            if (gizmo.type == CardType.Extra) {
                cData.extraType = CardGizmo.ExtraTypeToString(gizmo.extraType);
            } else {
                cData.extraType = "none";
            }
            cData.suit = gizmo.suit.ToString().ToLower();
            cData.rank = (int)gizmo.rank + 1;
            cData.obstacle = CardGizmo.ObstacleToString(gizmo.obstacle);

            cData.x = Mathf.Round(gizmo.transform.localPosition.x * tool.positionMultiplier);
            float calcY = gizmo.transform.localPosition.y * tool.positionMultiplier;
            cData.y = Mathf.Round(tool.invertY ? -calcY : calcY);

            float calcAngle = gizmo.transform.localEulerAngles.z;
            cData.angle = Mathf.Round(tool.invertAngle ? -calcAngle : calcAngle);
            cData.layer = gizmo.layer;

            cards.Add(cData);
        }
        existingLevel.manualConfig.cards = cards;

        // Export Tutorial Config using Helper
        TutorialDataHelper.SaveTutorialConfig(tool, existingLevel);

        TutorialDataHelper.AutoResolveReferences(tool);
        TutorialDataHelper.SaveExtraObjects(tool, existingLevel);

        SaveJSONs(tool);
        
        int dpCardsCount = existingLevel.drawPileData?.fixedCards != null ? existingLevel.drawPileData.fixedCards.Count : 0;
        int dpTotalCount = existingLevel.drawPileData != null ? existingLevel.drawPileData.count : 0;
        string checkCardStr = existingLevel.checkCardData != null ? $"{existingLevel.checkCardData.rank} of {existingLevel.checkCardData.suit}" : "none";
        int tapCardsCount = existingLevel.tutorialConfig?.tap_card != null ? existingLevel.tutorialConfig.tap_card.Count : 0;
        string tutorialConfigStatus = existingLevel.tutorialConfig != null ? $"tap_card: {tapCardsCount}, DP:{existingLevel.tutorialConfig.tap_drawpile}, Undo:{existingLevel.tutorialConfig.tap_undo}, Joker:{existingLevel.tutorialConfig.tap_joker}" : "empty ({})";

        Debug.Log($"Level '{targetId}' saved successfully! Canvas cards: {cards.Count}, TutorialConfig: {tutorialConfigStatus}, DrawPile fixed: {dpCardsCount} (total: {dpTotalCount}), CheckCard: {checkCardStr}");
        EditorUtility.DisplayDialog("Success", $"Level '{targetId}' saved successfully to JSON!\n\n• Canvas Cards: {cards.Count}\n• Tutorial Config: {tutorialConfigStatus}\n• DrawPile: {dpCardsCount} fixed cards (total count: {dpTotalCount})\n• CheckCard: {checkCardStr}", "OK");
    }

    public static void SaveJSONs(TutorialMapTool tool)
    {
        if (tool == null) return;
        string json = string.Empty;
        if (tool.loadedLevels == null || tool.loadedLevels.Length == 0)
        {
            json = "[]";
        }
        else
        {
            // Clean tutorialConfig for levels where nothing is configured or type is not tutorial
            foreach (var lvl in tool.loadedLevels)
            {
                if (lvl == null) continue;
                bool isTutorial = lvl.type == "tutorial";
                bool hasSteps = lvl.tutorialConfig != null && lvl.tutorialConfig.tap_card != null && lvl.tutorialConfig.tap_card.Count > 0;
                bool hasFlags = lvl.tutorialConfig != null && (lvl.tutorialConfig.tap_drawpile || lvl.tutorialConfig.tap_undo || lvl.tutorialConfig.tap_joker);

                if (!isTutorial || (!hasSteps && !hasFlags))
                {
                    lvl.tutorialConfig = null;
                }
            }

            ArrayWrapper<TutorialLevelData> wrapper = new ArrayWrapper<TutorialLevelData> { Items = tool.loadedLevels };
            json = JsonUtility.ToJson(wrapper, true);
            int start = json.IndexOf("[");
            int end = json.LastIndexOf("]");
            if (start != -1 && end != -1)
            {
                json = json.Substring(start, end - start + 1);
            }
            else
            {
                json = "[]";
            }

            // Replace default empty serialized tutorialConfig with empty object {}
            string emptyConfigPattern = @"\""tutorialConfig\""\s*:\s*\{\s*\""tap_card\""\s*:\s*\[\s*\]\s*,\s*\""tap_drawpile\""\s*:\s*false\s*,\s*\""tap_undo\""\s*:\s*false\s*,\s*\""tap_joker\""\s*:\s*false\s*\}";
            json = System.Text.RegularExpressions.Regex.Replace(json, emptyConfigPattern, "\"tutorialConfig\": {}");
        }
        File.WriteAllText(tool.jsonPath, json);
        AssetDatabase.Refresh();
    }

    public static void LoadJSONs(TutorialMapTool tool)
    {
        if (tool == null) return;
        if (string.IsNullOrEmpty(tool.jsonPath) || !File.Exists(tool.jsonPath))
        {
            EditorUtility.DisplayDialog("Error", $"JSON not found at: {tool.jsonPath}", "OK");
            return;
        }

        string json = File.ReadAllText(tool.jsonPath);
        string wrappedJson = "{\"Items\":" + json + "}";
        ArrayWrapper<TutorialLevelData> wrapper = JsonUtility.FromJson<ArrayWrapper<TutorialLevelData>>(wrappedJson);
        tool.loadedLevels = wrapper != null ? wrapper.Items : new TutorialLevelData[0];

        EditorUtility.SetDirty(tool);
        Debug.Log($"Loaded {tool.loadedLevels.Length} tutorial levels.");
    }

    public static void GenerateLevelInScene(TutorialMapTool tool, TutorialLevelData level)
    {
        if (tool == null || level == null) return;
        if (tool.cardPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Card Prefab in the TutorialMapTool.", "OK");
            return;
        }
        if (tool.cardSpriteData == null)
        {
            Debug.LogWarning("CardSpriteData is not assigned. Card visuals may not update.");
        }

        Undo.RegisterFullObjectHierarchyUndo(tool.gameObject, "Generate Tutorial Level");

        for (int i = tool.transform.childCount - 1; i >= 0; i--)
        {
            GameObject childObj = tool.transform.GetChild(i).gameObject;
            if (tool.checkCardObj != null && childObj == tool.checkCardObj.gameObject) continue;
            if (tool.drawPileObj != null && childObj == tool.drawPileObj.gameObject) continue;
            Undo.DestroyObjectImmediate(childObj);
        }

        Dictionary<int, CardGizmo> idToGizmo = new Dictionary<int, CardGizmo>();

        if (level.manualConfig == null || level.manualConfig.cards == null)
        {
            Debug.LogWarning($"Level {level.id} has no cards inside manualConfig.");
        }
        else
        {
            foreach (var cardData in level.manualConfig.cards)
            {
                GameObject cardGo = (GameObject)PrefabUtility.InstantiatePrefab(tool.cardPrefab);
                
                if (cardGo == null)
                {
                    Debug.LogError("Failed to instantiate card prefab.");
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(cardGo, "Create Tutorial Card");
                cardGo.name = $"card_{cardData.id}";
                cardGo.transform.SetParent(tool.transform);

                float stX = cardData.x / tool.positionMultiplier;
                float stY = (tool.invertY ? -cardData.y : cardData.y) / tool.positionMultiplier;
                float stAngle = tool.invertAngle ? -cardData.angle : cardData.angle;

                cardGo.transform.localPosition = new Vector3(stX, stY, 0);
                cardGo.transform.localEulerAngles = new Vector3(0, 0, stAngle);

                CardGizmo gizmo = cardGo.GetComponent<CardGizmo>();
                if (gizmo != null)
                {
                    gizmo.cardId = cardData.id;
                    gizmo.suit = TutorialCheckCard.ParseSuit(cardData.suit);
                    gizmo.rank = (CardRank)Mathf.Clamp(cardData.rank - 1, 0, 12);
                    gizmo.type = CardGizmo.ParseType(cardData.type);
                    if (gizmo.type == CardType.Extra) {
                        gizmo.extraType = CardGizmo.ParseExtraType(cardData.extraType);
                    } else {
                        gizmo.extraType = CardExtraType.None;
                    }
                    gizmo.obstacle = CardGizmo.ParseObstacle(cardData.obstacle);
                    gizmo.layer = cardData.layer;
                    gizmo.showFaceDetails = true;
                    gizmo.tutorialStep = 0;

                    if (tool.cardSpriteData != null)
                    {
                        gizmo.spriteData = tool.cardSpriteData;
                    }

                    gizmo.UpdateVisuals();
                    gizmo.UpdateSorting();
                    idToGizmo[cardData.id] = gizmo;
                }
            }
        }

        // Generate Draw Pile and Check Card
        TutorialDataHelper.GenerateExtraObjects(tool, level);

        // Load Tutorial Config (tap_card, tap_drawpile, tap_undo, tap_joker)
        TutorialDataHelper.LoadTutorialConfig(tool, level, idToGizmo);

        tool.targetLevelId = level.id.ToString();
        tool.targetType = level.type;
        tool.targetDifficulty = level.difficulty;
        tool.targetMode = level.mode;
        tool.UpdateLevelText();

        EditorUtility.SetDirty(tool);
        EditorApplication.delayCall += () =>
        {
            SceneView.RepaintAll();
        };
        Debug.Log($"Generated Level {level.id} into scene successfully with {tool.tutorialSteps.Count} tap_card steps (drawpile:{tool.tapDrawPile}, undo:{tool.tapUndo}, joker:{tool.tapJoker}).");
    }
}
