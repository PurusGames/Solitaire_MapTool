using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CardGizmo : MonoBehaviour
{
    [Header("Export Settings")]
    public int layer = 0;

    [Header("Collision Settings")]
    public float collisionScale = 0.8f;

    public Color gizmoColor = Color.green;

    private SpriteRenderer _spriteRenderer;

    private void OnDrawGizmos()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer.sprite == null) return;

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
        UnityEditor.Handles.Label(transform.position, "L:" + layer, style);
#endif
    }
}
