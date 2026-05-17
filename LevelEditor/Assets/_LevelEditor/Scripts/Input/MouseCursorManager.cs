using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Selectable = UnityEngine.UI.Selectable;

/// <summary>
/// Central place for changing the OS cursor when hovering clickable editor elements.
/// Add this once in the scene and assign cursor textures in the inspector.
/// </summary>
public sealed class MouseCursorManager : MonoBehaviour
{
    [Header("Cursor Textures")]
    [SerializeField] Texture2D defaultCursor;
    [SerializeField] Texture2D clickableCursor;
    [SerializeField] Vector2 defaultHotspot = Vector2.zero;
    [SerializeField] Vector2 clickableHotspot = Vector2.zero;
    [SerializeField] CursorMode cursorMode = CursorMode.ForceSoftware;

    [Header("World Hover")]
    [SerializeField] Camera targetCamera;
    [SerializeField] LayerMask clickableWorldLayers;

    [Header("UI Hover")]
    [SerializeField] bool includeUGuiClickables = true;
    [SerializeField] bool includeUIToolkitElements = true;
    [SerializeField] UIDocument[] uiToolkitDocuments;

    bool isShowingClickableCursor;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (clickableWorldLayers.value == 0)
            clickableWorldLayers = LayerMask.GetMask("Selectable", "GizmoHandle");
    }

    void OnEnable()
    {
        ApplyDefaultCursor();
    }

    void OnDisable()
    {
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, cursorMode);
        isShowingClickableCursor = false;
    }

    void Update()
    {
        bool shouldShowClickable = IsPointerOverClickable();
        if (shouldShowClickable == isShowingClickableCursor)
            return;

        if (shouldShowClickable)
            ApplyClickableCursor();
        else
            ApplyDefaultCursor();
    }

    bool IsPointerOverClickable()
    {
        if (Mouse.current == null)
            return false;

        if (includeUGuiClickables && IsPointerOverUGuiClickable())
            return true;

        if (includeUIToolkitElements && IsPointerOverUIToolkitElement())
            return true;

        if (targetCamera == null)
            targetCamera = Camera.main;

        return clickableWorldLayers.value != 0
            && RaycastHelper.IsClickingOnLayer(targetCamera, clickableWorldLayers);
    }

    bool IsPointerOverUGuiClickable()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            GameObject hit = result.gameObject;
            if (hit == null)
                continue;

            if (hit.GetComponentInParent<Selectable>() != null)
                return true;

            if (hit.GetComponentInParent<IPointerClickHandler>() != null)
                return true;
        }

        return false;
    }

    bool IsPointerOverUIToolkitElement()
    {
        UIDocument[] documents = uiToolkitDocuments;
        if (documents == null || documents.Length == 0)
            documents = FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        foreach (UIDocument document in documents)
        {
            if (document == null)
                continue;

            VisualElement root = document.rootVisualElement;
            if (root == null || root.panel == null)
                continue;

            Vector2 panelPosition = ScreenToPanelPosition(screenPosition, root);
            VisualElement picked = root.panel.Pick(panelPosition);
            if (picked == null || picked == root)
                continue;

            if (IsToolkitElementClickable(picked))
                return true;
        }

        return false;
    }

    static bool IsToolkitElementClickable(VisualElement element)
    {
        for (VisualElement current = element; current != null; current = current.parent)
        {
            if (current is UnityEngine.UIElements.Button || current is UnityEngine.UIElements.Toggle || current is Foldout || current is TextField)
                return true;

            if (current.focusable && current.pickingMode == PickingMode.Position)
                return true;
        }

        return false;
    }

    static Vector2 ScreenToPanelPosition(Vector2 screenPosition, VisualElement root)
    {
        Rect panelBounds = root.worldBound;
        float width = Mathf.Max(1f, panelBounds.width);
        float height = Mathf.Max(1f, panelBounds.height);

        return new Vector2(
            panelBounds.xMin + (screenPosition.x / Mathf.Max(1f, Screen.width)) * width,
            panelBounds.yMin + ((Screen.height - screenPosition.y) / Mathf.Max(1f, Screen.height)) * height);
    }

    void ApplyDefaultCursor()
    {
        UnityEngine.Cursor.SetCursor(defaultCursor, defaultHotspot, cursorMode);
        isShowingClickableCursor = false;
    }

    void ApplyClickableCursor()
    {
        Texture2D cursor = clickableCursor != null ? clickableCursor : defaultCursor;
        UnityEngine.Cursor.SetCursor(cursor, clickableHotspot, cursorMode);
        isShowingClickableCursor = true;
    }
}
