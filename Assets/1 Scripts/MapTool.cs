using UnityEngine;

[System.Serializable]
public class MapSlotData
{
    public string id;
    public string shapeId;
    public float x;
    public float y;
    public float angle;
    public int baseLayer;
}

[System.Serializable]
public class MapObject
{
    public string id;
    public MapSlotData[] map;
}

[RequireComponent(typeof(MapTemplate))]
public class MapTool : MonoBehaviour
{
    [Header("Config Paths")]
    public string mapsJsonPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/mapTemplates.json";
    
    [HideInInspector]
    public MapObject[] loadedMaps;

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
