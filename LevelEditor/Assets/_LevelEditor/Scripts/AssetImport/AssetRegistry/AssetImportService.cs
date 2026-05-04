using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AssetImportService
{
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
}
