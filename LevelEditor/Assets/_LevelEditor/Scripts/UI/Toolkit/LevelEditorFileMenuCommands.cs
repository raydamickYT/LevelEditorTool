using System;
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
        Debug.Log("New empty level requested");
        if (LevelProjectDirtyState.HasUnsavedChanges())
        {
            EditorPopupService.ShowConfirmDialog(
                "Unsaved changes",
                "You have unsaved changes. Do you want to discard them and create a new level?",
                "Save and create new level",
                () => //save and confirm
                {
                    SaveLevel();
                    if (!LevelProjectDirtyState.HasUnsavedChanges())
                        EditorPopupService.RunAfterSaveFeedback(ClearSceneForNewLevel);
                },
                () => ClearSceneForNewLevel(),
                "Discard changes"); // canceltext
            return;
        }

        ClearSceneForNewLevel();
    }

    static void ClearSceneForNewLevel()
    {
        if (EventManager.Instance == null)
            return;

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, Enumerable.Empty<GameObject>());

        if (LevelObjectsRoot.Instance != null)
            LevelObjectsRoot.Instance.DestroyAllRootLevelObjects();

        ObjectRegistry.ClearAllForNewLevel();
        LevelProjectSession.ClearProject();
        LevelViewportFrameState.Instance.ResetToDefaults();

        if (ObjectLibraryManager.Instance != null)
            ObjectLibraryManager.Instance.RebuildLibraryFromAssetStorage();

        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RebuildEntireHierarchy);

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, Enumerable.Empty<GameObject>());

        LevelProjectDirtyState.MarkClean();
        LevelEditorToolLinkManifest.WriteActiveLevelIfLinked();
        ExternalLevelJsonImportSession.Clear();
    }

    public static void OpenLevel()
    {
        if (LevelProjectDirtyState.HasUnsavedChanges())
        {
            EditorPopupService.ShowConfirmDialog(
                "Unsaved changes",
                "You have unsaved changes. Do you want to save them before opening another project?",
                "Save and open",
                () =>
                {
                    SaveLevel();
                    if (!LevelProjectDirtyState.HasUnsavedChanges())
                        EditorPopupService.RunAfterSaveFeedback(PromptAndOpenLevel);
                },
                () => PromptAndOpenLevel(),
                "Discard changes"); // canceltext
            return;
        }

        PromptAndOpenLevel();
    }

    static void PromptAndOpenLevel()
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Open project folder", "", false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        string projectDirectory = Path.GetFullPath(paths[0]);
        if (!LevelProjectService.TryGetLevelJsonPath(projectDirectory, out string levelJsonPath))
        {
            EditorPopupService.ShowWarning(
                "Project not found",
                $"Select a folder that contains {LevelProjectService.DefaultLevelFileName}.",
                projectDirectory);
            return;
        }

        LevelProjectService.LoadLevelFromPath(levelJsonPath);
    }

    public static void SaveLevel()
    {
        if (TrySaveOpenProject(showToast: true))
            return;

        // New project: choose parent folder, then name the project folder.
        LevelProjectSaveUtility.PromptSaveNewProject();
    }

    /// <summary>Silent background save for auto-save; only when a project path is already open.</summary>
    public static bool TrySaveOpenProjectSilent() => TrySaveOpenProject(showToast: false);

    static bool TrySaveOpenProject(bool showToast)
    {
        if (!LevelProjectSession.HasOpenProject)
            return false;

        string dir = LevelProjectSession.CurrentProjectDirectory;
        string path = LevelProjectSession.CurrentLevelJsonPath;
        if (string.IsNullOrEmpty(path))
            path = Path.Combine(dir, LevelProjectService.DefaultLevelFileName);

        string displayName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(displayName))
            displayName = Path.GetFileNameWithoutExtension(path);

        LevelProjectService.SaveLevelToPath(path, displayName, showToast);
        return true;
    }

    /// <summary>
    /// Save a copy to a new folder (parent + project name). Switches the open project to the new location.
    /// </summary>
    public static void SaveLevelAs()
        => LevelProjectSaveUtility.PromptSaveAs();

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

    public static void ImportGameAssets()
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Unity Project Folder", "", false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            Debug.Log("No Unity project folder selected");
            return;
        }

        string projectFolder = Path.GetFullPath(paths[0]);

        if (!UnityEditorPluginDetector.IsValidUnityProjectRoot(projectFolder))
        {
            EditorPopupService.ShowWarning(
                "Invalid Unity project",
                "The selected folder must be a Unity project root (it should contain an Assets folder).",
                projectFolder);
            return;
        }

        if (!UnityEditorPluginDetector.IsPluginInstalled(projectFolder))
        {
            EditorPopupService.ShowWarning(
                "Level Editor plugin not found",
                "This Unity project does not contain the Level Editor JSON Importer plugin. " +
                "Game assets from that project cannot be loaded until the plugin is installed.",
                UnityEditorPluginDetector.BuildMissingPluginDetails(projectFolder));
            return;
        }

        if (EventManager.Instance == null)
            return;

        EventManager.Instance.TriggerDelegate(AssetRegistryEvents.ImportUnityProjectAssets, projectFolder);
    }

    public static void ImportExternalLevelJson()
    {
        if (LevelProjectDirtyState.HasUnsavedChanges())
        {
            EditorPopupService.ShowConfirmDialog(
                "Unsaved changes",
                "You have unsaved changes. Importing external JSON will replace the current level.",
                "Save and import",
                () =>
                {
                    SaveLevel();
                    if (!LevelProjectDirtyState.HasUnsavedChanges())
                        EditorPopupService.RunAfterSaveFeedback(PromptAndImportExternalLevelJson);
                },
                PromptAndImportExternalLevelJson,
                "Discard and import");
            return;
        }

        PromptAndImportExternalLevelJson();
    }

    static void PromptAndImportExternalLevelJson()
    {
        ExtensionFilter[] extensions =
        {
            new ExtensionFilter("JSON Files", "json"),
            new ExtensionFilter("All Files", "*"),
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Import external level JSON", "", extensions, false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        string jsonPath = Path.GetFullPath(paths[0]);
        ExternalLevelImportResult result = ExternalLevelJsonImportService.ImportFromFile(jsonPath);
        if (result == null)
            return;

        if (!result.Success)
        {
            if (result.NeedsMappingWizard)
                return;

            EditorPopupService.ShowWarning(
                "Import failed",
                string.IsNullOrEmpty(result.ErrorMessage)
                    ? "The selected JSON could not be imported."
                    : result.ErrorMessage,
                jsonPath);
            return;
        }

        string message = $"Imported {result.SpawnedObjectCount} objects using {result.FormatDisplayName}.";
        if (result.Warnings.Count > 0)
            message += "\n" + string.Join("\n", result.Warnings);

        EditorPopupService.ShowToast(message);
        Debug.Log($"External JSON import OK ({result.FormatId}): {result.SpawnedObjectCount} objects from {jsonPath}");
    }

    public static void ExportExternalLevelJson()
    {
        ExternalLevelExportResult result = ExternalLevelJsonExportService.PromptAndExportToFile();
        if (result == null)
            return;

        if (!result.Success)
        {
            if (!string.Equals(result.ErrorMessage, "Export cancelled.", StringComparison.Ordinal))
            {
                EditorPopupService.ShowWarning(
                    "Export failed",
                    string.IsNullOrEmpty(result.ErrorMessage)
                        ? "The level could not be exported."
                        : result.ErrorMessage);
            }

            return;
        }

        string message = $"Exported {result.ExportedObjectCount} objects to {result.FormatDisplayName}.";
        if (result.Warnings.Count > 0)
            message += "\n" + string.Join("\n", result.Warnings);

        EditorPopupService.ShowToast(message);
        Debug.Log($"External JSON export OK ({result.FormatId}): {result.OutputPath}");
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
