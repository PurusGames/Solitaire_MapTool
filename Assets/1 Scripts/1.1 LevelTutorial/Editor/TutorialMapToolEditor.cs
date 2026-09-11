using UnityEngine;
using UnityEditor;
using System.IO;

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
                GUILayout.Label($"Level ID: {level.id} ({level.difficulty})", GUILayout.Width(180));
                
                if (GUILayout.Button("Generate In Scene", GUILayout.Width(160)))
                {
                    GenerateLevelInScene(tool, level);
                }
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


    private void ClearChildren(Transform t)
    {
        Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Clear Canvas");
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(t.GetChild(i).gameObject);
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

        // Find or create level
        var levelList = new System.Collections.Generic.List<TutorialLevelData>(tool.loadedLevels);
        TutorialLevelData existingLevel = levelList.Find(l => l.id == targetId);
        
        if (existingLevel == null)
        {
            if (EditorUtility.DisplayDialog("New Level", $"Level ID '{targetId}' not found in JSON. Add as new?", "Yes", "No"))
            {
                existingLevel = new TutorialLevelData 
                { 
                    id = targetId,
                    type = "tutorial",
                    mode = "classic",
                    difficulty = "easy",
                    tutorialConfig = new TutorialConfig { cards = new System.Collections.Generic.List<TutorialCardData>() },
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
            if (existingLevel.tutorialConfig == null) 
            {
                existingLevel.tutorialConfig = new TutorialConfig { cards = new System.Collections.Generic.List<TutorialCardData>() };
            }
        }

        // Collect cards from scene
        System.Collections.Generic.List<TutorialCardData> cards = new System.Collections.Generic.List<TutorialCardData>();
        CardGizmo[] gizmos = tool.GetComponentsInChildren<CardGizmo>();
        
        int cardIdCounter = 1;
        foreach (var gizmo in gizmos)
        {
            Vector3 localPos = tool.transform.InverseTransformPoint(gizmo.transform.position);
            float angle = gizmo.transform.localEulerAngles.z;
            if (angle > 180) angle -= 360f;

            cards.Add(new TutorialCardData()
            {
                id = cardIdCounter,
                type = string.IsNullOrEmpty(gizmo.type) ? "normal" : gizmo.type,
                suit = gizmo.suit.ToString().ToLower(),
                rank = (int)gizmo.rank + 1,
                obstacle = string.IsNullOrEmpty(gizmo.obstacle) ? "none" : gizmo.obstacle,
                x = Mathf.Round(localPos.x * tool.positionMultiplier * 100f) / 100f,
                y = Mathf.Round((tool.invertY ? -localPos.y : localPos.y) * tool.positionMultiplier * 100f) / 100f,
                angle = Mathf.Round((tool.invertAngle ? -angle : angle) * 100f) / 100f,
                layer = gizmo.layer
            });
            cardIdCounter++;
        }

        existingLevel.tutorialConfig.cards = cards;

        SaveJSONs(tool);
        
        Debug.Log($"Level '{targetId}' overridden & saved successfully with {cards.Count} cards!");
        EditorUtility.DisplayDialog("Success", $"Level '{targetId}' saved successfully to JSON!", "OK");
    }

    private void SaveJSONs(TutorialMapTool tool)
    {
        ArrayWrapper<TutorialLevelData> wrapper = new ArrayWrapper<TutorialLevelData> { Items = tool.loadedLevels };
        string json = JsonUtility.ToJson(wrapper, true);
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
            Undo.DestroyObjectImmediate(tool.transform.GetChild(i).gameObject);
        }

        if (level.tutorialConfig == null || level.tutorialConfig.cards == null)
        {
            Debug.LogWarning($"Level {level.id} has no tutorial config or cards.");
            return;
        }

        foreach (var cardData in level.tutorialConfig.cards)
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
                gizmo.type = cardData.type;
                gizmo.obstacle = cardData.obstacle;
                
                if (tool.cardSpriteData != null)
                {
                    gizmo.spriteData = tool.cardSpriteData;
                }
                
                gizmo.showFaceDetails = true;
                gizmo.UpdateVisuals();
                gizmo.UpdateSorting();
            }
        }

        Selection.activeGameObject = tool.gameObject;
        tool.targetLevelId = level.id.ToString();
        EditorUtility.SetDirty(tool);
        Debug.Log($"Generated Tutorial Level {level.id} in scene.");
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
