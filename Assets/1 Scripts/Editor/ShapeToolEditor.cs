using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CustomEditor(typeof(ShapeTool))]
public class ShapeToolEditor : Editor
{
    private PixiExportTool.ShapesRoot loadedShapes;

    private Vector2 scrollPos;
    private string newShapeId = "new_shape_id";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        ShapeTool tool = (ShapeTool)target;
        ShapeTemplate shapeTpl = tool.GetComponent<ShapeTemplate>();

        GUILayout.Space(15);
        if (GUILayout.Button("Load JSON", GUILayout.Height(30)))
        {
            LoadJSON(tool);
        }

        if (loadedShapes != null && loadedShapes.shapes != null && loadedShapes.shapes.Length > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Loaded {loadedShapes.shapes.Length} Shapes:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
            foreach (var shape in loadedShapes.shapes)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(shape.id, GUILayout.Width(150));
                GUILayout.Label($"{shape.slots?.Length ?? 0} slots", GUILayout.Width(60));
                
                if (GUILayout.Button("Generate In Scene", GUILayout.Width(130)))
                {
                    GenerateShapeInScene(tool, shapeTpl, shape);
                }
                
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Delete Shape", $"Are you sure you want to delete shape '{shape.id}'?", "Yes", "No"))
                    {
                        var list = loadedShapes.shapes.ToList();
                        list.Remove(shape);
                        loadedShapes.shapes = list.ToArray();
                        SaveJSON(tool);
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);
            GUILayout.Label("Current Scene Shape Actions:", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            newShapeId = EditorGUILayout.TextField("", newShapeId, GUILayout.Width(150));
            if (GUILayout.Button("Create New Empty Shape"))
            {
                CreateNewShape(shapeTpl);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Save Overwrite ({shapeTpl.shapeId}) To JSON", GUILayout.Height(40)))
            {
                SaveShapeToJSON(tool, shapeTpl);
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void LoadJSON(ShapeTool tool)
    {
        if (string.IsNullOrEmpty(tool.shapesJsonPath) || !File.Exists(tool.shapesJsonPath))
        {
            EditorUtility.DisplayDialog("Error", $"Shapes JSON not found at: {tool.shapesJsonPath}", "OK");
            return;
        }

        string shapesJson = File.ReadAllText(tool.shapesJsonPath);
        loadedShapes = JsonUtility.FromJson<PixiExportTool.ShapesRoot>(shapesJson);

        if (loadedShapes == null) 
        {
            loadedShapes = new PixiExportTool.ShapesRoot { shapes = new PixiExportTool.ShapeObject[0] };
        }
        else if (loadedShapes.shapes == null)
        {
            loadedShapes.shapes = new PixiExportTool.ShapeObject[0];
        }

        Debug.Log($"Loaded {loadedShapes.shapes.Length} shapes.");
    }

    private void CreateNewShape(ShapeTemplate shapeTpl)
    {
        shapeTpl.shapeId = newShapeId;
        ClearChildren(shapeTpl.transform);
        SceneView.RepaintAll();
        Debug.Log($"Created empty shape '{newShapeId}'. You can now place Card prefab instances inside it.");
    }

    private void GenerateShapeInScene(ShapeTool tool, ShapeTemplate shapeTpl, PixiExportTool.ShapeObject shapeData)
    {
        if (tool.cardPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Card Prefab in the ShapeTool.", "OK");
            return;
        }

        shapeTpl.shapeId = shapeData.id;
        ClearChildren(shapeTpl.transform);

        if (shapeData.slots == null) return;

        foreach (var slot in shapeData.slots)
        {
            GameObject card = (GameObject)PrefabUtility.InstantiatePrefab(tool.cardPrefab);
            card.transform.SetParent(shapeTpl.transform);

            float cx = slot.x / tool.positionMultiplier;
            float cy = (tool.invertY ? -slot.y : slot.y) / tool.positionMultiplier;
            float cangle = tool.invertAngle ? -slot.angle : slot.angle;

            card.transform.localPosition = new Vector3(cx, cy, 0);
            card.transform.localEulerAngles = new Vector3(0, 0, cangle);

            CardGizmo gizmo = card.GetComponent<CardGizmo>();
            if (gizmo != null) gizmo.layer = slot.layer;
        }

        Selection.activeGameObject = shapeTpl.gameObject;
        SceneView.FrameLastActiveSceneView();
        Debug.Log($"Generated shape '{shapeData.id}' in scene.");
    }

    private static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(t.GetChild(i).gameObject);
        }
    }

    private void SaveShapeToJSON(ShapeTool tool, ShapeTemplate shapeTpl)
    {
        if (loadedShapes == null || loadedShapes.shapes == null)
        {
            EditorUtility.DisplayDialog("Error", "Shapes not loaded! Please load JSON first.", "OK");
            return;
        }

        PixiExportTool.ShapeObject existingShape = loadedShapes.shapes.FirstOrDefault(s => s.id == shapeTpl.shapeId);
        if (existingShape == null)
        {
            if (EditorUtility.DisplayDialog("New Shape", $"Shape ID '{shapeTpl.shapeId}' not found in JSON. Add as new?", "Yes", "No"))
            {
                var list = loadedShapes.shapes.ToList();
                existingShape = new PixiExportTool.ShapeObject { id = shapeTpl.shapeId };
                list.Add(existingShape);
                loadedShapes.shapes = list.ToArray();
            }
            else return;
        }

        List<PixiExportTool.SlotData> slots = new List<PixiExportTool.SlotData>();
        CardGizmo[] cards = shapeTpl.GetComponentsInChildren<CardGizmo>();
        
        foreach (var card in cards)
        {
            Vector3 localPos = shapeTpl.transform.InverseTransformPoint(card.transform.position);
            float angle = card.transform.localEulerAngles.z;
            if (angle > 180) angle -= 360f;

            slots.Add(new PixiExportTool.SlotData()
            {
                x = Mathf.Round(localPos.x * tool.positionMultiplier * 100f) / 100f,
                y = Mathf.Round((tool.invertY ? -localPos.y : localPos.y) * tool.positionMultiplier * 100f) / 100f,
                angle = Mathf.Round((tool.invertAngle ? -angle : angle) * 100f) / 100f,
                layer = card.layer
            });
        }

        existingShape.slots = slots.ToArray();

        SaveJSON(tool);
        
        Debug.Log($"Shape '{shapeTpl.shapeId}' overridden & saved successfully! ({slots.Count} cards)");
        EditorUtility.DisplayDialog("Success", $"Shape '{shapeTpl.shapeId}' saved successfully to JSON!", "OK");
    }

    private void SaveJSON(ShapeTool tool)
    {
        string json = JsonUtility.ToJson(loadedShapes, true);
        File.WriteAllText(tool.shapesJsonPath, json);
    }

    /// <summary>
    /// Utility method for MapTool to generate a shape by its ID.
    /// </summary>
    public static void GenerateShapeFromId(ShapeTool tool, ShapeTemplate shapeTpl, string targetShapeId)
    {
        if (string.IsNullOrEmpty(tool.shapesJsonPath) || !File.Exists(tool.shapesJsonPath)) 
        {
            Debug.LogError($"Shapes JSON not found: {tool.shapesJsonPath}");
            return;
        }
        
        string shapesJson = File.ReadAllText(tool.shapesJsonPath);
        var loaded = JsonUtility.FromJson<PixiExportTool.ShapesRoot>(shapesJson);
        if (loaded == null || loaded.shapes == null) return;

        var shapeData = loaded.shapes.FirstOrDefault(s => s.id == targetShapeId);
        if (shapeData == null) 
        {
            Debug.LogWarning($"Shape '{targetShapeId}' not found in shapes.json.");
            return;
        }
        
        if (tool.cardPrefab == null) 
        {
            Debug.LogError("No Card Prefab assigned to the ShapeTool.");
            return;
        }
        
        ClearChildren(shapeTpl.transform);
        shapeTpl.shapeId = targetShapeId;
        
        if (shapeData.slots == null) return;

        foreach (var slot in shapeData.slots)
        {
            GameObject card = (GameObject)PrefabUtility.InstantiatePrefab(tool.cardPrefab);
            card.transform.SetParent(shapeTpl.transform);

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
