using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(TutorialMapTool))]
public class TutorialMapToolEditor : Editor
{
    private TutorialLevelData[] loadedLevels;
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

        if (loadedLevels != null && loadedLevels.Length > 0)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Loaded {loadedLevels.Length} Tutorial Levels:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("helpbox");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            foreach (var level in loadedLevels)
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
        loadedLevels = wrapper != null ? wrapper.Items : new TutorialLevelData[0];

        Debug.Log($"Loaded {loadedLevels.Length} tutorial levels.");
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
