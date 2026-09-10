using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class PixiExportTool : EditorWindow
{
    private string shapesPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/shapes.json";
    private string mapsPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/mapTemplates.json";

    private bool invertY = false;
    private bool invertAngle = false;
    private float positionMultiplier = 100f;

    [MenuItem("Tools/PixiJS Exporter")]
    public static void ShowWindow()
    {
        GetWindow<PixiExportTool>("PixiJS Exporter");
    }

    private void OnEnable()
    {
        shapesPath = EditorPrefs.GetString("PixiExport_ShapesPath", shapesPath);
        mapsPath = EditorPrefs.GetString("PixiExport_MapsPath", mapsPath);
        invertY = EditorPrefs.GetBool("PixiExport_InvertY", false);
        invertAngle = EditorPrefs.GetBool("PixiExport_InvertAngle", false);
        positionMultiplier = EditorPrefs.GetFloat("PixiExport_PositionMultiplier", 100f);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString("PixiExport_ShapesPath", shapesPath);
        EditorPrefs.SetString("PixiExport_MapsPath", mapsPath);
        EditorPrefs.SetBool("PixiExport_InvertY", invertY);
        EditorPrefs.SetBool("PixiExport_InvertAngle", invertAngle);
        EditorPrefs.SetFloat("PixiExport_PositionMultiplier", positionMultiplier);
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Settings", EditorStyles.boldLabel);
        
        shapesPath = EditorGUILayout.TextField("Shapes JSON Path", shapesPath);
        if (GUILayout.Button("Browse Shapes"))
        {
            string path = EditorUtility.SaveFilePanel("Save Shapes JSON", "", "shapes", "json");
            if (!string.IsNullOrEmpty(path)) shapesPath = path;
        }

        mapsPath = EditorGUILayout.TextField("Map Templates JSON Path", mapsPath);
        if (GUILayout.Button("Browse Map Templates"))
        {
            string path = EditorUtility.SaveFilePanel("Save Map Templates JSON", "", "mapTemplates", "json");
            if (!string.IsNullOrEmpty(path)) mapsPath = path;
        }

        GUILayout.Space(10);
        positionMultiplier = EditorGUILayout.FloatField("Position Multiplier (PPU)", positionMultiplier);
        invertY = EditorGUILayout.Toggle("Invert Y Axis (Pixi is +Y down)", invertY);
        invertAngle = EditorGUILayout.Toggle("Invert Angle", invertAngle);

        GUILayout.Space(20);

        if (GUILayout.Button("Export Shapes.json", GUILayout.Height(30)))
        {
            ExportShapes();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Export MapTemplates.json", GUILayout.Height(30)))
        {
            ExportMapTemplates();
        }
    }

    private void ExportShapes()
    {
        ShapeTemplate[] shapeTemplates = FindObjectsOfType<ShapeTemplate>();
        if (shapeTemplates.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No ShapeTemplate found in the scene!", "OK");
            return;
        }

        List<ShapeObject> shapeObjects = new List<ShapeObject>();

        foreach (var st in shapeTemplates)
        {
            if (st.transform.parent != null && st.transform.parent.GetComponent<MapTemplate>() != null)
            {
                // Wait, if it's placed inside a MapTemplate, we shouldn't export it as a base shape?
                // Mmm, better to only export root shapes or specific ones. 
                // Let's just grab those that are active and we can assume user separates map and shape scenes.
            }

            List<SlotData> slots = new List<SlotData>();
            CardGizmo[] cards = st.GetComponentsInChildren<CardGizmo>();
            foreach (var card in cards)
            {
                // Local position relative to the shape template
                Vector3 localPos = st.transform.InverseTransformPoint(card.transform.position);
                float angle = card.transform.localEulerAngles.z;
                if (angle > 180) angle -= 360f; // normalize to -180 to 180

                slots.Add(new SlotData()
                {
                    x = Mathf.Round(localPos.x * positionMultiplier * 100f) / 100f,
                    y = Mathf.Round((invertY ? -localPos.y : localPos.y) * positionMultiplier * 100f) / 100f,
                    angle = Mathf.Round((invertAngle ? -angle : angle) * 100f) / 100f,
                    layer = card.layer
                });
            }

            shapeObjects.Add(new ShapeObject()
            {
                id = st.shapeId,
                slots = slots.ToArray()
            });
        }

        ShapesRoot root = new ShapesRoot() { shapes = shapeObjects.ToArray() };
        string json = JsonUtility.ToJson(root, true);
        
        File.WriteAllText(shapesPath, json);
        Debug.Log($"Exported {shapeObjects.Count} shapes to {shapesPath}");
        EditorUtility.DisplayDialog("Success", $"Exported {shapeObjects.Count} shapes!", "OK");
    }

    private void ExportMapTemplates()
    {
        MapTemplate[] mapTemplates = FindObjectsOfType<MapTemplate>();
        if (mapTemplates.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No MapTemplate found in the scene!", "OK");
            return;
        }

        List<MapObject> mapObjects = new List<MapObject>();

        foreach (var mt in mapTemplates)
        {
            List<MapSlotData> mapSlots = new List<MapSlotData>();
            ShapeTemplate[] shapes = mt.GetComponentsInChildren<ShapeTemplate>();
            
            int shapeCounter = 1;

            foreach (var shape in shapes)
            {
                Vector3 localPos = mt.transform.InverseTransformPoint(shape.transform.position);
                float angle = shape.transform.localEulerAngles.z;
                if (angle > 180) angle -= 360f;

                mapSlots.Add(new MapSlotData()
                {
                    id = "board-shape-" + shapeCounter,
                    shapeId = shape.shapeId,
                    baseLayer = shape.baseLayer,
                    x = Mathf.Round(localPos.x * positionMultiplier * 100f) / 100f,
                    y = Mathf.Round((invertY ? -localPos.y : localPos.y) * positionMultiplier * 100f) / 100f,
                    angle = Mathf.Round((invertAngle ? -angle : angle) * 100f) / 100f
                });
                shapeCounter++;
            }

            mapObjects.Add(new MapObject()
            {
                id = mt.templateId,
                map = mapSlots.ToArray()
            });
        }

        string json = ToJsonArray(mapObjects.ToArray());
        
        File.WriteAllText(mapsPath, json);
        Debug.Log($"Exported {mapObjects.Count} map templates to {mapsPath}");
        EditorUtility.DisplayDialog("Success", $"Exported {mapObjects.Count} map templates!", "OK");
    }

    private string ToJsonArray<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T> { Items = array };
        string json = JsonUtility.ToJson(wrapper, true);
        int start = json.IndexOf("[");
        int end = json.LastIndexOf("]");
        if (start != -1 && end != -1)
        {
            return json.Substring(start, end - start + 1);
        }
        return "[]";
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

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
}

