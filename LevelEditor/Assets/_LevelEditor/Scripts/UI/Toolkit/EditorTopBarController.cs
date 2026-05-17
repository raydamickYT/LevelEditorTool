using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit top bar: File menu (new / open / save / export / import). Snapping and grid stay in uGUI elsewhere.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class EditorTopBarController : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;

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
        _menuTrigger = null;
        _menuPanel = null;
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
