using UnityEngine;
using System.IO;
using System;

public class SpriteImporter : IAssetImporter
{
    public bool CanImport(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
    }

    public ImportedSpriteData Import(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        if (AssetStorageService.HasAssetWithFileName(fileName, ImportedAssetTypes.Sprite))
        {
            Debug.LogWarning($"Sprite import skipped: a sprite named '{fileName}' is already imported.");
            return null;
        }

        byte[] fileBytes = File.ReadAllBytes(filePath);

        Texture2D texture = new Texture2D(2, 2);
        bool loaded = texture.LoadImage(fileBytes);

        if (!loaded)
        {
            Debug.LogWarning($"Failed to load image: {filePath}");
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        sprite.name = Path.GetFileNameWithoutExtension(filePath);

        var collection = new ImportedSpriteData
        {
            AssetID = Guid.NewGuid().ToString(),
            FileName = fileName,
            OriginalFilePath = filePath,
            Sprite = sprite,
            SpriteRectX = 0f,
            SpriteRectY = 0f,
            SpriteRectWidth = texture.width,
            SpriteRectHeight = texture.height
        };
        
        //store the imported sprite in a local folder.
        AssetStorageService.SaveLocalCopy(filePath, collection, ImportedAssetTypes.Sprite);

        return collection;
    }
}
