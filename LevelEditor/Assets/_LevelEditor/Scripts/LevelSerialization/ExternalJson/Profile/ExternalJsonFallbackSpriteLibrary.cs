using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Built-in white shape sprites for external JSON import (tinted per category at spawn time).
/// </summary>
public static class ExternalJsonFallbackSpriteLibrary
{
    const string ResourceFolder = "ExternalJson/FallbackSprites";

    static readonly Dictionary<ExternalJsonFallbackShape, Sprite> RuntimeSprites = new();
    static readonly Dictionary<ExternalJsonFallbackShape, Sprite> ResourceSprites = new();
    static bool resourcesLoaded;

    public static ExternalJsonFallbackShape GetDefaultShapeForKind(ExternalJsonShapeKind shapeKind)
    {
        return shapeKind switch
        {
            ExternalJsonShapeKind.PointObject => ExternalJsonFallbackShape.Point,
            ExternalJsonShapeKind.PointArray => ExternalJsonFallbackShape.Point,
            _ => ExternalJsonFallbackShape.Rect,
        };
    }

    public static Sprite GetSprite(ExternalJsonFallbackShape shape)
    {
        EnsureResourcesLoaded();

        if (ResourceSprites.TryGetValue(shape, out Sprite resourceSprite) && resourceSprite != null)
            return resourceSprite;

        if (!RuntimeSprites.TryGetValue(shape, out Sprite runtimeSprite) || runtimeSprite == null)
        {
            runtimeSprite = shape == ExternalJsonFallbackShape.Point
                ? CreateSolidSprite(16, 16)
                : CreateSolidSprite(32, 32);
            RuntimeSprites[shape] = runtimeSprite;
        }

        return runtimeSprite;
    }

    static void EnsureResourcesLoaded()
    {
        if (resourcesLoaded)
            return;

        resourcesLoaded = true;
        TryLoadResource(ExternalJsonFallbackShape.Point, "fallback_point");
        TryLoadResource(ExternalJsonFallbackShape.Rect, "fallback_rect");
    }

    static void TryLoadResource(ExternalJsonFallbackShape shape, string resourceName)
    {
        Sprite sprite = Resources.Load<Sprite>($"{ResourceFolder}/{resourceName}");
        if (sprite != null)
            ResourceSprites[shape] = sprite;
    }

    static Sprite CreateSolidSprite(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f);
    }
}
