using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SortingGroup))]
[ExecuteInEditMode]
public class CardGizmo : MonoBehaviour
{
    [Header("Export Settings")] [HideInInspector]
    public int layer = 0;

    [Header("Card Visual Auto-Update")]
    public CardSpriteData spriteData;
    public CardSuit suit = CardSuit.Heart;
    public CardRank rank = CardRank.Ace;

    [Header("Visual References")]
    public SpriteRenderer rankRenderer;
    public SpriteRenderer suitRenderer1;
    public SpriteRenderer suitRenderer2;

    [Header("Collision Settings")]
    public float collisionScale = 0.8f;
    public Color gizmoColor = Color.green;

    private SpriteRenderer _spriteRenderer;
    private SortingGroup _sortingGroup;

    private int _lastLayer = -9999;
    private float _lastZ = -9999f;

    private CardSuit _lastSuit = (CardSuit)(-1);
    private CardRank _lastRank = (CardRank)(-1);
    private CardSpriteData _lastSpriteData;

    private void Update()
    {
        if (!Application.isPlaying) 
        {
            UpdateVisuals();
            UpdateSorting();
        }

        if (Application.isPlaying) return;

        bool zChanged = !Mathf.Approximately(_lastZ, transform.localPosition.z);
        bool layerChanged = _lastLayer != layer;

        if (zChanged && !layerChanged)
        {
            layer = Mathf.RoundToInt(-transform.localPosition.z);
            _lastLayer = layer;
            _lastZ = transform.localPosition.z;
        }
        else if (layerChanged && !zChanged)
        {
            Vector3 pos = transform.localPosition;
            pos.z = -layer;
            transform.localPosition = pos;
            _lastZ = pos.z;
            _lastLayer = layer;
        }
        else if (zChanged && layerChanged)
        {
            _lastLayer = layer;
            _lastZ = transform.localPosition.z;
        }

        // Auto-generate name based on sibling index to avoid manual renaming
        if (transform.parent != null)
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
        _lastSpriteData = null;
    }

    public void UpdateVisuals()
    {
        if (spriteData == null) return;
        if (_lastSuit == suit && _lastRank == rank && _lastSpriteData == spriteData) return;

        _lastSuit = suit;
        _lastRank = rank;
        _lastSpriteData = spriteData;

        if (rankRenderer != null)
        {
            rankRenderer.sprite = spriteData.GetRankSprite(rank, suit);
        }
        
        Sprite suitSprite = spriteData.GetSuitSprite(suit);
        if (suitRenderer1 != null) suitRenderer1.sprite = suitSprite;
        if (suitRenderer2 != null) suitRenderer2.sprite = suitSprite;
    }
    
    public void UpdateSorting()
    {
        if (_sortingGroup == null)
        {
            _sortingGroup = GetComponent<SortingGroup>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        int totalLayer = layer;
        ShapeTemplate parentShape = GetComponentInParent<ShapeTemplate>();
        if (parentShape != null)
        {
            totalLayer += parentShape.baseLayer;
        }
        if (_sortingGroup != null)
        {
            _sortingGroup.sortingOrder = totalLayer;
        }
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = 0;
        }

        if (rankRenderer != null) rankRenderer.sortingOrder = 1;
        if (suitRenderer1 != null) suitRenderer1.sortingOrder = 1;
        if (suitRenderer2 != null) suitRenderer2.sortingOrder = 1;
    }
}
