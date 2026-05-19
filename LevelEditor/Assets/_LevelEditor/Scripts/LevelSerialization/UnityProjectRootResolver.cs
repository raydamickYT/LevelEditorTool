using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if !UNITY_EDITOR
using SFB;
#endif

/// <summary>
/// Resolves Unity project roots for registry entries using relative paths, a per-level link file,
/// and an optional folder picker when assets moved to another machine.
/// </summary>
public static class UnityProjectRootResolver
{
    public const string LinkFileName = "unity_project_link.json";

    [Serializable]
    sealed class UnityProjectLinkFile
    {
        public string unityProjectRoot = "";
    }

    public static bool NeedsExternalUnityProject(ImportedAssetMetaData asset)
    {
        if (asset == null || string.IsNullOrWhiteSpace(asset.AssetRelativePath))
            return false;

        return asset.AssetRelativePath.Replace('\\', '/')
            .StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
    }

    public static void SanitizeAssetPathsForExport(ImportedAssetMetaData asset, string levelDirectory)
    {
        if (asset == null || string.IsNullOrWhiteSpace(levelDirectory))
            return;

        if (!string.IsNullOrWhiteSpace(asset.LocalFilePath) && Path.IsPathRooted(asset.LocalFilePath))
        {
            string relativeLocal = TryMakeRelativePath(levelDirectory, asset.LocalFilePath);
            if (!string.IsNullOrEmpty(relativeLocal))
                asset.LocalFilePath = relativeLocal;
        }

        if (!string.IsNullOrWhiteSpace(asset.OriginalFilePath) && Path.IsPathRooted(asset.OriginalFilePath))
        {
            if (!string.IsNullOrWhiteSpace(asset.LocalFilePath) && !Path.IsPathRooted(asset.LocalFilePath))
                asset.OriginalFilePath = "";
            else
            {
                string relativeOriginal = TryMakeRelativePath(levelDirectory, asset.OriginalFilePath);
                asset.OriginalFilePath = relativeOriginal ?? "";
            }
        }

        if (!string.IsNullOrWhiteSpace(asset.SourceProjectRoot) && Path.IsPathRooted(asset.SourceProjectRoot))
        {
            string relativeRoot = TryMakeRelativePath(levelDirectory, asset.SourceProjectRoot);
            asset.SourceProjectRoot = relativeRoot ?? new DirectoryInfo(asset.SourceProjectRoot).Name;
        }
    }

    public static string ResolveSourceProjectRoot(string levelDirectory, string storedSourceProjectRoot)
    {
        if (!string.IsNullOrWhiteSpace(storedSourceProjectRoot))
        {
            string fromStored = ResolveStoredRootPath(levelDirectory, storedSourceProjectRoot);
            if (!string.IsNullOrEmpty(fromStored))
                return fromStored;
        }

        return ResolveLinkedRoot(levelDirectory);
    }

    public static string TryResolveUnityAssetAbsolutePath(string levelDirectory, ImportedAssetMetaData asset)
    {
        if (asset == null || string.IsNullOrWhiteSpace(asset.AssetRelativePath))
            return null;

        string projectRoot = ResolveSourceProjectRoot(levelDirectory, asset.SourceProjectRoot);
        if (string.IsNullOrEmpty(projectRoot))
            return null;

        string absolutePath = Path.GetFullPath(Path.Combine(
            projectRoot,
            asset.AssetRelativePath.Replace('/', Path.DirectorySeparatorChar)));

        return File.Exists(absolutePath) ? absolutePath : null;
    }

    public static bool TryPromptAndSaveLinkedRoot(string levelDirectory, string details = null)
    {
        string picked = PickUnityProjectFolder(levelDirectory);
        if (string.IsNullOrWhiteSpace(picked))
            return false;

        if (!Directory.Exists(Path.Combine(picked, "Assets")))
        {
            EditorPopupService.ShowWarning(
                "Invalid Unity project",
                "The selected folder must be a Unity project root (it should contain an Assets folder).",
                picked);
            return false;
        }

        SaveLinkedRoot(levelDirectory, picked);

        string message = "Unity project location updated for this level.";
        if (!string.IsNullOrWhiteSpace(details))
            message += "\n\n" + details;

        EditorPopupService.ShowInfo("Project linked", message, picked);
        return true;
    }

    public static string ResolveLinkedRoot(string levelDirectory)
    {
        if (string.IsNullOrWhiteSpace(levelDirectory))
            return null;

        string linkPath = Path.Combine(levelDirectory, LinkFileName);
        if (!File.Exists(linkPath))
            return null;

        UnityProjectLinkFile link = JsonUtility.FromJson<UnityProjectLinkFile>(File.ReadAllText(linkPath, Encoding.UTF8));
        if (link == null || string.IsNullOrWhiteSpace(link.unityProjectRoot))
            return null;

        return ResolveStoredRootPath(levelDirectory, link.unityProjectRoot);
    }

    public static void SaveLinkedRoot(string levelDirectory, string absoluteUnityProjectRoot)
    {
        if (string.IsNullOrWhiteSpace(levelDirectory) || string.IsNullOrWhiteSpace(absoluteUnityProjectRoot))
            return;

        string fullRoot = Path.GetFullPath(absoluteUnityProjectRoot);
        string stored = TryMakeRelativePath(levelDirectory, fullRoot) ?? fullRoot;

        UnityProjectLinkFile link = new() { unityProjectRoot = stored.Replace('\\', '/') };
        string linkPath = Path.Combine(levelDirectory, LinkFileName);
        File.WriteAllText(linkPath, JsonUtility.ToJson(link, true), Encoding.UTF8);
    }

    static string ResolveStoredRootPath(string levelDirectory, string storedRoot)
    {
        if (string.IsNullOrWhiteSpace(storedRoot))
            return null;

        storedRoot = storedRoot.Trim();
        if (Path.IsPathRooted(storedRoot))
        {
            return Directory.Exists(storedRoot) ? Path.GetFullPath(storedRoot) : null;
        }

        string combined = Path.GetFullPath(Path.Combine(
            levelDirectory,
            storedRoot.Replace('/', Path.DirectorySeparatorChar)));

        if (Directory.Exists(combined))
            return combined;

        return null;
    }

    static string TryMakeRelativePath(string baseDirectory, string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory) || string.IsNullOrWhiteSpace(absolutePath))
            return null;

        try
        {
            string relative = Path.GetRelativePath(
                Path.GetFullPath(baseDirectory),
                Path.GetFullPath(absolutePath));

            if (string.IsNullOrWhiteSpace(relative)
                || relative.StartsWith("..", StringComparison.Ordinal)
                || Path.IsPathRooted(relative))
            {
                return null;
            }

            return relative.Replace('\\', '/');
        }
        catch (Exception)
        {
            return null;
        }
    }

    static string PickUnityProjectFolder(string levelDirectory)
    {
#if UNITY_EDITOR
        string start = ResolveLinkedRoot(levelDirectory);
        if (string.IsNullOrEmpty(start))
            start = Directory.Exists(levelDirectory) ? levelDirectory : "";

        return EditorUtility.OpenFolderPanel("Locate Unity project for this level", start, "");
#else
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Locate Unity project for this level", "", false);
        if (paths == null || paths.Length == 0)
            return null;

        return paths[0];
#endif
    }
}
