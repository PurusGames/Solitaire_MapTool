using UnityEngine;

public enum CardSuit { Heart, Diamond, Club, Spade }
public enum CardRank { Ace, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

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
}
