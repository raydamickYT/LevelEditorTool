using UnityEditor;
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

        return new ImportedSpriteData
        {
            AssetID = Guid.NewGuid().ToString(),
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            Sprite = sprite
        };
    }
}
