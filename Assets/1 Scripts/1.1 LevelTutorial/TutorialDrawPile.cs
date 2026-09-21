using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TutorialDrawPile : MonoBehaviour
{
    public int count = 10;
    public float cardSpacing = 20f;
    public List<TutorialFixedCard> fixedCards = new List<TutorialFixedCard>();

    public void LoadData(TutorialDrawPileData data, GameObject cardPrefab, float positionMultiplier)
    {
        if (data == null) return;

        count = data.count;
        fixedCards = new List<TutorialFixedCard>();
        if (data.fixedCards != null)
        {
            foreach (var c in data.fixedCards)
            {
                fixedCards.Add(new TutorialFixedCard
                {
                    id = c.id,
                    type = string.IsNullOrEmpty(c.type) ? "normal" : c.type,
                    suit = c.suit,
                    rank = c.rank
                });
            }
        }
    }

    public TutorialDrawPileData SaveData()
    {
        // 1. Inspect child CardGizmo objects in Scene hierarchy first
        List<CardGizmo> childGizmos = new List<CardGizmo>();
        for (int i = 0; i < transform.childCount; i++)
        {
            CardGizmo gizmo = transform.GetChild(i).GetComponent<CardGizmo>();
            if (gizmo != null)
            {
                childGizmos.Add(gizmo);
            }
        }

        if (childGizmos.Count > 0)
        {
            fixedCards.Clear();
            for (int i = 0; i < childGizmos.Count; i++)
            {
                var gizmo = childGizmos[i];
                string cardId = $"draw-{i}";
                gizmo.gameObject.name = cardId;
                gizmo.cardId = -(i + 1);

                fixedCards.Add(new TutorialFixedCard
                {
                    id = cardId,
                    type = CardGizmo.TypeToString(gizmo.type),
                    suit = gizmo.suit.ToString().ToLower(),
                    rank = (int)gizmo.rank + 1
                });
            }
        }
        else
        {
            for (int i = 0; i < fixedCards.Count; i++)
            {
                if (string.IsNullOrEmpty(fixedCards[i].id))
                {
                    fixedCards[i].id = $"draw-{i}";
                }
            }
        }

        count = Mathf.Max(count, fixedCards.Count);

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        return new TutorialDrawPileData()
        {
            count = count,
            fixedCards = new List<TutorialFixedCard>(fixedCards)
        };
    }

    public void RefreshVisuals(GameObject cardPrefab, float positionMultiplier, CardSpriteData spriteData)
    {
        if (cardPrefab == null) return;

        // Remove existing children cleanly
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(child);
#else
            DestroyImmediate(child);
#endif
        }

        float mult = positionMultiplier > 0 ? positionMultiplier : 100f;

        for (int i = 0; i < fixedCards.Count; i++)
        {
            var fCard = fixedCards[i];
            
            GameObject go;
#if UNITY_EDITOR
            go = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
            if (go == null)
            {
                go = Instantiate(cardPrefab, transform);
            }
            else
            {
                go.transform.SetParent(transform, false);
            }
            Undo.RegisterCreatedObjectUndo(go, "Create DrawPile Card");
#else
            go = Instantiate(cardPrefab, transform);
#endif
            go.name = $"draw-{i}";

            float xOffset = -(i * cardSpacing) / mult;
            go.transform.localPosition = new Vector3(xOffset, 0, i);

            CardGizmo gizmo = go.GetComponent<CardGizmo>();
            if (gizmo != null)
            {
                gizmo.cardId = -(i + 1);
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

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
#endif
    }

    public void RealignChildrenPositions(float positionMultiplier)
    {
        float mult = positionMultiplier > 0 ? positionMultiplier : 100f;
        int idx = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            CardGizmo gizmo = child.GetComponent<CardGizmo>();
            if (gizmo != null)
            {
                child.name = $"draw-{idx}";
                gizmo.cardId = -(idx + 1);
                child.localPosition = new Vector3(-(idx * cardSpacing) / mult, 0, idx);
                gizmo.layer = -idx;
                gizmo.UpdateSorting();
                idx++;
            }
        }
    }
}
