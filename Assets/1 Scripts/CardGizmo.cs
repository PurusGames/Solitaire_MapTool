using UnityEngine;
using UnityEngine.Rendering;
using System;

public enum CardExtraType
{
    None,
    Plus2,
    Plus3,
    Plus5
}

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SortingGroup))]
[ExecuteInEditMode]
public class CardGizmo : MonoBehaviour
{
    [Header("Export Settings")] [HideInInspector]
    public int layer = 0;

    [Header("Tutorial Settings")]
    public int tutorialStep = 0;
    [HideInInspector]
    public int cardId = 0;

    [Header("Card Visual Auto-Update")]
    public CardSpriteData spriteData;
    public bool showFaceDetails = false;
    public CardSuit suit = CardSuit.Heart;
    public CardRank rank = CardRank.Ace;
    
    public CardType type = CardType.Normal;
    public CardExtraType extraType = CardExtraType.None;
    public CardObstacle obstacle = CardObstacle.None;

    [Header("Visual References")]
    public SpriteRenderer rankRenderer;
    public SpriteRenderer suitRenderer1;
    public SpriteRenderer suitRenderer2;
    public SpriteRenderer backRenderer;
    public SpriteRenderer typeRenderer;
    public SpriteRenderer obstacleRenderer;

    [Header("Collision Settings")]
    public float collisionScale = 0.8f;
    public Color gizmoColor = Color.green;

    private SpriteRenderer _spriteRenderer;
    private SortingGroup _sortingGroup;

    private int _lastLayer = -9999;

    private CardSuit _lastSuit = (CardSuit)(-1);
    private CardRank _lastRank = (CardRank)(-1);
    private CardType _lastType = (CardType)(-1);
    private CardExtraType _lastExtraType = (CardExtraType)(-1);
    private CardObstacle _lastObstacle = (CardObstacle)(-1);
    private CardSpriteData _lastSpriteData;
    private bool _lastShowFaceDetails = true;

    public TutorialMapTool GetTutorialMapTool()
    {
        TutorialMapTool tool = GetComponentInParent<TutorialMapTool>();
        if (tool == null) tool = FindObjectOfType<TutorialMapTool>();
        return tool;
    }

    private void Update()
    {
        if (!Application.isPlaying) 
        {
            UpdateVisuals();
            UpdateSorting();
        }

        if (Application.isPlaying) return;

        if (_sortingGroup == null)
        {
            _sortingGroup = GetComponent<SortingGroup>();
        }

        if (_sortingGroup != null)
        {
            bool layerChanged = _lastLayer != layer;
            bool sortingChanged = _sortingGroup.sortingOrder != layer;

            if (layerChanged && !sortingChanged)
            {
                _sortingGroup.sortingOrder = layer;
                _lastLayer = layer;
            }
            else if (sortingChanged && !layerChanged)
            {
                layer = _sortingGroup.sortingOrder;
                _lastLayer = layer;
            }
            else if (layerChanged && sortingChanged)
            {
                _lastLayer = layer;
                _sortingGroup.sortingOrder = layer;
            }
        }

        // Auto-generate name based on sibling index to avoid manual renaming (only for canvas / shape cards, not drawpile or checkcard)
        if (transform.parent != null && GetComponentInParent<TutorialDrawPile>() == null && GetComponentInParent<TutorialCheckCard>() == null)
        {
            string expectedName = "card_" + transform.GetSiblingIndex();
            if (gameObject.name != expectedName && !gameObject.name.EndsWith("(Clone)"))
            {
                gameObject.name = expectedName;
            }
            else if (gameObject.name.Contains("(Clone)"))
            {
                gameObject.name = "card_" + transform.GetSiblingIndex();
            }
        }
    }

    private void OnValidate()
    {
        _lastSuit = (CardSuit)(-1);
        _lastRank = (CardRank)(-1);
        _lastType = (CardType)(-1);
        _lastObstacle = (CardObstacle)(-1);
        _lastSpriteData = null;
        _lastShowFaceDetails = !showFaceDetails;
    }

    public void UpdateVisuals()
    {
        if (spriteData == null) return;
        if (_lastSuit == suit && _lastRank == rank && _lastType == type && _lastExtraType == extraType && _lastObstacle == obstacle && _lastSpriteData == spriteData && _lastShowFaceDetails == showFaceDetails) return;

        _lastSuit = suit;
        _lastRank = rank;
        _lastType = type;
        _lastExtraType = extraType;
        _lastObstacle = obstacle;
        _lastSpriteData = spriteData;
        _lastShowFaceDetails = showFaceDetails;

        bool isNormal = type == CardType.Normal || type == CardType.Extra;
        bool showStandardFace = showFaceDetails && (type == CardType.Normal);

        if (rankRenderer != null)
        {
            rankRenderer.enabled = showStandardFace;
            if (showStandardFace)
            {
                rankRenderer.sprite = spriteData.GetRankSprite(rank, suit);
            }
        }
        
        if (suitRenderer1 != null) 
        {
            suitRenderer1.enabled = showStandardFace;
            if (showStandardFace) suitRenderer1.sprite = spriteData.GetSuitSprite(suit);
        }
        if (suitRenderer2 != null) 
        {
            suitRenderer2.enabled = showStandardFace;
            if (showStandardFace) suitRenderer2.sprite = spriteData.GetSuitSprite(suit);
        }

        if (backRenderer != null)
        {
            backRenderer.enabled = !showFaceDetails;
        }

        if (typeRenderer != null)
        {
            if (showFaceDetails && type != CardType.Normal)
            {
                typeRenderer.enabled = true;
                typeRenderer.sprite = spriteData.GetTypeSprite(type);
            }
            else
            {
                typeRenderer.enabled = false;
                typeRenderer.sprite = null;
            }
        }

        if (obstacleRenderer != null)
        {
            if (obstacle != CardObstacle.None)
            {
                obstacleRenderer.enabled = true;
                obstacleRenderer.sprite = spriteData.GetObstacleSprite(obstacle);
            }
            else
            {
                obstacleRenderer.enabled = false;
                obstacleRenderer.sprite = null;
            }
        }
    }
    
    public void UpdateSorting()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = 0;
        }

        if (rankRenderer != null) rankRenderer.sortingOrder = 1;
        if (suitRenderer1 != null) suitRenderer1.sortingOrder = 1;
        if (suitRenderer2 != null) suitRenderer2.sortingOrder = 1;
        
        if (backRenderer != null) backRenderer.sortingOrder = 1;
        if (typeRenderer != null) typeRenderer.sortingOrder = 2;
        if (obstacleRenderer != null) obstacleRenderer.sortingOrder = 3;
    }

    public static CardType ParseType(string t)
    {
        if (string.IsNullOrEmpty(t)) return CardType.Normal;
        try {
            return (CardType)Enum.Parse(typeof(CardType), t, true);
        } catch { return CardType.Normal; }
    }

    public static string TypeToString(CardType t)
    {
        return t.ToString().ToLower();
    }

    public static CardExtraType ParseExtraType(string t)
    {
        if (string.IsNullOrEmpty(t)) return CardExtraType.None;
        try {
            return (CardExtraType)Enum.Parse(typeof(CardExtraType), t, true);
        } catch { return CardExtraType.None; }
    }

    public static string ExtraTypeToString(CardExtraType t)
    {
        return t.ToString().ToLower();
    }

    public static CardObstacle ParseObstacle(string o)
    {
        if (string.IsNullOrEmpty(o)) return CardObstacle.None;
        try {
            return (CardObstacle)Enum.Parse(typeof(CardObstacle), o, true);
        } catch { return CardObstacle.None; }
    }

    public static string ObstacleToString(CardObstacle o)
    {
        return o.ToString().ToLower();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (tutorialStep > 0)
        {
            Vector3 pos = transform.position + Vector3.up * 0.7f;
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.9f);
            UnityEditor.Handles.DrawSolidDisc(pos, Vector3.forward, 0.35f);
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawWireDisc(pos, Vector3.forward, 0.35f);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 13;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            UnityEditor.Handles.Label(pos, $"{tutorialStep}", style);
        }
    }
#endif
}
