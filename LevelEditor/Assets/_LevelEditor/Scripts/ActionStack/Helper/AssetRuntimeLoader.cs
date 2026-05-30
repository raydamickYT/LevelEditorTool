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

        Sprite loadedSprite = LoadSpriteFromPath(metaData);

        if (loadedSprite != null)
        {
            spriteCache[assetID] = loadedSprite;
        }

        return loadedSprite;
    }

    private static Sprite LoadSpriteFromPath(ImportedAssetMetaData metaData)
    {
        string localFilePath = metaData?.LocalFilePath;
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
        Texture2D texture = LoadTextureForMetadata(metaData, fileBytes);

        if (texture == null)
        {
            Debug.LogWarning($"Failed to load image: {localFilePath}");
            return null;
        }

        Rect rect = GetSpriteRect(metaData, texture);

        Sprite sprite = Sprite.Create(
            texture,
            rect,
            new Vector2(0.5f, 0.5f),
            metaData.PixelsPerUnit > 0 ? metaData.PixelsPerUnit : 100f
        );

        sprite.name = string.IsNullOrWhiteSpace(metaData.FileName)
            ? Path.GetFileNameWithoutExtension(localFilePath)
            : metaData.FileName;

        return sprite;
    }

    static Texture2D LoadTextureForMetadata(ImportedAssetMetaData metaData, byte[] fileBytes)
    {
        bool hasSpriteSheetRect = metaData != null
            && metaData.SpriteRectWidth > 0f
            && metaData.SpriteRectHeight > 0f;

        if (hasSpriteSheetRect)
        {
            Texture2D texture = new(2, 2);
            if (!texture.LoadImage(fileBytes))
            {
                Object.Destroy(texture);
                return null;
            }

            return texture;
        }

        return ImageLoadUtility.LoadTextureFromBytes(fileBytes);
    }

    static Rect GetSpriteRect(ImportedAssetMetaData metaData, Texture2D texture)
    {
        if (metaData != null
            && metaData.SpriteRectWidth > 0f
            && metaData.SpriteRectHeight > 0f)
        {
            float x = Mathf.Clamp(metaData.SpriteRectX, 0f, texture.width);
            float y = Mathf.Clamp(metaData.SpriteRectY, 0f, texture.height);
            float width = Mathf.Min(metaData.SpriteRectWidth, texture.width - x);
            float height = Mathf.Min(metaData.SpriteRectHeight, texture.height - y);

            if (width > 0f && height > 0f)
                return new Rect(x, y, width, height);
        }

        return new Rect(0, 0, texture.width, texture.height);
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
