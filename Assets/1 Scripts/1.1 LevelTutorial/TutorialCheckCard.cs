using UnityEngine;

public class TutorialCheckCard : MonoBehaviour
{
    public string id = "check-0";
    public string type = "normal";

        public void LoadData(TutorialCheckCardData data, GameObject cardPrefab)
    {
        id = data.id;
        type = string.IsNullOrEmpty(data.type) ? "normal" : data.type;
        
        if (transform.childCount == 0 && cardPrefab != null)
        {
            GameObject go = Instantiate(cardPrefab, transform);
            go.name = "checkCard_visual";
            go.transform.localPosition = Vector3.zero;
        }

        CardGizmo gizmo = GetComponentInChildren<CardGizmo>();
        if (gizmo != null)
        {
            gizmo.suit = ParseSuit(data.suit);
            gizmo.rank = (CardRank)Mathf.Clamp(data.rank - 1, 0, 12);
            gizmo.showFaceDetails = true;
            gizmo.UpdateVisuals();
        }
    }

    public TutorialCheckCardData SaveData()
    {
        CardGizmo gizmo = GetComponentInChildren<CardGizmo>();
        CardSuit suit = CardSuit.Heart;
        CardRank rank = CardRank.Ace;
        if (gizmo != null)
        {
            suit = gizmo.suit;
            rank = gizmo.rank;
        }

        return new TutorialCheckCardData()
        {
            id = id,
            type = type,
            suit = suit.ToString().ToLower(),
            rank = (int)rank + 1
        };
    }

    public static CardSuit ParseSuit(string suitStr)
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
