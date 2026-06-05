using System.Collections.Generic;
using System.IO;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class AssetImportService
{
    // Temporary: prefab skip popups are noisy during full Unity project imports.
    const bool ShowPrefabImportSkipPopups = false;

    private List<IAssetImporter> importers = new List<IAssetImporter>
    {
        new SpriteImporter()
    };

    public ImportedAssetMetaData ImportFile(string filePath)
    {
        foreach (IAssetImporter importer in importers)
        {
            if (importer.CanImport(filePath))
            {
                return importer.Import(filePath);
            }
        }

        Debug.LogWarning($"No importer found for file: {filePath}");
        return null;
    }

    public List<ImportedAssetMetaData> ImportFolder(string folderPath)
    {
        List<ImportedAssetMetaData> importedAssets = new List<ImportedAssetMetaData>();

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("Folder path is empty.");
            return importedAssets;
        }

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"Folder does not exist: {folderPath}");
            return importedAssets;
        }

        string[] files = Directory.GetFiles(folderPath);

        foreach (string file in files)
        {
            ImportedAssetMetaData importedAsset = ImportFile(file);

            if (importedAsset != null)
            {
                importedAssets.Add(importedAsset);
            }
        }

        return importedAssets;
    }

    public List<ImportedAssetMetaData> ImportUnityProjectAssets(string projectFolderPath)
    {
        List<ImportedAssetMetaData> importedAssets = new();

        if (string.IsNullOrWhiteSpace(projectFolderPath) || !Directory.Exists(projectFolderPath))
        {
            Debug.LogWarning($"Unity project folder does not exist: {projectFolderPath}");
            EditorPopupService.ShowWarning(
                "Unity project not found",
                "The selected folder does not exist.",
                projectFolderPath);
            return importedAssets;
        }

        string assetsRoot = Path.Combine(projectFolderPath, "Assets");
        if (!Directory.Exists(assetsRoot))
        {
            Debug.LogWarning($"Selected folder is not a Unity project root (missing Assets folder): {projectFolderPath}");
            EditorPopupService.ShowWarning(
                "Invalid Unity project folder",
                "Select the Unity project root folder. It should contain an Assets folder.",
                projectFolderPath);
            return importedAssets;
        }

        Dictionary<string, string> guidToAssetPath = BuildUnityGuidLookup(assetsRoot);

        foreach (string file in Directory.GetFiles(assetsRoot, "*.*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            string relativePath = GetUnityRelativeAssetPath(assetsRoot, file);
            string folderPath = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
            string extension = Path.GetExtension(file).ToLowerInvariant();

            ImportedAssetMetaData importedAsset = null;
            if (extension == ".prefab")
                importedAsset = ImportUnityPrefabReference(projectFolderPath, file, relativePath, folderPath, guidToAssetPath);
            else
                importedAsset = ImportUnityProjectFile(file, projectFolderPath, relativePath, folderPath);

            if (importedAsset != null)
                importedAssets.Add(importedAsset);
        }

        return importedAssets;
    }

    ImportedAssetMetaData ImportUnityProjectFile(string filePath, string projectRoot, string relativePath, string folderPath)
    {
        if (!CanImportFile(filePath))
            return null;

        ImportedAssetMetaData importedAsset = ImportFile(filePath);
        if (importedAsset == null)
            return null;

        importedAsset.SourceProjectRoot = Path.GetFullPath(projectRoot);
        importedAsset.AssetRelativePath = relativePath;
        importedAsset.FolderPath = folderPath;

        if (UnityAssetMetaReader.TryGetSpritePixelsPerUnit(filePath, out float pixelsPerUnit))
            importedAsset.PixelsPerUnit = pixelsPerUnit;

        AssetStorageService.SaveMetaData(importedAsset);
        return importedAsset;
    }

    ImportedAssetMetaData ImportUnityPrefabReference(string projectRoot, string filePath, string relativePath, string folderPath, Dictionary<string, string> guidToAssetPath)
    {
        if (AssetStorageService.HasAssetAtRelativePath(relativePath, ImportedAssetTypes.Prefab))
        {
            Debug.LogWarning($"Prefab import skipped: '{relativePath}' is already imported.");
            return null;
        }

        PrefabSpriteReference spriteReference = ResolvePrefabSpriteReference(filePath, guidToAssetPath);
        if (spriteReference == null || string.IsNullOrEmpty(spriteReference.AssetPath) || !CanImportFile(spriteReference.AssetPath))
        {
            Debug.LogWarning($"Prefab import skipped: '{relativePath}' has no supported SpriteRenderer sprite.");
            if (ShowPrefabImportSkipPopups)
            {
                EditorPopupService.ShowWarning(
                    "Prefab skipped",
                    "A prefab was skipped because it has no supported SpriteRenderer sprite.",
                    relativePath);
            }
            return null;
        }

        float pixelsPerUnit = 100f;
        UnityAssetMetaReader.TryGetSpritePixelsPerUnit(spriteReference.AssetPath, out pixelsPerUnit);

        Sprite previewSprite = LoadSpritePreview(spriteReference.AssetPath, spriteReference.FileId, pixelsPerUnit, out Rect spriteRect);
        if (previewSprite == null)
        {
            Debug.LogWarning($"Prefab import skipped: could not load preview sprite for '{relativePath}'.");
            if (ShowPrefabImportSkipPopups)
            {
                EditorPopupService.ShowWarning(
                    "Prefab skipped",
                    "A prefab was skipped because its preview sprite could not be loaded.",
                    relativePath);
            }
            return null;
        }

        ImportedSpriteData prefab = new()
        {
            AssetID = Guid.NewGuid().ToString(),
            FileName = Path.GetFileName(filePath),
            OriginalFilePath = filePath,
            Sprite = previewSprite,
            AssetType = ImportedAssetTypes.Prefab,
            SourceProjectRoot = Path.GetFullPath(projectRoot),
            AssetRelativePath = relativePath,
            FolderPath = folderPath,
            SpriteRectX = spriteRect.x,
            SpriteRectY = spriteRect.y,
            SpriteRectWidth = spriteRect.width,
            SpriteRectHeight = spriteRect.height,
            PixelsPerUnit = pixelsPerUnit
        };

        AssetStorageService.SaveLocalCopy(spriteReference.AssetPath, prefab, ImportedAssetTypes.Prefab);
        prefab.FileName = Path.GetFileName(filePath);
        prefab.OriginalFilePath = filePath;
        prefab.AssetType = ImportedAssetTypes.Prefab;
        prefab.SourceProjectRoot = Path.GetFullPath(projectRoot);
        prefab.AssetRelativePath = relativePath;
        prefab.FolderPath = folderPath;
        prefab.SpriteRectX = spriteRect.x;
        prefab.SpriteRectY = spriteRect.y;
        prefab.SpriteRectWidth = spriteRect.width;
        prefab.SpriteRectHeight = spriteRect.height;
        prefab.PixelsPerUnit = pixelsPerUnit;
        AssetStorageService.SaveMetaData(prefab);
        return prefab;
    }

    bool CanImportFile(string filePath)
    {
        foreach (IAssetImporter importer in importers)
        {
            if (importer.CanImport(filePath))
                return true;
        }

        return false;
    }

    static PrefabSpriteReference ResolvePrefabSpriteReference(string prefabPath, Dictionary<string, string> guidToAssetPath)
    {
        if (guidToAssetPath == null || string.IsNullOrEmpty(prefabPath) || !File.Exists(prefabPath))
            return null;

        string yaml = File.ReadAllText(prefabPath);
        Match match = Regex.Match(yaml, @"m_Sprite:\s*\{[^}]*fileID:\s*(-?\d+)[^}]*guid:\s*([a-fA-F0-9]{32})");
        if (!match.Success)
            return null;

        string guid = match.Groups[2].Value;
        if (!guidToAssetPath.TryGetValue(guid, out string assetPath))
            return null;

        long.TryParse(match.Groups[1].Value, out long fileId);
        return new PrefabSpriteReference(assetPath, fileId);
    }

    static Dictionary<string, string> BuildUnityGuidLookup(string assetsRoot)
    {
        Dictionary<string, string> lookup = new(StringComparer.OrdinalIgnoreCase);
        foreach (string metaPath in Directory.GetFiles(assetsRoot, "*.meta", SearchOption.AllDirectories))
        {
            string assetPath = metaPath.Substring(0, metaPath.Length - ".meta".Length);
            if (!File.Exists(assetPath))
                continue;

            string meta = File.ReadAllText(metaPath);
            Match match = Regex.Match(meta, @"^guid:\s*([a-fA-F0-9]{32})", RegexOptions.Multiline);
            if (match.Success)
                lookup[match.Groups[1].Value] = assetPath;
        }

        return lookup;
    }

    static Sprite LoadSpritePreview(string filePath, long fileId, float pixelsPerUnit, out Rect spriteRect)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);
        Texture2D texture = new(2, 2);
        if (!texture.LoadImage(fileBytes))
        {
            spriteRect = default;
            return null;
        }

        spriteRect = TryGetSpriteRectFromMeta(filePath + ".meta", fileId, out Rect metaRect)
            ? metaRect
            : new Rect(0, 0, texture.width, texture.height);

        spriteRect = ClampRectToTexture(spriteRect, texture);

        Sprite sprite = Sprite.Create(
            texture,
            spriteRect,
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit > 0f ? pixelsPerUnit : 100f);

        sprite.name = Path.GetFileNameWithoutExtension(filePath);
        return sprite;
    }

    static bool TryGetSpriteRectFromMeta(string metaPath, long fileId, out Rect rect)
    {
        rect = default;
        if (fileId == 0 || !File.Exists(metaPath))
            return false;

        string meta = File.ReadAllText(metaPath);
        MatchCollection matches = Regex.Matches(
            meta,
            @"-\s*serializedVersion:\s*\d+.*?rect:\s*.*?x:\s*(?<x>-?\d+(?:\.\d+)?)\s*y:\s*(?<y>-?\d+(?:\.\d+)?)\s*width:\s*(?<w>-?\d+(?:\.\d+)?)\s*height:\s*(?<h>-?\d+(?:\.\d+)?).*?internalID:\s*(?<id>-?\d+)",
            RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            if (!long.TryParse(match.Groups["id"].Value, out long internalId) || internalId != fileId)
                continue;

            rect = new Rect(
                ParseFloat(match.Groups["x"].Value),
                ParseFloat(match.Groups["y"].Value),
                ParseFloat(match.Groups["w"].Value),
                ParseFloat(match.Groups["h"].Value));
            return rect.width > 0f && rect.height > 0f;
        }

        return false;
    }

    static Rect ClampRectToTexture(Rect rect, Texture2D texture)
    {
        float x = Mathf.Clamp(rect.x, 0f, texture.width);
        float y = Mathf.Clamp(rect.y, 0f, texture.height);
        float width = Mathf.Min(rect.width, texture.width - x);
        float height = Mathf.Min(rect.height, texture.height - y);

        if (width <= 0f || height <= 0f)
            return new Rect(0, 0, texture.width, texture.height);

        return new Rect(x, y, width, height);
    }

    static float ParseFloat(string value)
    {
        return float.TryParse(
            value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float result)
            ? result
            : 0f;
    }

    sealed class PrefabSpriteReference
    {
        public string AssetPath { get; }
        public long FileId { get; }

        public PrefabSpriteReference(string assetPath, long fileId)
        {
            AssetPath = assetPath;
            FileId = fileId;
        }
    }

    static string GetUnityRelativeAssetPath(string assetsRoot, string filePath)
    {
        string root = Path.GetFullPath(assetsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullPath = Path.GetFullPath(filePath);
        string relativeToAssets = fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            : Path.GetFileName(filePath);

        relativeToAssets = relativeToAssets.Replace('\\', '/');
        return $"Assets/{relativeToAssets}";
    }
}
