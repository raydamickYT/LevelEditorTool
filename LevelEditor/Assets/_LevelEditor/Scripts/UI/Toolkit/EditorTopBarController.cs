using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit top bar: File menu, gizmo tools, grid/snapping toggles.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class EditorTopBarController : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    [SerializeField] GameObject gridRendererObject;

    VisualElement _root;
    VisualElement _menuPanel;
    Button _menuTrigger;
    Button _gizmoMoveButton;
    Button _gizmoRotateButton;
    Button _gizmoScaleButton;
    Toggle _snappingToggle;
    Toggle _gridToggle;
    Label _shortcutTooltip;
    IVisualElementScheduledItem _tooltipDelay;

    const int ShortcutTooltipDelayMs = 500;

    readonly List<(Button button, System.Action handler)> _registeredClicks = new();
    readonly List<(VisualElement element, EventCallback<PointerEnterEvent> onEnter, EventCallback<PointerLeaveEvent> onLeave)> _hoverCallbacks = new();

    void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        if (uiDocument == null)
            return;

        VisualElement root = uiDocument.rootVisualElement;
        _root = root;
        _menuPanel = root.Q<VisualElement>("menu-file-panel");
        _menuTrigger = root.Q<Button>("menu-file-trigger");

        if (_menuTrigger != null)
            _menuTrigger.clicked += ToggleFileMenu;

        Register(root.Q<Button>("item-new-project"), OnNewFile);
        Register(root.Q<Button>("item-open"), OnOpen);
        Register(root.Q<Button>("item-save"), OnSave);
        Register(root.Q<Button>("item-export"), OnExport);
        Register(root.Q<Button>("item-import-unity-assets"), OnImportUnityAssets);
        // Register(root.Q<Button>("item-import-folder"), OnImportFolder);
        Register(root.Q<Button>("item-import-assets"), OnImportAssets);

        SetupShortcutTooltips(root);
        SetupGizmoToolbar(root);
        SetupEditorOptions(root);
        EventManager.Instance.AddDelegateListener(GimzmoEvents.OnGizmoTypeChanged, (Action<GizmoType>)OnGizmoTypeChanged);
        EventManager.Instance.AddDelegateListener(ShortcutBindingEvents.OnCommandTriggered, (Action<EditorCommand>)OnEditorCommand);
        SyncGizmoToolbarVisual(GetInitialGizmoType());
    }

    void OnDisable()
    {
        CancelShortcutTooltipDelay();
        HideShortcutTooltip();

        foreach ((VisualElement element, EventCallback<PointerEnterEvent> onEnter, EventCallback<PointerLeaveEvent> onLeave) in _hoverCallbacks)
        {
            if (element == null)
                continue;

            element.UnregisterCallback(onEnter);
            element.UnregisterCallback(onLeave);
        }

        _hoverCallbacks.Clear();

        if (_menuTrigger != null)
            _menuTrigger.clicked -= ToggleFileMenu;

        foreach ((Button button, System.Action handler) in _registeredClicks)
        {
            if (button != null)
                button.clicked -= handler;
        }

        _registeredClicks.Clear();
        _root = null;
        _menuTrigger = null;
        _menuPanel = null;
        _gizmoMoveButton = null;
        _gizmoRotateButton = null;
        _gizmoScaleButton = null;
        _snappingToggle = null;
        _gridToggle = null;
        _shortcutTooltip = null;
    }

    void OnEditorCommand(EditorCommand command)
    {
        if (command == EditorCommand.ToggleSnapping)
            ToggleSnappingFromShortcut();
    }

    void ToggleSnappingFromShortcut()
    {
        if (_snappingToggle == null)
            return;

        _snappingToggle.value = !_snappingToggle.value;
    }

    void SetupEditorOptions(VisualElement root)
    {
        _snappingToggle = root.Q<Toggle>("snapping-toggle");
        _gridToggle = root.Q<Toggle>("grid-toggle");

        if (gridRendererObject == null)
            gridRendererObject = FindGridRendererObject();

        if (_gridToggle != null)
        {
            bool gridVisible = gridRendererObject == null || gridRendererObject.activeSelf;
            _gridToggle.SetValueWithoutNotify(gridVisible);
            _gridToggle.RegisterValueChangedCallback(OnGridToggleChanged);
            RegisterShortcutTooltip(_gridToggle, "Toggle scene grid visibility");
        }

        if (_snappingToggle != null)
        {
            _snappingToggle.SetValueWithoutNotify(false);
            _snappingToggle.RegisterValueChangedCallback(OnSnappingToggleChanged);
            RegisterShortcutTooltip(_snappingToggle, "Shortcut: S");
        }
    }

    static GameObject FindGridRendererObject()
    {
        BackgroundGridController grid = FindFirstObjectByType<BackgroundGridController>(FindObjectsInactive.Include);
        return grid != null ? grid.gameObject : null;
    }

    void OnGridToggleChanged(ChangeEvent<bool> evt)
    {
        if (gridRendererObject == null)
            return;

        gridRendererObject.SetActive(evt.newValue);
    }

    void OnSnappingToggleChanged(ChangeEvent<bool> evt)
    {
        SnappingToggleService.ApplySnappingEnabled(evt.newValue);
    }

    void SetupShortcutTooltips(VisualElement root)
    {
        _shortcutTooltip = root.Q<Label>("top-bar-shortcut-tooltip");
    }

    void SetupGizmoToolbar(VisualElement root)
    {
        _gizmoMoveButton = root.Q<Button>("gizmo-move-button");
        _gizmoRotateButton = root.Q<Button>("gizmo-rotate-button");
        _gizmoScaleButton = root.Q<Button>("gizmo-scale-button");

        Register(_gizmoMoveButton, () => TriggerGizmoCommand(EditorCommand.SwitchMoveTool));
        Register(_gizmoRotateButton, () => TriggerGizmoCommand(EditorCommand.SwitchRotateTool));
        Register(_gizmoScaleButton, () => TriggerGizmoCommand(EditorCommand.SwitchScaleTool));

        RegisterShortcutTooltip(_gizmoMoveButton, "Shortcut: W");
        RegisterShortcutTooltip(_gizmoRotateButton, "Shortcut: E");
        RegisterShortcutTooltip(_gizmoScaleButton, "Shortcut: R");

        RegisterShortcutTooltip(root.Q<Button>("item-new-file"), "Shortcut: Shift + N");
        RegisterShortcutTooltip(root.Q<Button>("item-open"), "Shortcut: Shift + O");
        RegisterShortcutTooltip(root.Q<Button>("item-save"), "Shortcut: Shift + S");
        RegisterShortcutTooltip(root.Q<Button>("item-import-game-assets"), "Shortcut: Shift + I");
    }

    void RegisterShortcutTooltip(VisualElement element, string shortcutText)
    {
        if (element == null || string.IsNullOrEmpty(shortcutText))
            return;

        EventCallback<PointerEnterEvent> onEnter = _ => BeginShortcutTooltipDelay(element, shortcutText);
        EventCallback<PointerLeaveEvent> onLeave = _ =>
        {
            CancelShortcutTooltipDelay();
            HideShortcutTooltip();
        };

        element.RegisterCallback(onEnter);
        element.RegisterCallback(onLeave);
        _hoverCallbacks.Add((element, onEnter, onLeave));
    }

    void BeginShortcutTooltipDelay(VisualElement anchor, string shortcutText)
    {
        CancelShortcutTooltipDelay();
        _tooltipDelay = anchor.schedule.Execute(() => ShowShortcutTooltip(anchor, shortcutText))
            .StartingIn(ShortcutTooltipDelayMs);
    }

    void CancelShortcutTooltipDelay()
    {
        _tooltipDelay?.Pause();
        _tooltipDelay = null;
    }

    void ShowShortcutTooltip(VisualElement anchor, string shortcutText)
    {
        if (_shortcutTooltip == null || _root == null || anchor == null)
            return;

        _shortcutTooltip.text = shortcutText;
        _shortcutTooltip.style.display = DisplayStyle.Flex;
        _shortcutTooltip.BringToFront();

        Rect anchorBounds = anchor.worldBound;
        Vector2 belowAnchor = new(anchorBounds.xMin, anchorBounds.yMax + 4f);
        Vector2 localPosition = _root.WorldToLocal(belowAnchor);

        _shortcutTooltip.style.left = localPosition.x;
        _shortcutTooltip.style.top = localPosition.y;
    }

    void HideShortcutTooltip()
    {
        if (_shortcutTooltip == null)
            return;

        _shortcutTooltip.style.display = DisplayStyle.None;
    }

    static void TriggerGizmoCommand(EditorCommand command)
    {
        if (EventManager.Instance == null)
            return;

        EventManager.Instance.TriggerDelegate(ShortcutBindingEvents.OnCommandTriggered, command);
    }

    static GizmoType GetInitialGizmoType()
    {
        return GizmoHandler.Instance != null
            ? GizmoHandler.Instance.GetCurrentGizmoType()
            : GizmoType.move;
    }

    void OnGizmoTypeChanged(GizmoType type) => SyncGizmoToolbarVisual(type);

    void SyncGizmoToolbarVisual(GizmoType activeType)
    {
        SetGizmoButtonActive(_gizmoMoveButton, activeType == GizmoType.move);
        SetGizmoButtonActive(_gizmoRotateButton, activeType == GizmoType.rotate);
        SetGizmoButtonActive(_gizmoScaleButton, activeType == GizmoType.scale);
    }

    static void SetGizmoButtonActive(Button button, bool active)
    {
        if (button == null)
            return;

        if (active)
            button.AddToClassList("gizmo-tool-button-active");
        else
            button.RemoveFromClassList("gizmo-tool-button-active");
    }

    void Update()
    {
        if (!IsMenuOpen() || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 panelPosition = ScreenToPanelPosition(Mouse.current.position.ReadValue());
        if (IsInsideElement(_menuTrigger, panelPosition) || IsInsideElement(_menuPanel, panelPosition))
            return;

        CloseMenu();
    }

    void Register(Button button, System.Action handler)
    {
        if (button == null || handler == null)
            return;

        button.clicked += handler;
        _registeredClicks.Add((button, handler));
    }

    void ToggleFileMenu()
    {
        if (_menuPanel == null)
            return;

        bool open = _menuPanel.style.display == DisplayStyle.None;
        _menuPanel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void CloseMenu()
    {
        if (_menuPanel != null)
            _menuPanel.style.display = DisplayStyle.None;
    }

    bool IsMenuOpen()
    {
        return _menuPanel != null && _menuPanel.resolvedStyle.display != DisplayStyle.None;
    }

    bool IsInsideElement(VisualElement element, Vector2 panelPosition)
    {
        return element != null && element.worldBound.Contains(panelPosition);
    }

    Vector2 ScreenToPanelPosition(Vector2 screenPosition)
    {
        Rect panelBounds = _root != null
            ? _root.worldBound
            : new Rect(0f, 0f, Screen.width, Screen.height);

        float width = Mathf.Max(1f, panelBounds.width);
        float height = Mathf.Max(1f, panelBounds.height);

        return new Vector2(
            panelBounds.xMin + (screenPosition.x / Mathf.Max(1f, Screen.width)) * width,
            panelBounds.yMin + ((Screen.height - screenPosition.y) / Mathf.Max(1f, Screen.height)) * height);
    }

    void OnNewFile()
    {
        CloseMenu();
        LevelEditorFileMenuCommands.NewEmptyLevel();
    }

    void OnOpen()
    {
        CloseMenu();
        LevelEditorFileMenuCommands.OpenLevel();
    }

    void OnSave()
    {
        CloseMenu();
        LevelEditorFileMenuCommands.SaveLevel();
    }

    void OnExport()
    {
        CloseMenu();
        LevelEditorFileMenuCommands.ExportLevel();
    }

    void OnImportFolder()
    {
        CloseMenu();
        LevelEditorFileMenuCommands.ImportFolder();
    }

    void OnImportUnityAssets()
    {
        CloseMenu();
        LevelEditorFileMenuCommands.ImportGameAssets();
    }

    void OnImportAssets()
    {
        CloseMenu();
        LevelEditorFileMenuCommands.ImportAssets();
    }
}
