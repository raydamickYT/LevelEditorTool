using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetRegistry : MonoBehaviour
{
    public static AssetRegistry Instance { get; }
    [FolderPath]
    public string filePath;
    public Dictionary<string, ImportedAssetData> importedSprites = new Dictionary<string, ImportedAssetData>();

    private AssetImportService assetImportService;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetImportService = new();

        if (string.IsNullOrEmpty(filePath)) return;
        
        List<ImportedAssetData> importedAssets = assetImportService.ImportFolder(filePath);

        foreach (ImportedAssetData asset in importedAssets)
        {
            importedSprites[asset.AssetID] = asset;
        }
    }

    // Update is called once per frame
    void Update()
    {

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
}
