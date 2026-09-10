using UnityEngine;

public class ShapeTemplate : MonoBehaviour
{
    [Header("Shape Definition (For shapes.json)")]
    [Tooltip("ID của shape (VD: diamond_ring, cross_stack...)")]
    public string shapeId = "new_shape";
    
    [Header("Map Instance Data (For mapTemplates.json)")]
    [Tooltip("Base layer khi shape này được đặt trong một Map")]
    public int baseLayer = 1;
}
