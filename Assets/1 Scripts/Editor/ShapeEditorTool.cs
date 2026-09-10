using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ShapeEditorTool : EditorWindow
{
    private string shapesPath = "D:/PurusGame/solitaire-classic/assets/preload/jsons/shapes.json";
    private GameObject cardPrefab;
    private float positionMultiplier = 100f;
    private bool invertY = false;
    private bool invertAngle = false;

    private PixiExportTool.ShapesRoot loadedData;
    private Vector2 scrollPos;

    [MenuItem("Tools/Shape Editor")]
    public static void ShowWindow()
    {
        GetWindow<ShapeEditorTool>("Shape Editor");
    }

    private void OnEnable()
    {
        shapesPath = EditorPrefs.GetString("PixiExport_ShapesPath", shapesPath);
        invertY = EditorPrefs.GetBool("PixiExport_InvertY", false);
        invertAngle = EditorPrefs.GetBool("PixiExport_InvertAngle", false);
        positionMultiplier = EditorPrefs.GetFloat("PixiExport_PositionMultiplier", 100f);
        
        string prefabPath = EditorPrefs.GetString("ShapeEditor_CardPrefab", "");
        if (!string.IsNullOrEmpty(prefabPath))
        {
            cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
    }

    private void OnDisable()
    {
        EditorPrefs.SetString("PixiExport_ShapesPath", shapesPath);
        EditorPrefs.SetBool("PixiExport_InvertY", invertY);
        EditorPrefs.SetBool("PixiExport_InvertAngle", invertAngle);
        EditorPrefs.SetFloat("PixiExport_PositionMultiplier", positionMultiplier);
        
        if (cardPrefab != null)
        {
            string path = AssetDatabase.GetAssetPath(cardPrefab);
            EditorPrefs.SetString("ShapeEditor_CardPrefab", path);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Shape Editor Settings", EditorStyles.boldLabel);
        
        shapesPath = EditorGUILayout.TextField("Shapes JSON Path", shapesPath);
        if (GUILayout.Button("Browse JSON"))
        {
            string p = EditorUtility.OpenFilePanel("Select shapes.json", "", "json");
            if (!string.IsNullOrEmpty(p)) shapesPath = p;
        }

        cardPrefab = (GameObject)EditorGUILayout.ObjectField("Card Prefab", cardPrefab, typeof(GameObject), false);

        positionMultiplier = EditorGUILayout.FloatField("Position Multiplier (PPU)", positionMultiplier);
        invertY = EditorGUILayout.Toggle("Invert Y Axis (Pixi is +Y down)", invertY);
        invertAngle = EditorGUILayout.Toggle("Invert Angle", invertAngle);

        GUILayout.Space(10);
        if (GUILayout.Button("Load Shapes JSON", GUILayout.Height(30)))
        {
            LoadJSON();
        }

        if (loadedData != null && loadedData.shapes != null)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Loaded {loadedData.shapes.Length} shapes", EditorStyles.boldLabel);

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var shape in loadedData.shapes)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(shape.id, GUILayout.Width(150));
                GUILayout.Label($"{shape.slots.Length} slots", GUILayout.Width(60));
                
                if (GUILayout.Button("Load to Scene"))
                {
                    GenerateShapeInScene(shape);
                }
                if (GUILayout.Button("Delete"))
                {
                    if (EditorUtility.DisplayDialog("Delete Shape", $"Delete {shape.id}?", "Yes", "No"))
                    {
                        var list = loadedData.shapes.ToList();
                        list.Remove(shape);
                        loadedData.shapes = list.ToArray();
                        SaveJSON();
                        GUIUtility.ExitGUI();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Blank Shape", GUILayout.Height(30)))
            {
                CreateBlankShape();
            }
            if (GUILayout.Button("Save Scene Shape to JSON", GUILayout.Height(30)))
            {
                SaveActiveShapeToJSON();
            }
            GUILayout.EndHorizontal();
        }
    }

    private void LoadJSON()
    {
        if (!File.Exists(shapesPath))
        {
            EditorUtility.DisplayDialog("Error", "File not found: " + shapesPath, "OK");
            return;
        }
        string json = File.ReadAllText(shapesPath);
        loadedData = JsonUtility.FromJson<PixiExportTool.ShapesRoot>(json);
        if (loadedData == null || loadedData.shapes == null)
        {
            loadedData = new PixiExportTool.ShapesRoot { shapes = new PixiExportTool.ShapeObject[0] };
        }
        Debug.Log("Loaded shapes from JSON.");
    }

    private void SaveJSON()
    {
        if (loadedData == null) return;
        string json = JsonUtility.ToJson(loadedData, true);
        File.WriteAllText(shapesPath, json);
        Debug.Log("Saved shapes to JSON.");
    }

    private void CreateBlankShape()
    {
        GameObject go = new GameObject("new_shape");
        var template = go.AddComponent<ShapeTemplate>();
        template.shapeId = "new_shape";
        Selection.activeGameObject = go;
    }

    private void GenerateShapeInScene(PixiExportTool.ShapeObject shapeData)
    {
        if (cardPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Card Prefab first.", "OK");
            return;
        }

        // Try to find if there is already one with this name
        GameObject existing = GameObject.Find(shapeData.id);
        if (existing != null)
        {
            if (EditorUtility.DisplayDialog("Warning", $"Shape {shapeData.id} already exists in scene. Replace?", "Yes", "No"))
            {
                DestroyImmediate(existing);
            }
            else return;
        }

        GameObject go = new GameObject(shapeData.id);
        var template = go.AddComponent<ShapeTemplate>();
        template.shapeId = shapeData.id;

        foreach (var slot in shapeData.slots)
        {
            GameObject card = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
            card.transform.SetParent(go.transform);
            
            float x = slot.x / positionMultiplier;
            float y = (invertY ? -slot.y : slot.y) / positionMultiplier;
            float angle = invertAngle ? -slot.angle : slot.angle;
            
            card.transform.localPosition = new Vector3(x, y, 0);
            card.transform.localEulerAngles = new Vector3(0, 0, angle);

            CardGizmo gizmo = card.GetComponent<CardGizmo>();
            if (gizmo != null) gizmo.layer = slot.layer;
        }

        Selection.activeGameObject = go;
        // Frame the object
        SceneView.FrameLastActiveSceneView();
    }

    private void SaveActiveShapeToJSON()
    {
        ShapeTemplate[] templates = FindObjectsOfType<ShapeTemplate>();
        if (templates.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No ShapeTemplate found in scene!", "OK");
            return;
        }

        // Update JSON data for each shape in scene
        bool changed = false;
        var shapeList = (loadedData != null && loadedData.shapes != null) ? loadedData.shapes.ToList() : new List<PixiExportTool.ShapeObject>();

        foreach (var st in templates)
        {
            if (st.transform.parent != null && st.transform.parent.GetComponent<MapTemplate>() != null) continue;
            List<PixiExportTool.SlotData> slots = new List<PixiExportTool.SlotData>();
            CardGizmo[] cards = st.GetComponentsInChildren<CardGizmo>();
            foreach (var card in cards)
            {
                Vector3 localPos = st.transform.InverseTransformPoint(card.transform.position);
                float angle = card.transform.localEulerAngles.z;
                if (angle > 180) angle -= 360f;

                slots.Add(new PixiExportTool.SlotData()
                {
                    x = Mathf.Round(localPos.x * positionMultiplier * 100f) / 100f,
                    y = Mathf.Round((invertY ? -localPos.y : localPos.y) * positionMultiplier * 100f) / 100f,
                    angle = Mathf.Round((invertAngle ? -angle : angle) * 100f) / 100f,
                    layer = card.layer
                });
            }

            PixiExportTool.ShapeObject existingShape = shapeList.FirstOrDefault(x => x.id == st.shapeId);
            if (existingShape != null)
            {
                existingShape.slots = slots.ToArray();
                changed = true;
                Debug.Log($"Updated existing shape: {st.shapeId}");
            }
            else
            {
                shapeList.Add(new PixiExportTool.ShapeObject()
                {
                    id = st.shapeId,
                    slots = slots.ToArray()
                });
                changed = true;
                Debug.Log($"Added new shape: {st.shapeId}");
            }
        }

        if (changed)
        {
            if (loadedData == null) loadedData = new PixiExportTool.ShapesRoot();
            loadedData.shapes = shapeList.ToArray();
            SaveJSON();
            EditorUtility.DisplayDialog("Success", "Saved shapes from scene to JSON.", "OK");
        }
    }
}


