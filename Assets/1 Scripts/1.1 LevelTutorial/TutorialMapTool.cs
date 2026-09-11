using UnityEngine;
using System.Collections.Generic;

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

    [Header("References")]
    public GameObject cardPrefab; 
    public CardSpriteData cardSpriteData;
    
    [Header("Generation Settings")]
    public float positionMultiplier = 100f;
    public bool invertY = true;
    public bool invertAngle = true;
}
