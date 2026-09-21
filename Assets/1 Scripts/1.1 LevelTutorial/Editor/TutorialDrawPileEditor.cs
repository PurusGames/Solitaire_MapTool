using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TutorialDrawPile))]
public class TutorialDrawPileEditor : Editor
{
    private ReorderableList cardList;
    private TutorialDrawPile pile;
    private TutorialMapTool tool;

    private void OnEnable()
    {
        pile = (TutorialDrawPile)target;
        tool = FindObjectOfType<TutorialMapTool>();

        cardList = new ReorderableList(serializedObject, serializedObject.FindProperty("fixedCards"), true, true, true, true);
        
        cardList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Fixed Cards Sequence (Drag to Reorder)");
        };

        cardList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = cardList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            
            SerializedProperty suitProp = element.FindPropertyRelative("suit");
            SerializedProperty rankProp = element.FindPropertyRelative("rank");
            float width = rect.width / 3;
            
            EditorGUI.BeginChangeCheck();
            
            CardSuit currentSuit = TutorialCheckCard.ParseSuit(suitProp.stringValue);
            CardSuit newSuit = (CardSuit)EditorGUI.EnumPopup(new Rect(rect.x, rect.y, width - 5, EditorGUIUtility.singleLineHeight), currentSuit);
            if (currentSuit != newSuit) suitProp.stringValue = newSuit.ToString().ToLower();
            
            int currentRankIdx = Mathf.Clamp(rankProp.intValue - 1, 0, 12);
            CardRank currentRank = (CardRank)currentRankIdx;
            CardRank newRank = (CardRank)EditorGUI.EnumPopup(new Rect(rect.x + width, rect.y, width - 5, EditorGUIUtility.singleLineHeight), currentRank);
            if (currentRank != newRank) rankProp.intValue = (int)newRank + 1;
            
            SerializedProperty typeProp = element.FindPropertyRelative("type");
            CardType currentType = CardGizmo.ParseType(typeProp.stringValue);
            CardType newType = (CardType)EditorGUI.EnumPopup(new Rect(rect.x + width * 2, rect.y, width - 5, EditorGUIUtility.singleLineHeight), currentType);
            if (currentType != newType) typeProp.stringValue = CardGizmo.TypeToString(newType);
            
            if (EditorGUI.EndChangeCheck())
            {
                // Serialized properties updated
            }
        };

        cardList.onChangedCallback = (ReorderableList list) => {
            serializedObject.ApplyModifiedProperties();
            EditorApplication.delayCall += () => { if (this != null) RefreshVisuals(); };
        };

        cardList.onAddCallback = (ReorderableList list) => {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(pile, "Add Fixed Card");
            pile.fixedCards.Add(new TutorialFixedCard { id = $"draw-{pile.fixedCards.Count}", type = "normal", suit = "heart", rank = 1 });
            serializedObject.Update();
            EditorApplication.delayCall += () => { if (this != null) RefreshVisuals(); };
        };

        cardList.onRemoveCallback = (ReorderableList list) => {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(pile, "Remove Fixed Card");
            if (list.index >= 0 && list.index < pile.fixedCards.Count)
            {
                pile.fixedCards.RemoveAt(list.index);
            }
            serializedObject.Update();
            EditorApplication.delayCall += () => { if (this != null) RefreshVisuals(); };
        };
    }

    private void RefreshVisuals()
    {
        if (tool == null) tool = FindObjectOfType<TutorialMapTool>();
        if (tool != null && pile != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(pile.gameObject, "Update Visuals");
            pile.RefreshVisuals(tool.cardPrefab, tool.positionMultiplier, tool.cardSpriteData);
            SceneView.RepaintAll();
        }
    }

    private void AddNewCardPrefabToPile()
    {
        if (tool == null) tool = FindObjectOfType<TutorialMapTool>();
        if (tool == null || tool.cardPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Missing Card Prefab in TutorialMapTool!", "OK");
            return;
        }

        Undo.RecordObject(pile, "Add Card to Draw Pile");

        int newIdx = pile.transform.childCount;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(tool.cardPrefab);
        go.transform.SetParent(pile.transform, false);
        go.name = $"draw-{newIdx}";

        float mult = tool.positionMultiplier > 0 ? tool.positionMultiplier : 100f;
        go.transform.localPosition = new Vector3(-(newIdx * pile.cardSpacing) / mult, 0, newIdx);

        CardGizmo gizmo = go.GetComponent<CardGizmo>();
        if (gizmo != null)
        {
            gizmo.cardId = -(newIdx + 1);
            gizmo.suit = CardSuit.Heart;
            gizmo.rank = CardRank.Ace;
            gizmo.type = CardType.Normal;
            gizmo.layer = -newIdx;
            gizmo.showFaceDetails = true;
            if (tool.cardSpriteData != null) gizmo.spriteData = tool.cardSpriteData;
            gizmo.UpdateVisuals();
            gizmo.UpdateSorting();
        }

        Undo.RegisterCreatedObjectUndo(go, "Create DrawPile Card");
        pile.SaveData();
        serializedObject.Update();
        Selection.activeGameObject = go;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("count"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardSpacing"));

        GUILayout.Space(10);
        cardList.DoLayoutList();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            EditorApplication.delayCall += () => { if (this != null) RefreshVisuals(); };
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Scene Actions:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("helpbox");

        GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
        if (GUILayout.Button("+ Add Card Prefab to Draw Pile", GUILayout.Height(30)))
        {
            AddNewCardPrefabToPile();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(3);
        if (GUILayout.Button("Realign & Sync From Scene Cards", GUILayout.Height(25)))
        {
            if (tool == null) tool = FindObjectOfType<TutorialMapTool>();
            float mult = tool != null ? tool.positionMultiplier : 100f;
            pile.RealignChildrenPositions(mult);
            pile.SaveData();
            serializedObject.Update();
            SceneView.RepaintAll();
        }

        GUILayout.Space(6);
        GUI.backgroundColor = new Color(0.6f, 0.95f, 0.6f);
        if (GUILayout.Button("🎯 + Add Draw Pile to Tutorial Steps", GUILayout.Height(28)))
        {
            if (tool == null) tool = FindObjectOfType<TutorialMapTool>();
            if (tool != null)
            {
                tool.AddDrawPileStep();
                Debug.Log("Added Tap Draw Pile tutorial step.");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "TutorialMapTool not found in scene.", "OK");
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
    }
}
