using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CustomEditor(typeof(MapTool))]
public class MapToolEditor : Editor
{
    private PixiExportTool.MapObject[] loadedMaps;
    private PixiExportTool.ShapesRoot loadedShapes;

    private Vector2 scrollPos;
    private string newMapId = "new_map_id";

    [System.Serializable]
    private class ArrayWrapper<T>
    {
        public T[] Items;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        MapTool tool = (MapTool)target;
        MapTemplate mapTpl = tool.GetComponent<MapTemplate>();

        GUILayout.Space(15);
        if (GUILayout.Button("Load JSONs", GUILayout.Height(30)))
        {
            LoadJSONs(tool);
        }

        if (loadedMaps != null && loadedMaps.Length > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Loaded {loadedMaps.Length} Map Templates:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
            foreach (var map in loadedMaps)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(map.id, GUILayout.Width(150));
                
                if (GUILayout.Button("Generate In Scene", GUILayout.Width(130)))
                {
                    GenerateMapInScene(tool, mapTpl, map);
                }
                
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Delete Map", $"Are you sure you want to delete map '{map.id}'?", "Yes", "No"))
                    {
                        var list = loadedMaps.ToList();
                        list.Remove(map);
                        loadedMaps = list.ToArray();
                        SaveJSONs(tool);
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);
            GUILayout.Label("Current Scene Map Actions:", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            newMapId = EditorGUILayout.TextField("", newMapId, GUILayout.Width(150));
            if (GUILayout.Button("Create New Empty Map"))
            {
                CreateNewMap(mapTpl);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Save Overwrite ({mapTpl.templateId}) To JSON", GUILayout.Height(40)))
            {
                SaveMapToJSON(tool, mapTpl);
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void LoadJSONs(MapTool tool)
    {
        if (string.IsNullOrEmpty(tool.mapsJsonPath) || !File.Exists(tool.mapsJsonPath))
        {
            EditorUtility.DisplayDialog("Error", $"Maps JSON not found at: {tool.mapsJsonPath}", "OK");
            return;
        }
        
        if (string.IsNullOrEmpty(tool.shapesJsonPath) || !File.Exists(tool.shapesJsonPath))
        {
            EditorUtility.DisplayDialog("Error", $"Shapes JSON not found at: {tool.shapesJsonPath}", "OK");
            return;
        }

        // Load Maps
        string mapsJson = File.ReadAllText(tool.mapsJsonPath);
        string wrappedMapsJson = "{\"Items\":" + mapsJson + "}";
        ArrayWrapper<PixiExportTool.MapObject> mapWrapper = JsonUtility.FromJson<ArrayWrapper<PixiExportTool.MapObject>>(wrappedMapsJson);
        loadedMaps = mapWrapper != null ? mapWrapper.Items : new PixiExportTool.MapObject[0];

        // Load Shapes
        string shapesJson = File.ReadAllText(tool.shapesJsonPath);
        loadedShapes = JsonUtility.FromJson<PixiExportTool.ShapesRoot>(shapesJson);

        Debug.Log($"Loaded {loadedMaps.Length} maps and {(loadedShapes?.shapes?.Length ?? 0)} shapes.");
    }

    private void CreateNewMap(MapTemplate curTemplate)
    {
        curTemplate.templateId = newMapId;
        ClearChildren(curTemplate.transform);
        SceneView.RepaintAll();
        Debug.Log($"Created empty map '{newMapId}'. You can now place ShapeTemplates inside it.");
    }

    private void GenerateMapInScene(MapTool tool, MapTemplate curTemplate, PixiExportTool.MapObject mapData)
    {
        if (tool.cardPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Card Prefab in the MapTool.", "OK");
            return;
        }

        if (loadedShapes == null || loadedShapes.shapes == null)
        {
            EditorUtility.DisplayDialog("Error", "No shapes loaded from JSON! Please ensure shapes.json has data.", "OK");
            return;
        }

        curTemplate.templateId = mapData.id;
        ClearChildren(curTemplate.transform);

        if (mapData.map == null) return;

        foreach (var mapSlot in mapData.map)
        {
            PixiExportTool.ShapeObject shapeData = loadedShapes.shapes.FirstOrDefault(s => s.id == mapSlot.shapeId);
            if (shapeData == null)
            {
                Debug.LogWarning($"Shape '{mapSlot.shapeId}' not found in shapes.json! Skipping.");
                continue;
            }

            GameObject shapeGo = new GameObject(mapSlot.id);
            shapeGo.transform.SetParent(curTemplate.transform);

            float stX = mapSlot.x / tool.positionMultiplier;
            float stY = (tool.invertY ? -mapSlot.y : mapSlot.y) / tool.positionMultiplier;
            float stAngle = tool.invertAngle ? -mapSlot.angle : mapSlot.angle;

            shapeGo.transform.localPosition = new Vector3(stX, stY, 0);
            shapeGo.transform.localEulerAngles = new Vector3(0, 0, stAngle);

            ShapeTemplate shapeTemplate = shapeGo.AddComponent<ShapeTemplate>();
            shapeTemplate.shapeId = mapSlot.shapeId;
            shapeTemplate.baseLayer = mapSlot.baseLayer;

            if (shapeData.slots != null)
            {
                foreach (var slot in shapeData.slots)
                {
                    GameObject card = (GameObject)PrefabUtility.InstantiatePrefab(tool.cardPrefab);
                    card.transform.SetParent(shapeGo.transform);

                    float cx = slot.x / tool.positionMultiplier;
                    float cy = (tool.invertY ? -slot.y : slot.y) / tool.positionMultiplier;
                    float cangle = tool.invertAngle ? -slot.angle : slot.angle;

                    card.transform.localPosition = new Vector3(cx, cy, 0);
                    card.transform.localEulerAngles = new Vector3(0, 0, cangle);

                    CardGizmo gizmo = card.GetComponent<CardGizmo>();
                    if (gizmo != null) gizmo.layer = slot.layer;
                }
            }
        }

        Selection.activeGameObject = curTemplate.gameObject;
        SceneView.FrameLastActiveSceneView();
        Debug.Log($"Generated map '{mapData.id}' in scene.");
    }

    private void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(t.GetChild(i).gameObject);
        }
    }

    private void SaveMapToJSON(MapTool tool, MapTemplate mt)
    {
        if (loadedMaps == null)
        {
            EditorUtility.DisplayDialog("Error", "Maps not loaded! Please load JSONs first.", "OK");
            return;
        }

        PixiExportTool.MapObject existingMap = loadedMaps.FirstOrDefault(m => m.id == mt.templateId);
        if (existingMap == null)
        {
            if (EditorUtility.DisplayDialog("New Map", $"Map ID '{mt.templateId}' not found in JSON. Add as new?", "Yes", "No"))
            {
                var list = loadedMaps.ToList();
                existingMap = new PixiExportTool.MapObject { id = mt.templateId };
                list.Add(existingMap);
                loadedMaps = list.ToArray();
            }
            else return;
        }

        List<PixiExportTool.MapSlotData> mapSlots = new List<PixiExportTool.MapSlotData>();
        ShapeTemplate[] shapes = mt.GetComponentsInChildren<ShapeTemplate>();
        
        int shapeCounter = 1;
        foreach (var shape in shapes)
        {
            Vector3 localPos = mt.transform.InverseTransformPoint(shape.transform.position);
            float angle = shape.transform.localEulerAngles.z;
            if (angle > 180) angle -= 360f;

            mapSlots.Add(new PixiExportTool.MapSlotData()
            {
                id = $"board-shape-{shapeCounter}",
                shapeId = shape.shapeId,
                baseLayer = shape.baseLayer,
                x = Mathf.Round(localPos.x * tool.positionMultiplier * 100f) / 100f,
                y = Mathf.Round((tool.invertY ? -localPos.y : localPos.y) * tool.positionMultiplier * 100f) / 100f,
                angle = Mathf.Round((tool.invertAngle ? -angle : angle) * 100f) / 100f
            });
            shapeCounter++;
        }

        existingMap.map = mapSlots.ToArray();

        SaveJSONs(tool);
        
        Debug.Log($"Map '{mt.templateId}' overridden & saved successfully!");
        EditorUtility.DisplayDialog("Success", $"Map '{mt.templateId}' saved successfully to JSON!", "OK");
    }

    private void SaveJSONs(MapTool tool)
    {
        string jsonArray = ToJsonArray(loadedMaps);
        File.WriteAllText(tool.mapsJsonPath, jsonArray);
    }

    private string ToJsonArray<T>(T[] array)
    {
        ArrayWrapper<T> wrapper = new ArrayWrapper<T> { Items = array };
        string json = JsonUtility.ToJson(wrapper, true);
        
        int start = json.IndexOf("[");
        int end = json.LastIndexOf("]");
        if (start != -1 && end != -1)
        {
            return json.Substring(start, end - start + 1);
        }
        return "[]";
    }
}
