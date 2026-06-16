using System;
using System.IO;
using SFB;
using UnityEngine;

/// <summary>
/// Saves a level into a named folder under a parent directory (user-chosen project name).
/// </summary>
public static class LevelProjectSaveUtility
{
    public static void PromptSaveNewProject()
        => PromptSaveToNamedFolder($"Level_{DateTime.Now:yyyyMMdd_HHmmss}", "Save project");

    /// <summary>
    /// Always asks for a new parent folder and project name (updates the open project path after save).
    /// </summary>
    public static void PromptSaveAs()
    {
        string defaultName = GetSuggestedProjectFolderName();
        string startFolder = LevelProjectSession.HasOpenProject
            ? LevelProjectSession.CurrentProjectDirectory
            : string.Empty;

        PromptSaveToNamedFolder(defaultName, "Save project as", startFolder);
    }

    static void PromptSaveToNamedFolder(string defaultProjectName, string dialogTitle, string folderPickerStart = "")
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel(
            "Choose parent folder for project",
            folderPickerStart ?? string.Empty,
            false);

        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        string parentFolder = Path.GetFullPath(paths[0]);
        string defaultName = string.IsNullOrWhiteSpace(defaultProjectName)
            ? $"Level_{DateTime.Now:yyyyMMdd_HHmmss}"
            : defaultProjectName;

        if (EditorPopupService.Instance == null)
        {
            SaveToNamedFolder(parentFolder, defaultName, overwriteExisting: false);
            return;
        }

        EditorPopupService.ShowSaveProjectFolderDialog(
            parentFolder,
            defaultName,
            dialogTitle,
            projectFolderName => SaveToNamedFolder(parentFolder, projectFolderName, overwriteExisting: false));
    }

    static string GetSuggestedProjectFolderName()
    {
        if (!LevelProjectSession.HasOpenProject)
            return $"Level_{DateTime.Now:yyyyMMdd_HHmmss}";

        string dir = LevelProjectSession.CurrentProjectDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        string name = Path.GetFileName(dir);
        return string.IsNullOrEmpty(name) ? $"Level_{DateTime.Now:yyyyMMdd_HHmmss}" : name;
    }

    public static void SaveToNamedFolder(string parentFolder, string projectFolderName, bool overwriteExisting)
    {
        if (string.IsNullOrWhiteSpace(parentFolder))
            return;

        string safeName = LevelExportRuntime.SanitizeFolderName(projectFolderName);
        string projectDirectory = Path.Combine(Path.GetFullPath(parentFolder), safeName);

        if (Directory.Exists(projectDirectory) && !overwriteExisting)
        {
            if (EditorPopupService.Instance == null)
            {
                Debug.LogWarning($"Save cancelled: folder already exists:\n{projectDirectory}");
                return;
            }

            EditorPopupService.ShowConfirmDialog(
                "Folder exists",
                $"The folder already exists:\n{projectDirectory}\n\nOverwrite level.json and bundled assets inside it?",
                "Overwrite",
                () => SaveToNamedFolder(parentFolder, safeName, overwriteExisting: true),
                null);
            return;
        }

        Directory.CreateDirectory(projectDirectory);
        string jsonPath = Path.Combine(projectDirectory, LevelProjectService.DefaultLevelFileName);
        LevelProjectService.SaveLevelToPath(jsonPath, safeName);
    }
}
