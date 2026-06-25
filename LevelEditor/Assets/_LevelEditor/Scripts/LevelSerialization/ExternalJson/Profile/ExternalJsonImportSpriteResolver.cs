using UnityEngine;

public readonly struct ExternalJsonResolvedSprite
{
    public Sprite Sprite { get; }
    public string AssetId { get; }
    public Color Tint { get; }
    public bool UseCategoryTint { get; }

    public ExternalJsonResolvedSprite(Sprite sprite, string assetId, Color tint, bool useCategoryTint)
    {
        Sprite = sprite;
        AssetId = assetId ?? string.Empty;
        Tint = tint;
        UseCategoryTint = useCategoryTint;
    }
}

public static class ExternalJsonImportSpriteResolver
{
    public static ExternalJsonResolvedSprite Resolve(
        ExternalJsonObjectSourceProfile source,
        string categoryId,
        Color categoryTint)
    {
        if (source != null
            && source.spriteMode == ExternalJsonSpriteMode.Custom
            && !string.IsNullOrWhiteSpace(source.spriteAssetId))
        {
            Sprite customSprite = AssetRuntimeLoader.LoadSpriteByAssetID(source.spriteAssetId.Trim());
            if (customSprite != null)
            {
                return new ExternalJsonResolvedSprite(
                    customSprite,
                    source.spriteAssetId.Trim(),
                    Color.white,
                    useCategoryTint: false);
            }
        }

        ExternalJsonShapeKind shape = source?.shape ?? ExternalJsonShapeKind.RectObject;
        ExternalJsonFallbackShape placeholderShape = ExternalJsonFallbackSpriteLibrary.GetDefaultShapeForKind(shape);

        Sprite placeholderSprite = ExternalJsonFallbackSpriteLibrary.GetSprite(placeholderShape);
        if (placeholderSprite == null && ObjectLibraryManager.Instance != null)
            placeholderSprite = ObjectLibraryManager.Instance.DefaultSprite;

        return new ExternalJsonResolvedSprite(
            placeholderSprite,
            string.Empty,
            categoryTint,
            useCategoryTint: true);
    }
}
