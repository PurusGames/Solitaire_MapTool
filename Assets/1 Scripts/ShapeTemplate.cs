using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(SortingGroup))]
public class ShapeTemplate : MonoBehaviour
{
    [Header("Shape Definition (For shapes.json)")]
    [Tooltip("ID shape (VD: diamond_ring, cross_stack...)")]
    public string shapeId = "new_shape";
    
    [Header("Map Instance Data (For mapTemplates.json)")]
    [HideInInspector]
    public int baseLayer = 1;

    private int _lastLayer = -9999;
    private SortingGroup _sortingGroup;

    private void Update()
    {
        if (Application.isPlaying) return;

        if (_sortingGroup == null)
        {
            _sortingGroup = GetComponent<SortingGroup>();
        }

        if (_sortingGroup != null)
        {
            bool layerChanged = _lastLayer != baseLayer;
            bool sortingChanged = _sortingGroup.sortingOrder != baseLayer;

            if (layerChanged && !sortingChanged)
            {
                _sortingGroup.sortingOrder = baseLayer;
                _lastLayer = baseLayer;
            }
            else if (sortingChanged && !layerChanged)
            {
                baseLayer = _sortingGroup.sortingOrder;
                _lastLayer = baseLayer;
            }
            else if (layerChanged && sortingChanged)
            {
                _lastLayer = baseLayer;
                _sortingGroup.sortingOrder = baseLayer;
            }
        }
    }
}
