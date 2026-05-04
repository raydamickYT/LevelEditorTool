using System.IO;
using UnityEngine;

public static class AssetRuntimeLoader
{
    public static Sprite LoadSpriteByAssetID(string assetID)
    {
        ImportedAssetMetaData metaData = AssetStorageService.GetAssetByID(assetID);

        if (metaData == null)
        {
            Debug.LogWarning($"No metadata found for asset ID: {assetID}");
            return null;
        }

        if (metaData.AssetType != ImportedAssetTypes.Sprite)
        {
            Debug.LogWarning($"Asset ID {assetID} is not a sprite. Type: {metaData.AssetType}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(metaData.LocalFilePath))
        {
            Debug.LogWarning($"Asset ID {assetID} has no local file path.");
            return null;
        }

        if (!File.Exists(metaData.LocalFilePath))
        {
            Debug.LogWarning($"Local sprite file does not exist: {metaData.LocalFilePath}");
            return null;
        }

        byte[] fileBytes = File.ReadAllBytes(metaData.LocalFilePath);

        Texture2D texture = new Texture2D(2, 2);
        bool loaded = texture.LoadImage(fileBytes);

        if (!loaded)
        {
            Debug.LogWarning($"Failed to load sprite image: {metaData.LocalFilePath}");
            return null;
        }

        float pixelsPerUnit = metaData.PixelsPerUnit <= 0 ? 100f : metaData.PixelsPerUnit;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );

        sprite.name = Path.GetFileNameWithoutExtension(metaData.LocalFilePath);

        return sprite;
    }
}
