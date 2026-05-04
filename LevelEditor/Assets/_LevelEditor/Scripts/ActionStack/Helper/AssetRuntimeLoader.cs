using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class AssetRuntimeLoader
{
    //memory cache to reduce load times on copy/paste
    private static readonly Dictionary<string, Sprite> spriteCache = new();

    public static Sprite LoadSpriteByAssetID(string assetID)
    {
        if (string.IsNullOrEmpty(assetID))
        {
            Debug.LogWarning("Cannot load sprite: assetID is empty");
            return null;
        }

        if (spriteCache.TryGetValue(assetID, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        ImportedAssetMetaData metaData = AssetStorageService.GetAssetByID(assetID);

        if (metaData == null)
        {
            Debug.LogWarning($"No metadata found for AssetID: {assetID}");
            return null;
        }

        Sprite loadedSprite = LoadSpriteFromPath(metaData.LocalFilePath, metaData.FileName);

        if (loadedSprite != null)
        {
            spriteCache[assetID] = loadedSprite;
        }

        return loadedSprite;
    }

    private static Sprite LoadSpriteFromPath(string localFilePath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            Debug.LogWarning("Cannot load sprite: LocalFilePath is empty.");
            return null;
        }

        if (!File.Exists(localFilePath))
        {
            Debug.LogWarning($"Sprite file does not exist: {localFilePath}");
            return null;
        }

        byte[] fileBytes = File.ReadAllBytes(localFilePath);

        Texture2D texture = new Texture2D(2, 2);
        bool loaded = texture.LoadImage(fileBytes);

        if (!loaded)
        {
            Debug.LogWarning($"Failed to load image: {localFilePath}");
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        sprite.name = string.IsNullOrWhiteSpace(fileName)
            ? Path.GetFileNameWithoutExtension(localFilePath)
            : fileName;

        return sprite;
    }

    public static void ClearCache()
    {
        spriteCache.Clear();
    }

    public static void RemoveFromCache(string assetID)
    {
        if (string.IsNullOrWhiteSpace(assetID))
            return;

        spriteCache.Remove(assetID);
    }
}
