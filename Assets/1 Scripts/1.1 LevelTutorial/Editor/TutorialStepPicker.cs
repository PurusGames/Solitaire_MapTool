using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class TutorialStepPicker
{
    public static bool isPicking = false;
    public static TutorialMapTool activeTool = null;

    static TutorialStepPicker()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    public static void StartPicking(TutorialMapTool tool)
    {
        if (tool == null)
        {
            tool = Object.FindObjectOfType<TutorialMapTool>();
        }

        if (tool == null)
        {
            EditorUtility.DisplayDialog("Error", "No TutorialMapTool found in scene.", "OK");
            return;
        }

        activeTool = tool;
        isPicking = true;

        EditorApplication.delayCall += () =>
        {
            SceneView.RepaintAll();
        };
    }

    public static void StopPicking()
    {
        isPicking = false;
        activeTool = null;

        EditorApplication.delayCall += () =>
        {
            SceneView.RepaintAll();
        };
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!isPicking || activeTool == null) return;

        Event e = Event.current;

        // Force passive control ID so Unity doesn't execute standard selection/manipulation
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        // Handle Escape to cancel
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            StopPicking();
            e.Use();
            return;
        }

        // Draw top banner & cursor in SceneView GUI
        Handles.BeginGUI();
        try
        {
            if (e.type == EventType.Repaint)
            {
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, sceneView.position.width, sceneView.position.height), MouseCursor.Link);
            }

            float bannerW = 440;
            float bannerH = 55;
            Rect rect = new Rect((sceneView.position.width - bannerW) / 2f, 15, bannerW, bannerH);
            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                normal = { textColor = new Color(0.3f, 1f, 0.5f) }
            };
            GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            GUI.Label(new Rect(rect.x, rect.y + 6, bannerW, 20), "🎯 PICK MODE: Click Card to add to tap_card", titleStyle);
            GUI.Label(new Rect(rect.x, rect.y + 27, bannerW, 20), "Click DrawPile to toggle tap_drawpile | Click outside or [ESC] to Cancel", subStyle);
        }
        finally
        {
            Handles.EndGUI();
        }

        // Hover highlight
        CardGizmo hoveredCard = FindCardUnderMouse(e.mousePosition);
        TutorialDrawPile hoveredPile = hoveredCard == null ? FindDrawPileUnderMouse(e.mousePosition) : null;

        if (e.type == EventType.Repaint)
        {
            if (hoveredCard != null)
            {
                SpriteRenderer sr = hoveredCard.GetComponent<SpriteRenderer>();
                Bounds b = sr != null ? sr.bounds : new Bounds(hoveredCard.transform.position, Vector3.one);

                bool isDrawChild = activeTool.drawPileObj != null && hoveredCard.transform.IsChildOf(activeTool.drawPileObj.transform);
                if (isDrawChild)
                {
                    Handles.color = new Color(0.3f, 0.8f, 1f, 0.9f);
                    Handles.DrawWireCube(b.center, b.size * 1.05f);
                    string dpStatus = activeTool.tapDrawPile ? "ON" : "OFF";
                    Handles.Label(b.center + Vector3.up * (b.extents.y + 0.35f), $"Click -> Toggle tap_drawpile (Current: {dpStatus})", EditorStyles.whiteBoldLabel);
                }
                else
                {
                    Handles.color = new Color(0.2f, 1f, 0.4f, 0.9f);
                    Handles.DrawWireCube(b.center, b.size * 1.05f);
                    string labelText = $"Click -> Add Step #{activeTool.GetNextStepNumber()}: {hoveredCard.name}";
                    Handles.Label(b.center + Vector3.up * (b.extents.y + 0.35f), labelText, EditorStyles.whiteBoldLabel);
                }
            }
            else if (hoveredPile != null)
            {
                Handles.color = new Color(0.3f, 0.8f, 1f, 0.9f);
                Handles.DrawWireCube(hoveredPile.transform.position, Vector3.one * 1.5f);
                string dpStatus = activeTool.tapDrawPile ? "ON" : "OFF";
                Handles.Label(hoveredPile.transform.position + Vector3.up * 1f, $"Click -> Toggle tap_drawpile (Current: {dpStatus})", EditorStyles.whiteBoldLabel);
            }
        }

        // Only repaint on mouse move to keep hover visuals responsive without recursive Repaint inside Repaint
        if (e.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        // On Mouse Click
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            CardGizmo pickedCard = FindCardUnderMouse(e.mousePosition);
            TutorialDrawPile pickedPile = pickedCard == null ? FindDrawPileUnderMouse(e.mousePosition) : null;

            if (pickedCard != null)
            {
                if (activeTool.drawPileObj != null && pickedCard.transform.IsChildOf(activeTool.drawPileObj.transform))
                {
                    activeTool.ToggleTapDrawPile();
                    Debug.Log($"Toggled Tutorial tap_drawpile to: {activeTool.tapDrawPile}");
                }
                else if (activeTool.checkCardObj != null && pickedCard.transform.IsChildOf(activeTool.checkCardObj.transform))
                {
                    Debug.LogWarning("CheckCard is the foundation card and cannot be added as a tap step.");
                }
                else
                {
                    activeTool.AddCardStep(pickedCard);
                    Selection.activeGameObject = pickedCard.gameObject;
                    Debug.Log($"Added Tutorial tap_card step #{pickedCard.tutorialStep} for card: {pickedCard.name}");
                }
                StopPicking();
                e.Use();
            }
            else if (pickedPile != null)
            {
                activeTool.ToggleTapDrawPile();
                Selection.activeGameObject = pickedPile.gameObject;
                Debug.Log($"Toggled Tutorial tap_drawpile to: {activeTool.tapDrawPile}");
                StopPicking();
                e.Use();
            }
            else
            {
                // Clicked outside / empty space: Cancel picking
                Debug.Log("Card picking cancelled (clicked outside).");
                StopPicking();
                e.Use();
            }
        }
    }

    public static CardGizmo FindCardUnderMouse(Vector2 mousePos)
    {
        // 1. Try HandleUtility.PickGameObject
        GameObject picked = HandleUtility.PickGameObject(mousePos, false);
        if (picked != null)
        {
            CardGizmo g = picked.GetComponentInParent<CardGizmo>();
            if (g != null) return g;
        }

        // 2. Fallback: Raycast to 2D Sprite bounds
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        Vector2 worldPos = ray.origin;

        CardGizmo bestCard = null;
        int highestOrder = int.MinValue;

        CardGizmo[] allGizmos = Object.FindObjectsOfType<CardGizmo>();
        foreach (var gizmo in allGizmos)
        {
            SpriteRenderer sr = gizmo.GetComponent<SpriteRenderer>();
            if (sr != null && sr.bounds.Contains(worldPos))
            {
                int order = gizmo.layer * 1000;
                SortingGroup sg = gizmo.GetComponent<SortingGroup>();
                if (sg != null) order = sg.sortingOrder;

                if (order > highestOrder)
                {
                    highestOrder = order;
                    bestCard = gizmo;
                }
            }
        }

        return bestCard;
    }

    public static TutorialDrawPile FindDrawPileUnderMouse(Vector2 mousePos)
    {
        GameObject picked = HandleUtility.PickGameObject(mousePos, false);
        if (picked != null)
        {
            TutorialDrawPile pile = picked.GetComponentInParent<TutorialDrawPile>();
            if (pile != null) return pile;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        Vector2 worldPos = ray.origin;

        TutorialDrawPile[] allPiles = Object.FindObjectsOfType<TutorialDrawPile>();
        foreach (var p in allPiles)
        {
            foreach (var sr in p.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.bounds.Contains(worldPos)) return p;
            }
        }

        return null;
    }
}
