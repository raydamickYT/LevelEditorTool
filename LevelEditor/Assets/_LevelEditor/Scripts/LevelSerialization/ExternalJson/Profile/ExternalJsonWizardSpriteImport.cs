using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFB;
using UnityEngine;

/// <summary>
/// Lets the mapping wizard pick a sprite file from disk, import it into the object library, and return its asset id.
/// </summary>
public static class ExternalJsonWizardSpriteImport
{
    static readonly AssetImportService ImportService = new();

    public static bool TryPickAndImport(out string assetId)
    {
        assetId = string.Empty;

        ExtensionFilter[] extensions =
        {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
            new ExtensionFilter("All Files", "*"),
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select sprite for this category", "", extensions, false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return false;

        string filePath = Path.GetFullPath(paths[0]);
        string fileName = Path.GetFileName(filePath);

        ImportedAssetMetaData imported = ImportService.ImportFile(filePath);
        if (imported != null)
        {
            assetId = imported.AssetID;
            NotifyLibrary(new List<ImportedAssetMetaData> { imported });
            return !string.IsNullOrWhiteSpace(assetId);
        }

        // SpriteImporter skips duplicates by file name; reuse the existing library entry instead.
        ImportedAssetMetaData existing = FindExistingByFileName(fileName);
        if (existing != null)
        {
            assetId = existing.AssetID;
            return !string.IsNullOrWhiteSpace(assetId);
        }

        return false;
    }

    static ImportedAssetMetaData FindExistingByFileName(string fileName)
    {
        string targetName = Path.GetFileNameWithoutExtension(fileName);
        return AssetStorageService.GetAllCachedImportedAssets()
            .FirstOrDefault(asset =>
                asset != null
                && !string.IsNullOrWhiteSpace(asset.FileName)
                && string.Equals(
                    Path.GetFileNameWithoutExtension(asset.FileName),
                    targetName,
                    System.StringComparison.OrdinalIgnoreCase));
    }

    static void NotifyLibrary(List<ImportedAssetMetaData> assets)
    {
        if (EventManager.Instance == null || assets == null || assets.Count == 0)
            return;

        EventManager.Instance.TriggerDelegate(ObjectLibraryManagerEvents.UpdateObjectLibrary, assets);
    }
}
