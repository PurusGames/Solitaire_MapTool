using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class TutorialCardData
{
public int id;
    public string type;
    public string extraType;
    public string suit;
    public int rank;
    public string obstacle;
    public float x;
    public float y;
    public float angle;
    public int layer;
}

[System.Serializable]
public class ManualConfig
{
    public List<TutorialCardData> cards;
}

[System.Serializable]
public class TutorialCheckCardData
{
    public string id;
    public string type;
    public string suit;
    public int rank;
}

[System.Serializable]
public class TutorialFixedCard 
{
    public string id;
    public string type;
    public string suit;
    public int rank;
}

[System.Serializable]
public class TutorialDrawPileData
{
    public int count;
    public List<TutorialFixedCard> fixedCards;
}

[System.Serializable]
public class TutorialInstruction
{
    public string type;
    public int cardId;
}

[System.Serializable]
public class TutorialConfig
{
    public List<TutorialInstruction> instructions;
}

public enum TutorialStepType
{
    TapCard,
    TapDrawPile
}

[System.Serializable]
public class TutorialStepItem
{
    public TutorialStepType stepType = TutorialStepType.TapCard;
    public CardGizmo targetCard;
    [HideInInspector]
    public int cardId;
}

[System.Serializable]
public class TutorialLevelData
{
    public int id;
    public string type;
    public string mode;
    public string difficulty;
    public ManualConfig manualConfig;
    public TutorialCheckCardData checkCardData;
    public TutorialDrawPileData drawPileData;
    public TutorialConfig tutorialConfig;
}

public class TutorialMapTool : MonoBehaviour
{
    [Header("Config Files")]
    public string jsonPath = "";

    [HideInInspector]
    public TutorialLevelData[] loadedLevels;

    [Header("References")]
    public GameObject cardPrefab; 
    public CardSpriteData cardSpriteData;
    
    [Header("Sub-tool References")]
    public TutorialCheckCard checkCardObj;
    public TutorialDrawPile drawPileObj;
    public Text levelTextIndicator;
    
    [Header("Current Target")]
    [Tooltip("Leave empty to auto-increment max ID")]
    public string targetLevelId = "";

    public string targetType = "tutorial"; // default
    public string targetDifficulty = "easy";
    public string targetMode = "classic";

    [Header("Generation Settings")]
    public float positionMultiplier = 100f;
    public bool invertY = true;
    public bool invertAngle = true;
    public bool autoCenterOnSave = true;

    [Header("Tutorial Steps")]
    public List<TutorialStepItem> tutorialSteps = new List<TutorialStepItem>();

    private void OnValidate()
    {
        if (checkCardObj == null) checkCardObj = GetComponentInChildren<TutorialCheckCard>();
        if (checkCardObj == null) checkCardObj = FindObjectOfType<TutorialCheckCard>();

        if (drawPileObj == null) drawPileObj = GetComponentInChildren<TutorialDrawPile>();
        if (drawPileObj == null) drawPileObj = FindObjectOfType<TutorialDrawPile>();

        UpdateLevelText();
    }

    public TutorialDrawPile GetDrawPile()
    {
        if (drawPileObj == null)
        {
            drawPileObj = GetComponentInChildren<TutorialDrawPile>();
            if (drawPileObj == null) drawPileObj = FindObjectOfType<TutorialDrawPile>();
        }
        return drawPileObj;
    }

    public TutorialCheckCard GetCheckCard()
    {
        if (checkCardObj == null)
        {
            checkCardObj = GetComponentInChildren<TutorialCheckCard>();
            if (checkCardObj == null) checkCardObj = FindObjectOfType<TutorialCheckCard>();
        }
        return checkCardObj;
    }

    public void UpdateLevelText()
    {
        if (levelTextIndicator != null)
        {
            string levelStr = string.IsNullOrEmpty(targetLevelId) ? "NEW LEVEL" : targetLevelId;
#if UNITY_EDITOR
            if (levelTextIndicator.text != "Level: " + levelStr)
            {
                UnityEditor.Undo.RecordObject(levelTextIndicator, "Update Level Text");
                levelTextIndicator.text = "Level: " + levelStr;
                UnityEditor.EditorUtility.SetDirty(levelTextIndicator);
            }
#endif
        }
    }

    public int GetNextStepNumber()
    {
        return tutorialSteps.Count + 1;
    }

    public void SyncTutorialSteps()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Sync Tutorial Steps");
#endif
        // 1. Reset tutorialStep on all canvas cards
        CardGizmo[] allCards = GetComponentsInChildren<CardGizmo>();
        foreach (var card in allCards)
        {
            if (checkCardObj != null && card.transform.IsChildOf(checkCardObj.transform)) continue;
            if (drawPileObj != null && card.transform.IsChildOf(drawPileObj.transform)) continue;
            if (card.GetComponent<TutorialCheckCard>() != null || card.gameObject.name == "CheckCardData") continue;

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(card, "Reset Card Step");
#endif
            card.tutorialStep = 0;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(card);
#endif
        }

        // 2. Remove invalid card steps where targetCard was destroyed
        tutorialSteps.RemoveAll(s => s.stepType == TutorialStepType.TapCard && s.targetCard == null);

        // 3. Assign step index (1-based) to each step's card
        for (int i = 0; i < tutorialSteps.Count; i++)
        {
            var step = tutorialSteps[i];
            if (step.stepType == TutorialStepType.TapCard && step.targetCard != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(step.targetCard, "Set Card Step");
#endif
                step.targetCard.tutorialStep = i + 1;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(step.targetCard);
#endif
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    public void AddCardStep(CardGizmo card)
    {
        if (card == null) return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Add Card Tutorial Step");
#endif
        // Remove existing step for this card if present so it moves to next
        tutorialSteps.RemoveAll(s => s.stepType == TutorialStepType.TapCard && s.targetCard == card);

        tutorialSteps.Add(new TutorialStepItem
        {
            stepType = TutorialStepType.TapCard,
            targetCard = card
        });

        SyncTutorialSteps();
    }

    public void SetCardStep(CardGizmo card, int stepNumber)
    {
        if (card == null) return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Set Card Tutorial Step");
#endif
        tutorialSteps.RemoveAll(s => s.stepType == TutorialStepType.TapCard && s.targetCard == card);

        int insertIndex = Mathf.Clamp(stepNumber - 1, 0, tutorialSteps.Count);
        tutorialSteps.Insert(insertIndex, new TutorialStepItem
        {
            stepType = TutorialStepType.TapCard,
            targetCard = card
        });

        SyncTutorialSteps();
    }

    public void RemoveCardStep(CardGizmo card)
    {
        if (card == null) return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Remove Card Tutorial Step");
#endif
        tutorialSteps.RemoveAll(s => s.stepType == TutorialStepType.TapCard && s.targetCard == card);
        SyncTutorialSteps();
    }

    public void AddDrawPileStep()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Add Draw Pile Tutorial Step");
#endif
        tutorialSteps.Add(new TutorialStepItem
        {
            stepType = TutorialStepType.TapDrawPile,
            targetCard = null
        });

        SyncTutorialSteps();
    }

    public void MoveStep(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= tutorialSteps.Count) return;
        if (toIndex < 0 || toIndex >= tutorialSteps.Count) return;

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Move Tutorial Step");
#endif
        var item = tutorialSteps[fromIndex];
        tutorialSteps.RemoveAt(fromIndex);
        tutorialSteps.Insert(toIndex, item);

        SyncTutorialSteps();
    }

    public void RemoveStepAt(int index)
    {
        if (index < 0 || index >= tutorialSteps.Count) return;

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Remove Tutorial Step");
#endif
        tutorialSteps.RemoveAt(index);
        SyncTutorialSteps();
    }

    public void ClearTutorialSteps()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Clear Tutorial Steps");
#endif
        tutorialSteps.Clear();
        SyncTutorialSteps();
    }

    public Vector3 GetStepWorldPosition(TutorialStepItem step)
    {
        if (step == null) return Vector3.zero;
        if (step.stepType == TutorialStepType.TapCard && step.targetCard != null)
        {
            return step.targetCard.transform.position;
        }
        if (step.stepType == TutorialStepType.TapDrawPile && drawPileObj != null)
        {
            return drawPileObj.transform.position;
        }
        return Vector3.zero;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        string levelStr = string.IsNullOrEmpty(targetLevelId) ? "NEW LEVEL" : targetLevelId;
        
        if (levelTextIndicator == null)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.fontSize = 30;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, "Level: " + levelStr, style);
        }

        // Draw path connecting tutorial steps
        if (tutorialSteps != null && tutorialSteps.Count > 0)
        {
            for (int i = 0; i < tutorialSteps.Count; i++)
            {
                var step = tutorialSteps[i];
                if (step.stepType == TutorialStepType.TapDrawPile && drawPileObj != null)
                {
                    Vector3 pos = drawPileObj.transform.position + Vector3.up * 0.7f;
                    UnityEditor.Handles.color = new Color(0.2f, 0.7f, 1f, 0.9f);
                    UnityEditor.Handles.DrawSolidDisc(pos, Vector3.forward, 0.35f);
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.DrawWireDisc(pos, Vector3.forward, 0.35f);

                    GUIStyle dpStyle = new GUIStyle();
                    dpStyle.normal.textColor = Color.white;
                    dpStyle.fontSize = 13;
                    dpStyle.fontStyle = FontStyle.Bold;
                    dpStyle.alignment = TextAnchor.MiddleCenter;
                    UnityEditor.Handles.Label(pos, $"{i + 1}", dpStyle);
                }

                if (i < tutorialSteps.Count - 1)
                {
                    Vector3 p1 = GetStepWorldPosition(tutorialSteps[i]);
                    Vector3 p2 = GetStepWorldPosition(tutorialSteps[i + 1]);
                    if (p1 != Vector3.zero && p2 != Vector3.zero)
                    {
                        UnityEditor.Handles.color = new Color(0.2f, 0.85f, 1f, 0.8f);
                        UnityEditor.Handles.DrawDottedLine(p1, p2, 4f);

                        // Direction marker
                        Vector3 mid = (p1 + p2) * 0.5f;
                        Vector3 dir = (p2 - p1).normalized;
                        if (dir != Vector3.zero)
                        {
                            UnityEditor.Handles.ConeHandleCap(0, mid, Quaternion.LookRotation(dir, Vector3.forward), 0.25f, EventType.Repaint);
                        }
                    }
                }
            }
        }
    }
#endif
}
