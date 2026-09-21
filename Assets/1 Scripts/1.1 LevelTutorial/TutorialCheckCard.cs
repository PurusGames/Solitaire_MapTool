using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TutorialCheckCard : MonoBehaviour
{
    public string id = "check-0";
    public CardType type = CardType.Normal;

    public void LoadData(TutorialCheckCardData data, GameObject cardPrefab)
    {
        if (data != null)
        {
            id = data.id;
            type = CardGizmo.ParseType(data.type);
        }

        CardGizmo gizmo = GetComponentInChildren<CardGizmo>();
#if UNITY_EDITOR
        // If an existing child is not a prefab instance, remove it so we instantiate a proper prefab instance
        if (gizmo != null && !PrefabUtility.IsPartOfAnyPrefab(gizmo.gameObject))
        {
            Undo.DestroyObjectImmediate(gizmo.gameObject);
            gizmo = null;
        }
#endif

        if (gizmo == null && cardPrefab != null)
        {
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
            Undo.RegisterCreatedObjectUndo(go, "Create CheckCard Visual");
#else
            go = Instantiate(cardPrefab, transform);
#endif
            go.name = "checkCard_visual";
            go.transform.localPosition = Vector3.zero;
            gizmo = go.GetComponent<CardGizmo>();
        }

        if (gizmo != null)
        {
            if (data != null)
            {
                gizmo.suit = ParseSuit(data.suit);
                gizmo.rank = (CardRank)Mathf.Clamp(data.rank - 1, 0, 12);
                gizmo.type = CardGizmo.ParseType(data.type);
            }
            gizmo.showFaceDetails = true;
            gizmo.UpdateVisuals();
            gizmo.UpdateSorting();
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
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
            type = gizmo.type;
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        return new TutorialCheckCardData()
        {
            id = id,
            type = CardGizmo.TypeToString(type),
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
