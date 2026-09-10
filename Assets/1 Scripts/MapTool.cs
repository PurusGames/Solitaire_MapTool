using UnityEngine;

[RequireComponent(typeof(MapTemplate))]
public class MapTool : MonoBehaviour
{
    [Header("Config Paths")]
    public string mapsJsonPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/mapTemplates.json";
    public string shapesJsonPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/shapes.json";
    
    [Header("References")]
    public GameObject cardPrefab;

    [Header("Export Settings")]
    public float positionMultiplier = 100f;
    public bool invertY = true;
    public bool invertAngle = true;
}
