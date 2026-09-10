using UnityEngine;

[RequireComponent(typeof(ShapeTemplate))]
public class ShapeTool : MonoBehaviour
{
    [Header("Config Paths")]
    public string shapesJsonPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/shapes.json";
    
    [Header("References")]
    public GameObject cardPrefab;

    [Header("Export Settings")]
    public float positionMultiplier = 100f;
    public bool invertY = true;
    public bool invertAngle = true;
}
