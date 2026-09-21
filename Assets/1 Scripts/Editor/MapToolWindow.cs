using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class MapToolWindow : EditorWindow
{
    private MapTool activeTool;
    private Vector2 mainScrollPos;
    private Vector2 mapsScrollPos;
    private string searchFilter = "";

    private bool showMapsFoldout = true;
    private bool showActionsFoldout = true;
    private bool showSettingsFoldout = true;

    [MenuItem("Tools/Map Tool Window")]
    public static void ShowWindow()
    {
        MapToolWindow window = GetWindow<MapToolWindow>("Map Tool");
        window.minSize = new Vector2(340, 500);
        window.Show();
    }

    private void OnEnable()
    {
        FindToolInScene();
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        Repaint();
    }

    private void FindToolInScene()
    {
        if (activeTool == null)
        {
            activeTool = FindObjectOfType<MapTool>();
        }
    }

    private void OnGUI()
    {
        // Top Toolbar / Status
        DrawTopToolbar();

        if (activeTool == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No 'MapTool' found in the current active scene.\nPlease open a map scene or assign the MapTool component above.", MessageType.Warning);
            if (GUILayout.Button("Retry Find in Scene", GUILayout.Height(30)))
            {
                FindToolInScene();
            }
            return;
        }

        MapTemplate mapTpl = activeTool.GetComponent<MapTemplate>();
        if (mapTpl == null)
        {
            mapTpl = activeTool.gameObject.AddComponent<MapTemplate>();
        }

        mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

        DrawContextualBanner(mapTpl);
        DrawMapTemplatesSection(mapTpl);
        DrawSceneActionsSection(mapTpl);

        // Settings Foldout
        DrawSettingsSection();

        EditorGUILayout.Space(20);
        EditorGUILayout.EndScrollView();
    }

    private void DrawTopToolbar()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        GUI.color = activeTool != null ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.7f, 0.7f);
        GUILayout.Label(activeTool != null ? "● Connected" : "○ Disconnected", EditorStyles.boldLabel, GUILayout.Width(95));
        GUI.color = Color.white;

        MapTool prev = activeTool;
        activeTool = (MapTool)EditorGUILayout.ObjectField(activeTool, typeof(MapTool), true);
        if (prev != activeTool && activeTool != null)
        {
            Repaint();
        }

        if (activeTool != null)
        {
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeGameObject = activeTool.gameObject;
            }
        }
        else
        {
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                FindToolInScene();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawContextualBanner(MapTemplate mapTpl)
    {
        GameObject selGo = Selection.activeGameObject;
        ShapeTemplate selShape = selGo != null ? selGo.GetComponentInParent<ShapeTemplate>() : null;
        CardGizmo selCard = selGo != null ? selGo.GetComponent<CardGizmo>() : null;

        if (selShape != null && selShape.transform.IsChildOf(activeTool.transform))
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();

            GUIStyle bold = new GUIStyle(EditorStyles.boldLabel);
            bold.normal.textColor = new Color(0.2f, 0.8f, 1f);
            GUILayout.Label($"Selected Shape: {selShape.name}", bold);
            GUILayout.Label($"ID: {selShape.shapeId}", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Base Layer: {selShape.baseLayer}", EditorStyles.boldLabel, GUILayout.Width(100));

            if (GUILayout.Button("-", GUILayout.Width(35), GUILayout.Height(22)))
            {
                Undo.RecordObject(selShape, "Change Base Layer");
                selShape.baseLayer--;
                EditorUtility.SetDirty(selShape);
            }
            if (GUILayout.Button("+", GUILayout.Width(35), GUILayout.Height(22)))
            {
                Undo.RecordObject(selShape, "Change Base Layer");
                selShape.baseLayer++;
                EditorUtility.SetDirty(selShape);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Select Root Map", GUILayout.Height(22)))
            {
                Selection.activeGameObject = activeTool.gameObject;
            }
            EditorGUILayout.EndHorizontal();

            if (selCard != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"Selected Card: {selCard.name} | Layer: {selCard.layer} | {selCard.suit} {selCard.rank}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        else
        {
            int shapeCount = mapTpl.transform.childCount;
            int cardCount = mapTpl.GetComponentsInChildren<CardGizmo>().Length;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Current Map: {mapTpl.templateId}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Shapes: {shapeCount} | Cards: {cardCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }

    private void DrawSceneActionsSection(MapTemplate mapTpl)
    {
        showActionsFoldout = EditorGUILayout.Foldout(showActionsFoldout, "Current Scene Map Actions", true, EditorStyles.foldoutHeader);
        if (!showActionsFoldout) return;

        EditorGUILayout.BeginVertical("helpbox");

        // Template ID input
        EditorGUI.BeginChangeCheck();
        string newId = EditorGUILayout.TextField("Map Template ID", mapTpl.templateId);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mapTpl, "Change Template ID");
            mapTpl.templateId = newId;
            EditorUtility.SetDirty(mapTpl);
        }

        GUILayout.Space(4);

        if (GUILayout.Button("+ Create New Empty Map", GUILayout.Height(28)))
        {
            MapToolEditor.CreateNewMap(activeTool, mapTpl);
        }

        GUILayout.Space(4);
        GUI.backgroundColor = new Color(0.7f, 0.88f, 1f);
        if (GUILayout.Button("+ Add New Shape to Map", GUILayout.Height(32)))
        {
            MapToolEditor.AddNewShape(activeTool, mapTpl);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(4);
        if (GUILayout.Button("Center Map to (0,0)", GUILayout.Height(26)))
        {
            MapToolEditor.CenterMap(mapTpl);
        }

        GUILayout.Space(8);
        GUI.backgroundColor = new Color(0.55f, 0.92f, 0.55f);
        string btnText = string.IsNullOrEmpty(mapTpl.templateId) ?
            "💾 Save To JSON (Auto-Increment ID)" :
            $"💾 Save Overwrite ({mapTpl.templateId}) To JSON";

        if (GUILayout.Button(btnText, GUILayout.Height(38)))
        {
            MapToolEditor.SaveMapToJSON(activeTool, mapTpl);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawMapTemplatesSection(MapTemplate mapTpl)
    {
        int count = activeTool.loadedMaps != null ? activeTool.loadedMaps.Length : 0;
        showMapsFoldout = EditorGUILayout.Foldout(showMapsFoldout, $"Map Templates JSON & Loaded Maps ({count})", true, EditorStyles.foldoutHeader);
        if (!showMapsFoldout) return;

        EditorGUILayout.BeginVertical("helpbox");

        // JSON Path with Browse
        EditorGUILayout.BeginHorizontal();
        activeTool.mapsJsonPath = EditorGUILayout.TextField("Maps JSON Path", activeTool.mapsJsonPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string dir = "";
            if (!string.IsNullOrEmpty(activeTool.mapsJsonPath) && File.Exists(activeTool.mapsJsonPath))
                dir = Path.GetDirectoryName(activeTool.mapsJsonPath);

            string path = EditorUtility.OpenFilePanel("Select Maps JSON", dir, "json");
            if (!string.IsNullOrEmpty(path))
            {
                Undo.RecordObject(activeTool, "Change Maps JSON Path");
                activeTool.mapsJsonPath = path;
                EditorUtility.SetDirty(activeTool);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Load JSON", GUILayout.Height(28)))
        {
            MapToolEditor.LoadJSONs(activeTool);
        }

        // Loaded Maps List
        if (activeTool.loadedMaps != null && activeTool.loadedMaps.Length > 0)
        {
            GUILayout.Space(6);

            // Filter search bar
            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField("Search Map", searchFilter);
            if (!string.IsNullOrEmpty(searchFilter))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    searchFilter = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUILayout.LabelField($"Loaded {activeTool.loadedMaps.Length} Map Templates:", EditorStyles.boldLabel);

            mapsScrollPos = EditorGUILayout.BeginScrollView(mapsScrollPos, GUILayout.Height(220));
            foreach (var map in activeTool.loadedMaps)
            {
                if (!string.IsNullOrEmpty(searchFilter) &&
                    (map.id == null || map.id.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) < 0))
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal("box");

                GUILayout.Label(map.id, GUILayout.Width(130));

                int shapesInMap = map.map != null ? map.map.Length : 0;
                GUILayout.Label($"{shapesInMap} shapes", EditorStyles.miniLabel, GUILayout.Width(65));

                if (GUILayout.Button("Generate", GUILayout.Width(75)))
                {
                    MapToolEditor.GenerateMapInScene(activeTool, mapTpl, map);
                }

                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Del", GUILayout.Width(35)))
                {
                    if (EditorUtility.DisplayDialog("Delete Map", $"Are you sure you want to delete map '{map.id}'?", "Yes", "No"))
                    {
                        var list = activeTool.loadedMaps.ToList();
                        list.Remove(map);
                        activeTool.loadedMaps = list.ToArray();
                        MapToolEditor.SaveJSONs(activeTool);
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawSettingsSection()
    {
        showSettingsFoldout = EditorGUILayout.Foldout(showSettingsFoldout, "Tool Settings & Configurations", true, EditorStyles.foldoutHeader);
        if (!showSettingsFoldout) return;

        EditorGUILayout.BeginVertical("helpbox");

        EditorGUI.BeginChangeCheck();

        activeTool.shapePrefab = (GameObject)EditorGUILayout.ObjectField("Shape Prefab", activeTool.shapePrefab, typeof(GameObject), false);
        activeTool.positionMultiplier = EditorGUILayout.FloatField("Position Multiplier", activeTool.positionMultiplier);
        activeTool.invertY = EditorGUILayout.Toggle("Invert Y", activeTool.invertY);
        activeTool.invertAngle = EditorGUILayout.Toggle("Invert Angle", activeTool.invertAngle);
        activeTool.autoCenterOnSave = EditorGUILayout.Toggle("Auto Center On Save", activeTool.autoCenterOnSave);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(activeTool, "Update Tool Settings");
            EditorUtility.SetDirty(activeTool);
        }

        EditorGUILayout.EndVertical();
    }
}
