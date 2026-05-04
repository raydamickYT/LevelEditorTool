using System.IO;
using UnityEngine;

public static class SpriteAssetLoader
{
    public static Sprite LoadSprite(string localFilePath, float pixelsPerUnit = 100f)
    {
        if (string.IsNullOrEmpty(localFilePath))
        {
            Debug.LogWarning("Cannot load sprite: filepath is empty");
            return null;
        }

        if (!File.Exists(localFilePath))
        {
            Debug.LogWarning("The sprite you're trying to import does not exist in the local folder");
            return null;
        }

        byte[] fileBytes = File.ReadAllBytes(localFilePath);

        Texture2D texture = new Texture2D(2, 2);
        bool loaded = texture.LoadImage(fileBytes);

        if (!loaded)
        {
            Debug.LogWarning($"Failed to load image from {localFilePath}");
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );

        sprite.name = Path.GetFileNameWithoutExtension(localFilePath);

        return sprite;
    }
}
