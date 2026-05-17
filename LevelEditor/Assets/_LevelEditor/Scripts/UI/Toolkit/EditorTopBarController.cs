using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit top bar: File menu (new / open / save / export / import). Snapping and grid stay in uGUI elsewhere.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class EditorTopBarController : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;

    VisualElement _root;
    VisualElement _menuPanel;
    Button _menuTrigger;

    readonly List<(Button button, System.Action handler)> _registeredClicks = new();

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

        Register(root.Q<Button>("item-new-file"), OnNewFile);
        Register(root.Q<Button>("item-open"), OnOpen);
        Register(root.Q<Button>("item-save"), OnSave);
        Register(root.Q<Button>("item-export"), OnExport);
        Register(root.Q<Button>("item-import-game-assets"), OnImportGameAssets);
        Register(root.Q<Button>("item-import-folder"), OnImportFolder);
        Register(root.Q<Button>("item-import-assets"), OnImportAssets);
    }

    void OnDisable()
    {
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

    void OnImportGameAssets()
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
