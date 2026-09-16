using UnityEngine;
using System.Collections.Generic;

public class TutorialDrawPile : MonoBehaviour
{
    public int count = 10;
    public float cardSpacing = 20f;
    public List<TutorialFixedCard> fixedCards = new List<TutorialFixedCard>();

    public void LoadData(TutorialDrawPileData data, GameObject cardPrefab, float positionMultiplier)
    {
        count = data.count;
        fixedCards = new List<TutorialFixedCard>();
        if (data.fixedCards != null)
        {
            foreach(var c in data.fixedCards) 
            {
               fixedCards.Add(new TutorialFixedCard { id = c.id, type = string.IsNullOrEmpty(c.type) ? "normal" : c.type, suit = c.suit, rank = c.rank });
            }
        }
    }

    public TutorialDrawPileData SaveData()
    {
        for (int i = 0; i < fixedCards.Count; i++)
        {
            fixedCards[i].id = $"draw-{i}";
        }
        
        return new TutorialDrawPileData()
        {
            count = count,
            fixedCards = new List<TutorialFixedCard>(fixedCards)
        };
    }

    public void RefreshVisuals(GameObject cardPrefab, float positionMultiplier, CardSpriteData spriteData)
    {
        if (cardPrefab == null) return;
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < fixedCards.Count; i++)
        {
            var fCard = fixedCards[i];
            GameObject go = Instantiate(cardPrefab, transform);
            go.name = $"draw-{i}";

            float xOffset = -(i * cardSpacing) / positionMultiplier;
            go.transform.localPosition = new Vector3(xOffset, 0, i);
            
            CardGizmo gizmo = go.GetComponent<CardGizmo>();
            if (gizmo != null)
            {
                gizmo.type = CardGizmo.ParseType(fCard.type);
                gizmo.suit = TutorialCheckCard.ParseSuit(fCard.suit);
                gizmo.rank = (CardRank)Mathf.Clamp(fCard.rank - 1, 0, 12);
                gizmo.layer = -i;
                gizmo.showFaceDetails = true;
                if (spriteData != null) gizmo.spriteData = spriteData;
                gizmo.UpdateVisuals();
                gizmo.UpdateSorting();
            }
        }
    }
}
