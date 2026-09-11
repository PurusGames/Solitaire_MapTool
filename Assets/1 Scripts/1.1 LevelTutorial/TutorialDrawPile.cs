using UnityEngine;
using System.Collections.Generic;

public class TutorialDrawPile : MonoBehaviour
{
    public int count = 10;
    public float cardSpacing = 20f;

    public void LoadData(TutorialDrawPileData data, GameObject cardPrefab, float positionMultiplier)
    {
        count = data.count;
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (data.fixedCards != null && cardPrefab != null)
        {
            for (int i = 0; i < data.fixedCards.Count; i++)
            {
                var fCard = data.fixedCards[i];
                GameObject go = Instantiate(cardPrefab, transform);
                go.name = fCard.id;

                float xOffset = -(i * cardSpacing) / positionMultiplier;
                go.transform.localPosition = new Vector3(xOffset, 0, -i);
                
                CardGizmo gizmo = go.GetComponent<CardGizmo>();
                if (gizmo != null)
                {
                    gizmo.type = fCard.type;
                    gizmo.suit = TutorialCheckCard.ParseSuit(fCard.suit);
                    gizmo.rank = (CardRank)Mathf.Clamp(fCard.rank - 1, 0, 12);
                    gizmo.layer = i;
                    gizmo.showFaceDetails = true;
                    gizmo.UpdateVisuals();
                    gizmo.UpdateSorting();
                }
            }
        }
    }

    public TutorialDrawPileData SaveData()
    {
        TutorialDrawPileData data = new TutorialDrawPileData();
        data.count = count;
        data.fixedCards = new List<TutorialFixedCard>();

        CardGizmo[] gizmos = GetComponentsInChildren<CardGizmo>();
        for (int i = 0; i < gizmos.Length; i++)
        {
            var gizmo = gizmos[i];
            data.fixedCards.Add(new TutorialFixedCard()
            {
                id = $"draw-{i}",
                type = string.IsNullOrEmpty(gizmo.type) ? "normal" : gizmo.type,
                suit = gizmo.suit.ToString().ToLower(),
                rank = (int)gizmo.rank + 1
            });
        }
        
        return data;
    }
}
