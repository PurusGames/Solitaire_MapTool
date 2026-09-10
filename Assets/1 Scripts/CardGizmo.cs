using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteInEditMode]
public class CardGizmo : MonoBehaviour
{
    [Header("Export Settings")]
    [HideInInspector]
    public int layer = 0;

    [Header("Collision Settings")]
    public float collisionScale = 0.8f;

    public Color gizmoColor = Color.green;

    private SpriteRenderer _spriteRenderer;

    private int _lastLayer = -9999;
    private float _lastZ = -9999f;

    private void Update()
    {
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
    }

    private void OnDrawGizmos()
    {
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

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = totalLayer;
        }

        if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;

        Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;
        Vector2 scaledSize = spriteSize * collisionScale;

        Gizmos.color = gizmoColor;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, scaledSize);
        Gizmos.matrix = oldMatrix;
        
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
        UnityEditor.Handles.Label(transform.position, "L:" + totalLayer, style);
#endif
    }
}
