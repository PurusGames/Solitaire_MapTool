using UnityEngine;

[RequireComponent(typeof(MapTemplate))]
public class MapTool : MonoBehaviour
{
    [Header("Config Paths")]
    public string mapsJsonPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/mapTemplates.json";
    
    [Header("References")]
    public GameObject shapePrefab;

    [Header("Export Settings")]
    public float positionMultiplier = 100f;
    public bool invertY = true;
    public bool invertAngle = true;
    
    [Header("Auto Format")]
    [Tooltip("If true, the map will be automatically centered at (0,0) before saving.")]
    public bool autoCenterOnSave = true;
}
