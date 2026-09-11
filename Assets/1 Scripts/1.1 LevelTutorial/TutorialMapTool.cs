using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class TutorialCardData
{
    public int id;
    public string type;
    public string suit;
    public int rank;
    public string obstacle;
    public float x;
    public float y;
    public float angle;
    public int layer;
}

[System.Serializable]
public class TutorialConfig
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
public class TutorialLevelData
{
    public int id;
    public string type;
    public string mode;
    public string difficulty;
    public TutorialConfig tutorialConfig;
    public TutorialCheckCardData checkCardData;
    public TutorialDrawPileData drawPileData;
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
    
    [Header("Generation Settings")]
    [Header("Current Target")]
    [Tooltip("Leave empty to auto-increment max ID")]
    public string targetLevelId = "";

    [Header("Generation Settings")]
    public float positionMultiplier = 100f;
    public bool invertY = true;
    public bool invertAngle = true;
    public bool autoCenterOnSave = true;


    private void OnValidate()
    {
        UpdateLevelText();
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
    }
#endif
}
