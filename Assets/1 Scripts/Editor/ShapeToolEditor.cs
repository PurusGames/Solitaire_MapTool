using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CustomEditor(typeof(ShapeTool))]
public class ShapeToolEditor : Editor
{

    private Vector2 scrollPos;
    private string newShapeId = "new_shape_id";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            if (prop.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
                continue;
            }
            
            EditorGUILayout.PropertyField(prop, true);
            
            if (prop.name == "shapesJsonPath")
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Browse...", GUILayout.Width(100)))
                {
                    string dir = "";
                    if (!string.IsNullOrEmpty(prop.stringValue) && File.Exists(prop.stringValue))
                        dir = Path.GetDirectoryName(prop.stringValue);
                    
                    string path = EditorUtility.OpenFilePanel("Select Shapes JSON", dir, "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        prop.stringValue = path;
                        GUI.FocusControl(null);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
        }
        serializedObject.ApplyModifiedProperties();
        
        ShapeTool tool = (ShapeTool)target;
        ShapeTemplate shapeTpl = tool.GetComponent<ShapeTemplate>();

        GUILayout.Space(15);
        if (GUILayout.Button("Load JSON", GUILayout.Height(30)))
        {
            LoadJSON(tool);
        }

        if (tool.loadedShapes != null && tool.loadedShapes.shapes != null && tool.loadedShapes.shapes.Length > 0)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Loaded {tool.loadedShapes.shapes.Length} Shapes:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("helpbox");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
            foreach (var shape in tool.loadedShapes.shapes)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(shape.id, GUILayout.Width(150));
                GUILayout.Label($"{shape.slots?.Length ?? 0} slots", GUILayout.Width(60));
                
                if (GUILayout.Button("Generate In Scene", GUILayout.Width(130)))
                {
                    GenerateShapeInScene(tool, shapeTpl, shape);
                }
                
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Delete Shape", $"Are you sure you want to delete shape '{shape.id}'?", "Yes", "No"))
                    {
                        var list = tool.loadedShapes.shapes.ToList();
                        list.Remove(shape);
                        tool.loadedShapes.shapes = list.ToArray();
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
            EditorGUILayout.LabelField("Current Scene Shape Actions:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("helpbox");
            GUILayout.BeginHorizontal();
            newShapeId = EditorGUILayout.TextField("", newShapeId, GUILayout.Width(150));
            if (GUILayout.Button("Create New Empty Shape"))
            {
                CreateNewShape(shapeTpl);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
            if (GUILayout.Button("Add New Card to Shape", GUILayout.Height(30)))
            {
                if (tool.cardPrefab != null)
                {
                    GameObject newCard = (GameObject)PrefabUtility.InstantiatePrefab(tool.cardPrefab);
                    newCard.transform.SetParent(shapeTpl.transform, false);
                    newCard.name = "card_" + (shapeTpl.transform.childCount);
                    Selection.activeGameObject = newCard;
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Missing Card Prefab in ShapeTool!", "OK");
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
            if (GUILayout.Button($"Save Overwrite ({shapeTpl.shapeId}) To JSON", GUILayout.Height(40)))
            {
                SaveShapeToJSON(tool, shapeTpl);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
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
        tool.loadedShapes = JsonUtility.FromJson<ShapesRoot>(shapesJson);

        if (tool.loadedShapes == null) 
        {
            tool.loadedShapes = new ShapesRoot { shapes = new ShapeObject[0] };
        }
        else if (tool.loadedShapes.shapes == null)
        {
            tool.loadedShapes.shapes = new ShapeObject[0];
        }

        EditorUtility.SetDirty(tool);
        Debug.Log($"Loaded {tool.loadedShapes.shapes.Length} shapes.");
    }

    private void CreateNewShape(ShapeTemplate shapeTpl)
    {
        shapeTpl.shapeId = newShapeId;
        ClearChildren(shapeTpl.transform);
        SceneView.RepaintAll();
        Debug.Log($"Created empty shape '{newShapeId}'. You can now place Card prefab instances inside it.");
    }

    private void GenerateShapeInScene(ShapeTool tool, ShapeTemplate shapeTpl, ShapeObject shapeData)
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

            card.transform.localPosition = new Vector3(cx, cy, -slot.layer);
            card.transform.localEulerAngles = new Vector3(0, 0, cangle);

            CardGizmo gizmo = card.GetComponent<CardGizmo>();
            if (gizmo != null) gizmo.layer = slot.layer;
        }

        Selection.activeGameObject = shapeTpl.gameObject;
        // SceneView.FrameLastActiveSceneView();
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
        if (tool.loadedShapes == null || tool.loadedShapes.shapes == null)
        {
            EditorUtility.DisplayDialog("Error", "Shapes not loaded! Please load JSON first.", "OK");
            return;
        }

        ShapeObject existingShape = tool.loadedShapes.shapes.FirstOrDefault(s => s.id == shapeTpl.shapeId);
        if (existingShape == null)
        {
            if (EditorUtility.DisplayDialog("New Shape", $"Shape ID '{shapeTpl.shapeId}' not found in JSON. Add as new?", "Yes", "No"))
            {
                var list = tool.loadedShapes.shapes.ToList();
                existingShape = new ShapeObject { id = shapeTpl.shapeId };
                list.Add(existingShape);
                tool.loadedShapes.shapes = list.ToArray();
            }
            else return;
        }

        List<SlotData> slots = new List<SlotData>();
        CardGizmo[] cards = shapeTpl.GetComponentsInChildren<CardGizmo>();
        
        foreach (var card in cards)
        {
            Vector3 localPos = shapeTpl.transform.InverseTransformPoint(card.transform.position);
            float angle = card.transform.localEulerAngles.z;
            if (angle > 180) angle -= 360f;

            slots.Add(new SlotData()
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
        string json = JsonUtility.ToJson(tool.loadedShapes, true);
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
        var loaded = JsonUtility.FromJson<ShapesRoot>(shapesJson);
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

            card.transform.localPosition = new Vector3(cx, cy, -slot.layer);
            card.transform.localEulerAngles = new Vector3(0, 0, cangle);

            CardGizmo gizmo = card.GetComponent<CardGizmo>();
            if (gizmo != null) gizmo.layer = slot.layer;
        }
    }
}
