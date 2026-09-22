using UnityEngine;

public enum CardSuit { Heart, Diamond, Club, Spade }
public enum CardRank { Ace, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

public enum CardType
{
    Normal,
    Joker,
    Extra,
    Key
}

public enum CardObstacle
{
    None,
    // Frozen,
    Locked
}

[CreateAssetMenu(fileName = "CardSpriteData", menuName = "Solitaire Map Tool/Card Sprite Data")]
public class CardSpriteData : ScriptableObject
{
    [Header("Rank Sprites (0=Ace, 1=Two ... 12=King)")]
    public Sprite[] redRanks = new Sprite[13];
    public Sprite[] blackRanks = new Sprite[13];

    [Header("Suit Sprites")]
    public Sprite heartSprite;
    public Sprite diamondSprite;
    public Sprite clubSprite;
    public Sprite spadeSprite;

    [Header("Special Type Sprites")]
    public Sprite jokerSprite;
    [UnityEngine.Serialization.FormerlySerializedAs("extraSprite")]
    public Sprite extraPlus5Sprite;
    public Sprite extraPlus2Sprite;
    public Sprite extraPlus3Sprite;
    public Sprite keySprite;

    [Header("Obstacle Sprites")]
    // public Sprite frozenSprite;
    public Sprite lockedSprite;

    public Sprite GetSuitSprite(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Heart: return heartSprite;
            case CardSuit.Diamond: return diamondSprite;
            case CardSuit.Club: return clubSprite;
            case CardSuit.Spade: return spadeSprite;
            default: return null;
        }
    }

    public Sprite GetRankSprite(CardRank rank, CardSuit suit)
    {
        bool isRed = (suit == CardSuit.Heart || suit == CardSuit.Diamond);
        int index = (int)rank;
        if (isRed)
        {
            if (redRanks != null && index < redRanks.Length) return redRanks[index];
        }
        else
        {
            if (blackRanks != null && index < blackRanks.Length) return blackRanks[index];
        }
        return null;
    }

    public Sprite GetTypeSprite(CardType type, CardExtraType extraType = CardExtraType.None)
    {
        switch (type)
        {
            case CardType.Joker: return jokerSprite;
            case CardType.Extra:
                if (extraType == CardExtraType.Plus2) return extraPlus2Sprite;
                if (extraType == CardExtraType.Plus3) return extraPlus3Sprite;
                return extraPlus5Sprite;
            case CardType.Key: return keySprite;
            default: return null;
        }
    }

    public Sprite GetObstacleSprite(CardObstacle obstacle)
    {
        switch (obstacle)
        {
            // case CardObstacle.Frozen: return frozenSprite;
            case CardObstacle.Locked: return lockedSprite;
            default: return null;
        }
    }
}
