using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(TutorialMapTool))]
public class TutorialMapToolEditor : Editor
{
    private Vector2 scrollPos;

    [System.Serializable]
    private class ArrayWrapper<T>
    {
        public T[] Items;
    }

    public override void OnInspectorGUI()
    {
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
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
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
                    if (level.type == "tutorial" && level.tutorialConfig == null) 
                    {
                        level.tutorialConfig = new TutorialConfig 
                        {
                            instructions = new System.Collections.Generic.List<TutorialInstruction> 
                            {
                                new TutorialInstruction { type = "tap_card", cardId = 2 },
                                new TutorialInstruction { type = "tap_drawpile", cardId = 0 }
                            }
                        };
                    } 
                    else if (level.type != "tutorial") 
                    {
                        level.tutorialConfig = null;
                    }
                    SaveJSONs(tool);
                    GUIUtility.ExitGUI();
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
                        var list = new System.Collections.Generic.List<TutorialLevelData>(tool.loadedLevels);
                        list.Remove(level);
                        tool.loadedLevels = list.ToArray();
                        SaveJSONs(tool);
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;
                
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

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

    private void CenterMap(TutorialMapTool tool)
    {
        CardGizmo[] allGizmos = tool.GetComponentsInChildren<CardGizmo>();
        System.Collections.Generic.List<CardGizmo> canvasCards = new System.Collections.Generic.List<CardGizmo>();
        
        foreach (var gizmo in allGizmos)
        {
            if (tool.checkCardObj != null && gizmo.transform.IsChildOf(tool.checkCardObj.transform)) continue;
            if (tool.drawPileObj != null && gizmo.transform.IsChildOf(tool.drawPileObj.transform)) continue;
            if (gizmo.GetComponent<TutorialCheckCard>() != null || gizmo.gameObject.name == "CheckCardData") continue;

            canvasCards.Add(gizmo);
        }

        if (canvasCards.Count == 0)
        {
            Debug.LogWarning("No cards found in canvas! Cannot determine map center.");
            return;
        }

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, 0);

        foreach (var card in canvasCards)
        {
            Vector3 localPos = tool.transform.InverseTransformPoint(card.transform.position);
            if (localPos.x < min.x) min.x = localPos.x;
            if (localPos.x > max.x) max.x = localPos.x;
            if (localPos.y < min.y) min.y = localPos.y;
            if (localPos.y > max.y) max.y = localPos.y;
        }

        Vector3 center = (min + max) / 2f;

        if (center.sqrMagnitude < 0.0001f)
        {
            Debug.Log("Map is already centered.");
            return;
        }

        System.Collections.Generic.List<Transform> transformsToMove = new System.Collections.Generic.List<Transform>();
        foreach (var card in canvasCards)
        {
            transformsToMove.Add(card.transform);
        }
        Undo.RecordObjects(transformsToMove.ToArray(), "Center Map");

        foreach (var card in canvasCards)
        {
            card.transform.localPosition -= center;
        }

        Debug.Log($"Map centered! Applied offset: {-center}");
        SceneView.RepaintAll();
    }

    private void ClearChildren(Transform t)
    {
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
        
        // Use standard tool defaults when clearing
        if (tool != null) 
        {
            var defaultLevel = new TutorialLevelData();
            defaultLevel.checkCardData = new TutorialCheckCardData { id = "check-0", type = "normal", suit = "spade", rank = 4 };
            defaultLevel.drawPileData = new TutorialDrawPileData { count = 10, fixedCards = new System.Collections.Generic.List<TutorialFixedCard>() };
            UnityEditor.TutorialDataHelper.GenerateExtraObjects(tool, defaultLevel);
        }
    }

    private void AddNewCard(TutorialMapTool tool)
    {
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
            gizmo.UpdateVisuals();
        }
        
        Selection.activeGameObject = newCard;
        Undo.RegisterCreatedObjectUndo(newCard, "Add Card");
    }

    private void SaveTutorialMapToJSON(TutorialMapTool tool)
    {
        if (tool.loadedLevels == null)
        {
            EditorUtility.DisplayDialog("Error", "Levels not loaded! Please load JSON first.", "OK");
            return;
        }

        if (tool.autoCenterOnSave)
        {
            CenterMap(tool);
        }

        int targetId = 0;
        if (string.IsNullOrEmpty(tool.targetLevelId))
        {
            int maxId = 0;
            foreach (var l in tool.loadedLevels)
            {
                if (l.id > maxId) maxId = l.id;
            }
            targetId = maxId + 1;
            tool.targetLevelId = targetId.ToString();
            tool.UpdateLevelText();
            EditorUtility.SetDirty(tool);
        }
        else
        {
            if (!int.TryParse(tool.targetLevelId, out targetId))
            {
                EditorUtility.DisplayDialog("Error", "Target Level ID must be an integer.", "OK");
                return;
            }
        }

        var levelList = new System.Collections.Generic.List<TutorialLevelData>(tool.loadedLevels);
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
                    manualConfig = new ManualConfig { cards = new System.Collections.Generic.List<TutorialCardData>() },
                    checkCardData = new TutorialCheckCardData { id = "check-0", type = "normal", suit = "spade", rank = 4 },
                    drawPileData = new TutorialDrawPileData 
                    { 
                        count = 10,
                        fixedCards = new System.Collections.Generic.List<TutorialFixedCard>()
                    }
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
                existingLevel.manualConfig = new ManualConfig { cards = new System.Collections.Generic.List<TutorialCardData>() };
            }
        }

        if (tool.targetType == "tutorial" && existingLevel.tutorialConfig == null)
        {
            existingLevel.tutorialConfig = new TutorialConfig 
            {
                instructions = new System.Collections.Generic.List<TutorialInstruction> 
                {
                    new TutorialInstruction { type = "tap_card", cardId = 2 },
                    new TutorialInstruction { type = "tap_drawpile", cardId = 0 }
                }
            };
        }
        else if (tool.targetType != "tutorial")
        {
            // Nullify tutorial config if it's not tutorial type
            existingLevel.tutorialConfig = null;
        }

        System.Collections.Generic.List<TutorialCardData> cards = new System.Collections.Generic.List<TutorialCardData>();
        CardGizmo[] gizmos = tool.GetComponentsInChildren<CardGizmo>();
        
        int cardIdCounter = 1;
        foreach (var gizmo in gizmos)
        {
            if (tool.checkCardObj != null && gizmo.transform.IsChildOf(tool.checkCardObj.transform)) continue;
            if (tool.drawPileObj != null && gizmo.transform.IsChildOf(tool.drawPileObj.transform)) continue;
            if (gizmo.GetComponent<TutorialCheckCard>() != null || gizmo.gameObject.name == "CheckCardData") continue;
            Vector3 localPos = tool.transform.InverseTransformPoint(gizmo.transform.position);
            float angle = gizmo.transform.localEulerAngles.z;
            if (angle > 180) angle -= 360f;

            cards.Add(new TutorialCardData()
            {
                id = cardIdCounter,
                type = CardGizmo.TypeToString(gizmo.type),
                suit = gizmo.suit.ToString().ToLower(),
                rank = (int)gizmo.rank + 1,
                obstacle = CardGizmo.ObstacleToString(gizmo.obstacle),
                x = Mathf.Round(localPos.x * tool.positionMultiplier * 100f) / 100f,
                y = Mathf.Round((tool.invertY ? -localPos.y : localPos.y) * tool.positionMultiplier * 100f) / 100f,
                angle = Mathf.Round((tool.invertAngle ? -angle : angle) * 100f) / 100f,
                layer = gizmo.layer
            });
            cardIdCounter++;
        }

        existingLevel.manualConfig.cards = cards;

        UnityEditor.TutorialDataHelper.SaveExtraObjects(tool, existingLevel);

        SaveJSONs(tool);
        
        Debug.Log($"Level '{targetId}' overridden & saved successfully with {cards.Count} cards!");
        EditorUtility.DisplayDialog("Success", $"Level '{targetId}' saved successfully to JSON!", "OK");
    }

    private void SaveJSONs(TutorialMapTool tool)
    {
        string json = string.Empty;
        if (tool.loadedLevels == null || tool.loadedLevels.Length == 0)
        {
            json = "[]";
        }
        else
        {
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
        }
        File.WriteAllText(tool.jsonPath, json);
        AssetDatabase.Refresh();
    }

    private void LoadJSONs(TutorialMapTool tool)
    {
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

    private void GenerateLevelInScene(TutorialMapTool tool, TutorialLevelData level)
    {
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

                cardGo.transform.localPosition = new Vector3(stX, stY, -cardData.layer);
                cardGo.transform.localEulerAngles = new Vector3(0, 0, stAngle);

                CardGizmo gizmo = cardGo.GetComponent<CardGizmo>();
                if (gizmo != null)
                {
                    gizmo.layer = cardData.layer;
                    gizmo.suit = ParseSuit(cardData.suit);
                    
                    int rankIdx = cardData.rank - 1;
                    rankIdx = Mathf.Clamp(rankIdx, 0, 12);
                    gizmo.rank = (CardRank)rankIdx;
                    gizmo.type = CardGizmo.ParseType(cardData.type);
                    gizmo.obstacle = CardGizmo.ParseObstacle(cardData.obstacle);
                    
                    if (tool.cardSpriteData != null)
                    {
                        gizmo.spriteData = tool.cardSpriteData;
                    }
                    
                    gizmo.showFaceDetails = true;
                    gizmo.UpdateVisuals();
                    gizmo.UpdateSorting();
                }
            }
        }

        UnityEditor.TutorialDataHelper.GenerateExtraObjects(tool, level);

        Selection.activeGameObject = tool.gameObject;
        tool.targetLevelId = level.id.ToString();
        tool.targetType = string.IsNullOrEmpty(level.type) ? "tutorial" : level.type;
        tool.targetMode = string.IsNullOrEmpty(level.mode) ? "classic" : level.mode;
        tool.targetDifficulty = string.IsNullOrEmpty(level.difficulty) ? "easy" : level.difficulty;
        tool.UpdateLevelText();
        EditorUtility.SetDirty(tool);
        Debug.Log($"Generated Level {level.id} [{tool.targetType}] in scene.");
    }

    private CardSuit ParseSuit(string suitStr)
    {
        if (string.IsNullOrEmpty(suitStr)) return CardSuit.Heart;
        switch (suitStr.ToLower())
        {
            case "heart": return CardSuit.Heart;
            case "spade": return CardSuit.Spade;
            case "diamond": return CardSuit.Diamond;
            case "club": return CardSuit.Club;
            default: return CardSuit.Heart;
        }
    }
}
