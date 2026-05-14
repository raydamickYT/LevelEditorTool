using System.IO;
using System.Linq;
using SFB;
using UnityEngine;

/// <summary>
/// Shared file / import actions for the level editor (uGUI buttons and UI Toolkit top bar).
/// </summary>
public static class LevelEditorFileMenuCommands
{
    public static void NewEmptyLevel()
    {
        if (EventManager.Instance == null)
            return;

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, Enumerable.Empty<GameObject>());

        if (LevelObjectsRoot.Instance != null)
            LevelObjectsRoot.Instance.DestroyAllRootLevelObjects();

        ObjectRegistry.ClearAllForNewLevel();
        LevelProjectSession.ClearProject();

        if (ObjectLibraryManager.Instance != null)
            ObjectLibraryManager.Instance.RebuildLibraryFromAssetStorage();

        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RebuildEntireHierarchy);

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, Enumerable.Empty<GameObject>());
    }

    public static void OpenLevel()
    {
        ExtensionFilter[] filters = { new ExtensionFilter("Level JSON", "json") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open level", "", filters, false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        string full = Path.GetFullPath(paths[0]);
        LevelProjectService.LoadLevelFromPath(full);
    }

    public static void SaveLevel()
    {
        if (LevelProjectSession.HasOpenProject)
        {
            string dir = LevelProjectSession.CurrentProjectDirectory;
            string path = LevelProjectSession.CurrentLevelJsonPath;
            if (string.IsNullOrEmpty(path))
                path = Path.Combine(dir, LevelProjectService.DefaultLevelFileName);

            string displayName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(displayName))
                displayName = Path.GetFileNameWithoutExtension(path);

            LevelProjectService.SaveLevelToPath(path, displayName);
            return;
        }

        // New project: user picks a filename — we create a folder with that name and write level.json inside.
        string picked = StandaloneFileBrowser.SaveFilePanel(
            "Save new project",
            "",
            "MyLevel",
            "json");

        if (string.IsNullOrEmpty(picked))
            return;

        string projectName = Path.GetFileNameWithoutExtension(picked);
        string parentDir = Path.GetDirectoryName(picked);
        if (string.IsNullOrEmpty(parentDir) || string.IsNullOrEmpty(projectName))
            return;

        string projectRoot = Path.Combine(parentDir, projectName);
        Directory.CreateDirectory(projectRoot);

        string jsonPath = Path.Combine(projectRoot, LevelProjectService.DefaultLevelFileName);
        LevelProjectService.SaveLevelToPath(jsonPath, projectName);
    }

    /// <summary>
    /// Writes the current level into the open project folder (same data as Save). If nothing is saved yet, runs <see cref="SaveLevel"/> first.
    /// </summary>
    public static void ExportLevel()
    {
        if (!LevelProjectSession.HasOpenProject)
        {
            Debug.Log("Pick a folder and name for the new project (Save), then export runs into that folder.");
            SaveLevel();
            if (!LevelProjectSession.HasOpenProject)
                return;
        }

        string dir = LevelProjectSession.CurrentProjectDirectory;
        string displayName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        LevelExportRuntime.ExportLevelToFolder(dir, displayName);
    }

    public static void ImportFolder()
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Asset Folder", "", false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            Debug.Log("No folder selected");
            return;
        }

        if (EventManager.Instance == null)
            return;

        EventManager.Instance.TriggerDelegate(AssetRegistryEvents.ImportAssets, paths[0], true);
    }

    public static void ImportAssets()
    {
        ExtensionFilter[] extensions =
        {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
            new ExtensionFilter("All Files", "*")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select File", "", extensions, true);
        if (paths == null || paths.Length == 0)
        {
            Debug.Log("No file selected");
            return;
        }

        if (EventManager.Instance == null)
            return;

        foreach (string path in paths)
            EventManager.Instance.TriggerDelegate(AssetRegistryEvents.ImportAssets, path, false);
    }
}
