using UnityEngine;

[System.Serializable]
public class SlotData
{
    public float x;
    public float y;
    public float angle;
    public int layer;
}

[System.Serializable]
public class ShapeObject
{
    public string id;
    public SlotData[] slots;
}

[System.Serializable]
public class ShapesRoot
{
    public ShapeObject[] shapes;
}

[RequireComponent(typeof(ShapeTemplate))]
public class ShapeTool : MonoBehaviour
{
    [Header("Config Paths")]
    public string shapesJsonPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/shapes.json";
    
    [HideInInspector]
    public ShapesRoot loadedShapes;

    [Header("References")]
    public GameObject cardPrefab;

    [Header("Export Settings")]
    public float positionMultiplier = 100f;
    public bool invertY = true;
    public bool invertAngle = true;
}
