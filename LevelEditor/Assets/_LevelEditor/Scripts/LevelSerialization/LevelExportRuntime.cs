using System;
using System.IO;
using SFB;
using UnityEngine;

/// <summary>
/// Player-build friendly export: uses SFB for a folder picker (same as <see cref="ImportButton"/>).
/// Editor menu export with a custom name still uses <see cref="ExportLevelFolderWindow"/> (UnityEditor only).
/// </summary>
public static class LevelExportRuntime
{
    /// <summary>
    /// Opens SFB folder picker for the parent directory, then creates <paramref name="subFolderName"/>
    /// (or <c>Level_yyyyMMdd_HHmmss</c> when null/whitespace) and writes <see cref="LevelProjectService.DefaultLevelFileName"/> there.
    /// </summary>
    public static void ExportLevelPickParentFolder(string subFolderName = null)
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Choose parent folder for export", "", false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        string parent = paths[0];
        string folder = string.IsNullOrWhiteSpace(subFolderName)
            ? $"Level_{DateTime.Now:yyyyMMdd_HHmmss}"
            : SanitizeFolderName(subFolderName);

        string exportDir = Path.Combine(parent, folder);
        if (Directory.Exists(exportDir))
        {
            Debug.LogWarning($"Export cancelled: folder already exists:\n{exportDir}");
            return;
        }

        Directory.CreateDirectory(exportDir);
        string jsonPath = Path.Combine(exportDir, LevelProjectService.DefaultLevelFileName);
        LevelProjectService.SaveLevelToPath(jsonPath, folder);
    }

    /// <summary>Export without dialog when your UI already knows both paths.</summary>
    public static void ExportLevelToFolder(string exportDirectory, string levelDisplayName)
    {
        if (string.IsNullOrWhiteSpace(exportDirectory))
            return;

        exportDirectory = Path.GetFullPath(exportDirectory);
        Directory.CreateDirectory(exportDirectory);
        string jsonPath = Path.Combine(exportDirectory, LevelProjectService.DefaultLevelFileName);
        LevelProjectService.SaveLevelToPath(jsonPath, levelDisplayName ?? Path.GetFileName(exportDirectory));
    }

    static string SanitizeFolderName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        name = name.Trim();
        return string.IsNullOrEmpty(name) ? $"Level_{DateTime.Now:yyyyMMdd_HHmmss}" : name;
    }
}
