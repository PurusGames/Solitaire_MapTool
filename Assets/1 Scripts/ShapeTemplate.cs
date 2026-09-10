using UnityEngine;

public class ShapeTemplate : MonoBehaviour
{
    [Header("Shape Definition (For shapes.json)")]
    [Tooltip("ID shape (VD: diamond_ring, cross_stack...)")]
    public string shapeId = "new_shape";
    
    [Header("Map Instance Data (For mapTemplates.json)")]
    public int baseLayer = 1;
}
