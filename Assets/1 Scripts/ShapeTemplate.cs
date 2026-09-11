using UnityEngine;

[ExecuteInEditMode]
public class ShapeTemplate : MonoBehaviour
{
    [Header("Shape Definition (For shapes.json)")]
    [Tooltip("ID shape (VD: diamond_ring, cross_stack...)")]
    public string shapeId = "new_shape";
    
    [Header("Map Instance Data (For mapTemplates.json)")]
    [HideInInspector]
    public int baseLayer = 1;

    private int _lastLayer = -9999;
    private float _lastZ = -9999f;

    private void Update()
    {
        if (Application.isPlaying) return;

        bool zChanged = !Mathf.Approximately(_lastZ, transform.localPosition.z);
        bool layerChanged = _lastLayer != baseLayer;

        if (zChanged && !layerChanged)
        {
            baseLayer = Mathf.RoundToInt(-transform.localPosition.z);
            _lastLayer = baseLayer;
            _lastZ = transform.localPosition.z;
        }
        else if (layerChanged && !zChanged)
        {
            Vector3 pos = transform.localPosition;
            pos.z = -baseLayer;
            transform.localPosition = pos;
            _lastZ = pos.z;
            _lastLayer = baseLayer;
        }
        else if (zChanged && layerChanged)
        {
            _lastLayer = baseLayer;
            _lastZ = transform.localPosition.z;
        }
    }

//     private void OnDrawGizmos()
//     {
// #if UNITY_EDITOR
//         GUIStyle style = new GUIStyle();
//         style.normal.textColor = Color.yellow;
//         style.alignment = TextAnchor.MiddleCenter;
//         style.fontSize = 14;
//         UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, "ShapeBaseL:" + baseLayer, style);
// #endif
//     }
}
