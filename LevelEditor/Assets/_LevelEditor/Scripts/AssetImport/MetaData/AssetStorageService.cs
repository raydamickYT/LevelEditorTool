using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// this class is responsible for storing all imported assets in local folders and creating links to meta data files. 
/// this'll make it possible to reference these assets later on. 
/// **note: I've added a lot of comments here since this is new to me so I need them**
/// </summary>
public static class AssetStorageService
{
    //appcontext.BaseDirectory takes the root folder of where the application is stored.
    //but when we're in the editor it should store it in the project directory
#if UNITY_EDITOR
    private static readonly string RootFolder = Path.Combine(Application.dataPath, "..", "UserData");
#else
    private static readonly string RootFolder = Path.Combine(System.AppContext.BaseDirectory, "UserData");
#endif


    private static readonly string AssetFolder = Path.Combine(RootFolder, "Assets");

    private static readonly string SpriteFolder = Path.Combine(AssetFolder, "Sprites");
    private const string ProjectAssetsFolderName = "ProjectAssets";
    private const string ProjectAssetSpritesFolderName = "Sprites";

    //caching meta data
    private static AssetMetaDataCollection cachedMetaData;
    private static readonly Dictionary<string, ImportedAssetMetaData> assetLookup = new();
    private static bool metaDataLoaded;

    //this function fills the ImportedSpriteData completely and sends it to the metaData Function
    public static void SaveLocalCopy(string originalPath, ImportedSpriteData data, string type)
    {
        Debug.Log(RootFolder); //to check where it stores it on editor runtime while testing.

        //create the sprite dir
        Directory.CreateDirectory(SpriteFolder);

        //setup some data we want to save later
        string extension = Path.GetExtension(originalPath);
        string safeFileName = data.AssetID + extension;
        string destinationPath = Path.Combine(SpriteFolder, safeFileName);

        //create a copy of the asset and store it in the pre-determined folder
        File.Copy(originalPath, destinationPath, overwrite: true);

        //save all the paths and names in the data class
        data.OriginalFilePath = originalPath;
        data.FileName = Path.GetFileName(originalPath);
        data.LocalFilePath = destinationPath;
        data.AssetType = type;

        if (type == ImportedAssetTypes.Sprite) //todo this can become a switch case when there's different types of assets we import.
        {
            SaveSprite(data);
        }


        SaveMetaData(data);
    }

    //this function stores the earlier made ImportedSpriteData and stores it in a metadata file
    public static void SaveMetaData(ImportedAssetMetaData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.AssetID))
            return;

        //check if there's already metadata stored
        AssetMetaDataCollection collection = GetCachedMetaData();

        int existingIndex = collection.Assets.FindIndex(asset => asset.AssetID == data.AssetID);

        //if not
        if (existingIndex >= 0)
        {
            //create a new one
            collection.Assets[existingIndex] = data;
        }
        else
        {
            //else add this asset
            collection.Assets.Add(data);
        }

        assetLookup[data.AssetID] = data;
    }

    /// <summary>
    /// Session-only registry: do not persist <c>UserData/asset_registry.json</c> across runs.
    /// The tool loads registry data from a saved project folder when the user opens a project.
    /// </summary>
    private static AssetMetaDataCollection LoadMetaDataFromDisk()
    {
        return new AssetMetaDataCollection { Assets = new List<ImportedAssetMetaData>() };
    }

    private static AssetMetaDataCollection GetCachedMetaData()
    {
        if (metaDataLoaded && cachedMetaData != null)
        {
            return cachedMetaData;
        }

        cachedMetaData = LoadMetaDataFromDisk();
        RebuildAssetLookup();

        metaDataLoaded = true;

        return cachedMetaData;
    }
    private static void RebuildAssetLookup()
    {
        assetLookup.Clear();

        if (cachedMetaData == null || cachedMetaData.Assets == null)
        {
            return;
        }

        foreach (ImportedAssetMetaData asset in cachedMetaData.Assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.AssetID))
            {
                continue;
            }

            assetLookup[asset.AssetID] = asset;
        }
    }

    private static void SaveSprite(ImportedSpriteData data)
    {
        //save the sprite in the data class.
        if (data.Sprite != null)
        {
            data.Width = (int)data.Sprite.rect.width;
            data.Height = (int)data.Sprite.rect.height;
        }
    }

    public static ImportedAssetMetaData GetAssetByID(string assetID)
    {
        if (string.IsNullOrWhiteSpace(assetID))
        {
            return null;
        }

        GetCachedMetaData();

        if (assetLookup.TryGetValue(assetID, out ImportedAssetMetaData asset))
        {
            return asset;
        }

        return null;
    }

    public static bool HasAssetWithFileName(string fileName, string assetType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        GetCachedMetaData();
        if (cachedMetaData?.Assets == null)
            return false;

        string targetFileName = Path.GetFileName(fileName);
        string targetNameWithoutExtension = Path.GetFileNameWithoutExtension(targetFileName);

        return cachedMetaData.Assets.Any(asset =>
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.FileName))
                return false;

            if (!string.IsNullOrWhiteSpace(assetType)
                && !string.Equals(asset.AssetType, assetType, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string existingFileName = Path.GetFileName(asset.FileName);
            string existingNameWithoutExtension = Path.GetFileNameWithoutExtension(existingFileName);

            return string.Equals(existingFileName, targetFileName, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(existingNameWithoutExtension, targetNameWithoutExtension, System.StringComparison.OrdinalIgnoreCase);
        });
    }

    public static bool HasAssetAtRelativePath(string assetRelativePath, string assetType = null)
    {
        if (string.IsNullOrWhiteSpace(assetRelativePath))
            return false;

        GetCachedMetaData();
        if (cachedMetaData?.Assets == null)
            return false;

        string normalizedPath = NormalizeRelativePath(assetRelativePath);
        return cachedMetaData.Assets.Any(asset =>
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.AssetRelativePath))
                return false;

            if (!string.IsNullOrWhiteSpace(assetType)
                && !string.Equals(asset.AssetType, assetType, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.Equals(
                NormalizeRelativePath(asset.AssetRelativePath),
                normalizedPath,
                System.StringComparison.OrdinalIgnoreCase);
        });
    }

    static string NormalizeRelativePath(string path)
    {
        return (path ?? string.Empty).Replace('\\', '/').Trim('/');
    }

    /// <summary>All entries currently in the in-memory registry (after <see cref="GetCachedMetaData"/>).</summary>
    public static List<ImportedAssetMetaData> GetAllCachedImportedAssets()
    {
        GetCachedMetaData();
        if (cachedMetaData?.Assets == null)
            return new List<ImportedAssetMetaData>();

        // One row per AssetID (registry JSON can theoretically contain duplicates after merges).
        return cachedMetaData.Assets
            .Where(a => a != null && !string.IsNullOrWhiteSpace(a.AssetID))
            .GroupBy(a => a.AssetID)
            .Select(g => g.First())
            .ToList();
    }

    public static void ClearMetaDataCache()
    {
        cachedMetaData = null;
        assetLookup.Clear();
        metaDataLoaded = false;
    }
    public static void ReloadMetaDataCache()
    {
        ClearMetaDataCache();
        GetCachedMetaData();
    }

    /// <summary>
    /// Merges <c>BundledAssets/asset_registry.json</c> under <paramref name="levelDirectory"/> into the in-memory registry
    /// and resolves <see cref="ImportedAssetMetaData.LocalFilePath"/> to absolute paths on disk.
    /// </summary>
    public static void MergeBundledAssetsFromLevelFolder(string levelDirectory)
    {
        if (string.IsNullOrEmpty(levelDirectory))
            return;

        string registryPath = Path.Combine(levelDirectory, "BundledAssets", "asset_registry.json");
        if (!File.Exists(registryPath))
            return;

        string json = File.ReadAllText(registryPath);
        AssetMetaDataCollection fragment = JsonUtility.FromJson<AssetMetaDataCollection>(json);
        if (fragment == null || fragment.Assets == null)
            return;

        GetCachedMetaData();

        foreach (ImportedAssetMetaData asset in fragment.Assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.AssetID))
                continue;

            string rel = asset.LocalFilePath.Replace('\\', '/');
            string abs = Path.GetFullPath(Path.Combine(levelDirectory, rel.Replace('/', Path.DirectorySeparatorChar)));

            if (!File.Exists(abs))
            {
                Debug.LogWarning($"Bundled asset file missing for {asset.AssetID}: {abs}");
                continue;
            }

            asset.LocalFilePath = abs;

            int existingIndex = cachedMetaData.Assets.FindIndex(a => a != null && a.AssetID == asset.AssetID);
            if (existingIndex >= 0)
                cachedMetaData.Assets[existingIndex] = asset;
            else
                cachedMetaData.Assets.Add(asset);

            assetLookup[asset.AssetID] = asset;
        }

        AssetRuntimeLoader.ClearCache();
    }

    /// <summary>Serializes the current registry to a level folder (full snapshot for reopening the project).</summary>
    public static void WriteProjectRegistrySnapshot(string destinationFilePath)
    {
        if (string.IsNullOrEmpty(destinationFilePath))
            return;

        GetCachedMetaData();
        if (cachedMetaData == null)
            cachedMetaData = new AssetMetaDataCollection { Assets = new List<ImportedAssetMetaData>() };

        string dir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        AssetMetaDataCollection projectSnapshot = new() { Assets = new List<ImportedAssetMetaData>() };
        foreach (ImportedAssetMetaData asset in cachedMetaData.Assets
            .Where(a => a != null && !string.IsNullOrWhiteSpace(a.AssetID))
            .GroupBy(a => a.AssetID)
            .Select(g => g.First()))
        {
            ImportedAssetMetaData clone = CloneMetaData(asset);
            TryCopyAssetIntoProjectSnapshot(asset, clone, dir);
            projectSnapshot.Assets.Add(clone);
        }

        File.WriteAllText(destinationFilePath, JsonUtility.ToJson(projectSnapshot, true), Encoding.UTF8);
    }

    static ImportedAssetMetaData CloneMetaData(ImportedAssetMetaData asset)
    {
        string json = JsonUtility.ToJson(asset);
        return JsonUtility.FromJson<ImportedAssetMetaData>(json);
    }

    static bool TryCopyAssetIntoProjectSnapshot(ImportedAssetMetaData sourceAsset, ImportedAssetMetaData snapshotAsset, string projectDirectory)
    {
        if (sourceAsset == null || snapshotAsset == null || string.IsNullOrEmpty(projectDirectory))
            return false;

        string sourcePath = ResolveSnapshotSourcePath(sourceAsset);
        if (string.IsNullOrEmpty(sourcePath))
            return false;

        string extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrEmpty(extension))
            extension = ".png";

        string relativePath = Path.Combine(ProjectAssetsFolderName, ProjectAssetSpritesFolderName, $"{sourceAsset.AssetID}{extension}")
            .Replace('\\', '/');
        string destinationPath = Path.GetFullPath(Path.Combine(projectDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        string destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        string sourceFullPath = Path.GetFullPath(sourcePath);
        if (!sourceFullPath.Equals(destinationPath, System.StringComparison.OrdinalIgnoreCase))
        {
            byte[] bytes = File.ReadAllBytes(sourceFullPath);
            File.WriteAllBytes(destinationPath, bytes);
        }

        snapshotAsset.LocalFilePath = relativePath;
        return true;
    }

    static string ResolveSnapshotSourcePath(ImportedAssetMetaData asset)
    {
        if (asset == null)
            return null;

        if (!string.IsNullOrWhiteSpace(asset.LocalFilePath) && File.Exists(asset.LocalFilePath))
            return Path.GetFullPath(asset.LocalFilePath);

        if (string.Equals(asset.AssetType, ImportedAssetTypes.Sprite, System.StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(asset.OriginalFilePath)
            && File.Exists(asset.OriginalFilePath))
        {
            return Path.GetFullPath(asset.OriginalFilePath);
        }

        return null;
    }

    /// <summary>Merges a full registry snapshot from a level folder into memory (paths resolved against disk / level folder).</summary>
    public static void MergeProjectRegistrySnapshot(string projectRegistryFilePath, string levelDirectory)
    {
        if (string.IsNullOrEmpty(projectRegistryFilePath) || !File.Exists(projectRegistryFilePath))
            return;

        string json = File.ReadAllText(projectRegistryFilePath, Encoding.UTF8);
        AssetMetaDataCollection fragment = JsonUtility.FromJson<AssetMetaDataCollection>(json);
        if (fragment == null || fragment.Assets == null)
            return;

        GetCachedMetaData();

        foreach (ImportedAssetMetaData asset in fragment.Assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.AssetID))
                continue;

            string resolved = ResolveRegistryAssetPath(asset, levelDirectory);
            if (string.IsNullOrEmpty(resolved) || !File.Exists(resolved))
            {
                Debug.LogWarning($"Project registry: file missing for {asset.AssetID} ({asset.LocalFilePath})");
                continue;
            }

            asset.LocalFilePath = resolved;

            int existingIndex = cachedMetaData.Assets.FindIndex(a => a != null && a.AssetID == asset.AssetID);
            if (existingIndex >= 0)
                cachedMetaData.Assets[existingIndex] = asset;
            else
                cachedMetaData.Assets.Add(asset);

            assetLookup[asset.AssetID] = asset;
        }

        AssetRuntimeLoader.ClearCache();
    }

    static string ResolveRegistryAssetPath(ImportedAssetMetaData asset, string levelDirectory)
    {
        if (asset == null)
            return null;

        string localPath = asset.LocalFilePath;
        if (string.IsNullOrWhiteSpace(localPath))
            localPath = null;

        if (!string.IsNullOrWhiteSpace(localPath) && File.Exists(localPath))
            return Path.GetFullPath(localPath);

        if (!string.IsNullOrWhiteSpace(localPath) && !string.IsNullOrEmpty(levelDirectory))
        {
            string combined = Path.GetFullPath(Path.Combine(levelDirectory, localPath.Replace('/', Path.DirectorySeparatorChar)));
            if (File.Exists(combined))
                return combined;
        }

        // Old project snapshots could point at session-only UserData. For sprite imports we can still
        // recover from the original Unity project path if that source project is available.
        if (string.Equals(asset.AssetType, ImportedAssetTypes.Sprite, System.StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(asset.OriginalFilePath) && File.Exists(asset.OriginalFilePath))
                return Path.GetFullPath(asset.OriginalFilePath);

            if (!string.IsNullOrWhiteSpace(asset.SourceProjectRoot) && !string.IsNullOrWhiteSpace(asset.AssetRelativePath))
            {
                string sourceProjectPath = Path.GetFullPath(Path.Combine(
                    asset.SourceProjectRoot,
                    asset.AssetRelativePath.Replace('/', Path.DirectorySeparatorChar)));

                if (File.Exists(sourceProjectPath))
                    return sourceProjectPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Clears in-memory registry and deletes session <c>UserData</c> (imported sprite copies, etc.) when the tool closes.
    /// </summary>
    public static void ResetRuntimeWorkspace()
    {
        ClearMetaDataCache();
        AssetRuntimeLoader.ClearCache();
        LevelProjectSession.ClearProject();

        try
        {
            if (Directory.Exists(RootFolder))
                Directory.Delete(RootFolder, true);
        }
        catch (IOException ex)
        {
            Debug.LogWarning("Could not delete session UserData folder: " + ex.Message);
        }
    }
}


public static class ImportedAssetTypes
{
    public const string Sprite = "Sprite";
    public const string Prefab = "Prefab";
}