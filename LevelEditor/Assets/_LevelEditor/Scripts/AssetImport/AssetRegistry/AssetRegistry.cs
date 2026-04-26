using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

public class AssetRegistry : MonoBehaviour
{
    [FolderPath]
    public string filePath = "";
    public Dictionary<string, ImportedAssetData> importedSprites = new Dictionary<string, ImportedAssetData>();

    private AssetImportService assetImportService;

    [Header("Debug")]
    [SerializeField]
    private bool canImportOnStart;

    void Awake()
    {
        EventManager.Instance.AddDelegateListener(AssetRegistryEvents.ImportAssets, (Action<string, bool>)EventCalls);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetImportService = new();

        if (canImportOnStart)
            ImportSprites(filePath);
    }
    void EventCalls(string path, bool isFolder = false)
    {
        if (assetImportService == null) return;
        if (string.IsNullOrEmpty(path)) return;

        if (isFolder)
        {
            ImportSprites(path);
            return;
        }
        singleFileImport(path);

    }

    void singleFileImport(string path)
    {
        string filename = Path.GetFileName(path);

        ImportedAssetData importedAssetData = assetImportService.ImportFile(path);
        importedSprites.Add(filename, importedAssetData);

        EventManager.Instance.TriggerDelegate(ObjectLibraryManagerEvents.UpdateObjectLibrary, new List<ImportedAssetData>() { importedAssetData });
    }

    void ImportSprites(string path)
    {
        List<ImportedAssetData> importedAssets = assetImportService.ImportFolder(path);

        foreach (ImportedAssetData asset in importedAssets)
        {
            importedSprites[asset.AssetID] = asset;
        }

        EventManager.Instance.TriggerDelegate(ObjectLibraryManagerEvents.UpdateObjectLibrary, importedAssets);
    }
}

public static class AssetRegistryEvents
{
    public const string ImportAssets = "ImportAssets";
}
