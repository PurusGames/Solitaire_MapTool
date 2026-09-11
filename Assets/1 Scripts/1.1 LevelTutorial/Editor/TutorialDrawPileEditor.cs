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
            float width = rect.width / 2;
            
            EditorGUI.BeginChangeCheck();
            
            CardSuit currentSuit = TutorialCheckCard.ParseSuit(suitProp.stringValue);
            CardSuit newSuit = (CardSuit)EditorGUI.EnumPopup(new Rect(rect.x, rect.y, width - 5, EditorGUIUtility.singleLineHeight), currentSuit);
            if (currentSuit != newSuit) suitProp.stringValue = newSuit.ToString().ToLower();
            
            int currentRankIdx = Mathf.Clamp(rankProp.intValue - 1, 0, 12);
            CardRank currentRank = (CardRank)currentRankIdx;
            CardRank newRank = (CardRank)EditorGUI.EnumPopup(new Rect(rect.x + width, rect.y, width - 5, EditorGUIUtility.singleLineHeight), currentRank);
            if (currentRank != newRank) rankProp.intValue = (int)newRank + 1;
            
            if (EditorGUI.EndChangeCheck())
            {
                // We don't apply properties here, the OnInspectorGUI change check handles it.
            }
        };

        cardList.onChangedCallback = (ReorderableList list) => {
            serializedObject.ApplyModifiedProperties();
            EditorApplication.delayCall += () => { if (this != null) RefreshVisuals(); };
        };

        cardList.onAddCallback = (ReorderableList list) => {
            // It is usually better to let ReorderableList handle adding, but we want a default value.
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
        if (tool != null && pile != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(pile.gameObject, "Update Visuals");
            pile.RefreshVisuals(tool.cardPrefab, tool.positionMultiplier, tool.cardSpriteData);
            SceneView.RepaintAll();
        }
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
    }
}
