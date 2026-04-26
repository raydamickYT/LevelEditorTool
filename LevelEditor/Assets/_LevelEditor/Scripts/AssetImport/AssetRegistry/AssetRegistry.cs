using System.Collections.Generic;
using System.IO;
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetImportService = new();
        
        if (canImportOnStart)
            ImportSprites();
    }

    void singleFileImport()
    {
        if (assetImportService == null) return;
        if (!string.IsNullOrEmpty(filePath))
        {
            string filename = Path.GetFileName(filePath);
            importedSprites.Add(filename, assetImportService.ImportFile(filePath));
        }
    }

    void ImportSprites()
    {
        if (string.IsNullOrEmpty(filePath)) return;

        List<ImportedAssetData> importedAssets = assetImportService.ImportFolder(filePath);

        foreach (ImportedAssetData asset in importedAssets)
        {
            importedSprites[asset.AssetID] = asset;
        }

        EventManager.Instance.TriggerDelegate(ObjectLibraryManagerEvents.UpdateObjectLibrary, importedAssets);
    }
}
