using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

public class AssetRegistry : MonoBehaviour
{
    [FolderPath]
    public string filePath = "";
    public Dictionary<string, ImportedAssetMetaData> importedSprites = new Dictionary<string, ImportedAssetMetaData>();

    private AssetImportService assetImportService;

    [Header("Debug")]
    [SerializeField]
    private bool canImportOnStart;

    void Awake()
    {
        EventManager.Instance.AddDelegateListener(AssetRegistryEvents.ImportAssets, (Action<string, bool>)EventCalls);
        EventManager.Instance.AddDelegateListener(AssetRegistryEvents.ImportUnityProjectAssets, (Action<string>)ImportUnityProjectAssets);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetImportService = new();

        if (canImportOnStart)
            MultiFileImport(filePath);
    }
    void EventCalls(string path, bool isMultiFile = false)
    {
        if (assetImportService == null) return;
        if (string.IsNullOrEmpty(path)) return;

        if (isMultiFile)
        {
            MultiFileImport(path);
            return;
        }
        singleFileImport(path);

    }

    public void ImportUnityProjectAssets(string projectFolderPath)
    {
        if (assetImportService == null || string.IsNullOrEmpty(projectFolderPath))
            return;

        string fullProjectPath = Path.GetFullPath(projectFolderPath);

        if (!UnityEditorPluginDetector.IsValidUnityProjectRoot(fullProjectPath)
            || !UnityEditorPluginDetector.IsPluginInstalled(fullProjectPath))
        {
            return;
        }

        List<ImportedAssetMetaData> importedAssets = assetImportService.ImportUnityProjectAssets(fullProjectPath);
        foreach (ImportedAssetMetaData asset in importedAssets)
        {
            importedSprites[asset.AssetID] = asset;
        }

        LevelEditorUnityLinkSession.SetLinked(fullProjectPath);
        LevelEditorGlobalUnityLink.Save(fullProjectPath);

        if (LevelProjectSession.HasOpenProject)
            UnityProjectRootResolver.SaveLinkedRoot(LevelProjectSession.CurrentProjectDirectory, fullProjectPath);

        LevelEditorToolLinkManifest.WriteLinkEstablished(fullProjectPath);

        EditorPopupService.ShowInfo(
            "Unity project linked",
            $"Loaded {importedAssets.Count} assets from the linked Unity project.",
            fullProjectPath);

        EventManager.Instance.TriggerDelegate(ObjectLibraryManagerEvents.UpdateObjectLibrary, importedAssets);
    }

    void singleFileImport(string path)
    {
        ImportedAssetMetaData importedAssetData = assetImportService.ImportFile(path);
        if (importedAssetData == null) return;

        importedSprites[importedAssetData.AssetID] = importedAssetData;

        EventManager.Instance.TriggerDelegate(ObjectLibraryManagerEvents.UpdateObjectLibrary, new List<ImportedAssetMetaData>() { importedAssetData });
    }

    void MultiFileImport(string path)
    {
        List<ImportedAssetMetaData> importedAssets = assetImportService.ImportFolder(path);

        foreach (ImportedAssetMetaData asset in importedAssets)
        {
            importedSprites[asset.AssetID] = asset;
        }

        EventManager.Instance.TriggerDelegate(ObjectLibraryManagerEvents.UpdateObjectLibrary, importedAssets);
    }

    public bool TryGetAsset(string assetID, out ImportedAssetMetaData assetData)
    {
        return importedSprites.TryGetValue(assetID, out assetData);
    }
}

public static class AssetRegistryEvents
{
    public const string ImportAssets = "ImportAssets";
    public const string ImportUnityProjectAssets = "ImportUnityProjectAssets";
}
