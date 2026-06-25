using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class ExternalLevelJsonImportService
{
    const float DefaultPixelScale = 0.01f;
    const float PlatformerBoxHeightPixels = 10f;

    static readonly IExternalLevelJsonImporter[] Importers =
    {
        new PlatformerLevelJsonImporter(),
    };

    public static IReadOnlyList<IExternalLevelJsonImporter> RegisteredImporters => Importers;

    public static ExternalLevelImportResult ImportFromFile(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath) || !File.Exists(jsonFilePath))
        {
            return new ExternalLevelImportResult
            {
                Success = false,
                ErrorMessage = "JSON file not found.",
                SourcePath = jsonFilePath,
            };
        }

        string json;
        try
        {
            json = File.ReadAllText(jsonFilePath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return new ExternalLevelImportResult
            {
                Success = false,
                ErrorMessage = "Could not read JSON file: " + ex.Message,
                SourcePath = jsonFilePath,
            };
        }

        return ImportFromText(json, jsonFilePath);
    }

    public static ExternalLevelImportResult ImportFromText(string json, string sourcePath = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ExternalLevelImportResult
            {
                Success = false,
                ErrorMessage = "JSON is empty.",
                SourcePath = sourcePath,
            };
        }

        IExternalLevelJsonImporter importer = Importers.FirstOrDefault(i => i.CanImport(json));
        if (importer == null)
        {
            return new ExternalLevelImportResult
            {
                Success = false,
                ErrorMessage = "No registered importer recognizes this JSON structure.",
                SourcePath = sourcePath,
            };
        }

        PrepareSceneForImport();

        ExternalLevelImportResult result = importer.Import(json, sourcePath);
        if (result == null)
        {
            return new ExternalLevelImportResult
            {
                Success = false,
                ErrorMessage = "Importer returned no result.",
                SourcePath = sourcePath,
            };
        }

        if (result.Success)
        {
            ExternalLevelJsonImportSession.Set(sourcePath, json, result.FormatId, result.FormatDisplayName);
            LevelProjectDirtyState.MarkDirty();
            EventManager.Instance?.TriggerDelegate(ObjectHierarchyEvents.RebuildEntireHierarchy);
            EventManager.Instance?.TriggerDelegate(
                SelectionEvents.ReplaceSelectionWithObject,
                Enumerable.Empty<GameObject>());
        }

        return result;
    }

    public static GameObject SpawnImportedObject(
        string objectName,
        Vector2 pixelPosition,
        Vector2 pixelSize,
        Color tint,
        string formatId,
        string category,
        int sourceIndex,
        string sourceJsonFragment,
        float pixelScale = DefaultPixelScale)
    {
        GameObject template = ObjectLibraryManager.Instance != null
            ? ObjectLibraryManager.Instance.SpawnPrefabTemplate
            : null;

        if (template == null)
        {
            Debug.LogError("External JSON import: ObjectLibraryManager.SpawnPrefabTemplate is missing.");
            return null;
        }

        Sprite sprite = ResolvePlaceholderSprite();
        Vector3 worldCenter = PixelRectToWorldCenter(pixelPosition, pixelSize, pixelScale);
        Vector3 scale = ComputeScaleForPixelSize(pixelSize, sprite, pixelScale);

        LevelObject.Memento memento = new LevelObject.Memento(
            worldCenter,
            Quaternion.identity,
            scale,
            template,
            string.Empty,
            sprite,
            null,
            objectName,
            hasCollision: true);

        GameObject spawned = LevelObjectSpawner.Spawn(memento, preserveObjectID: false);
        if (spawned == null)
            return null;

        if (spawned.TryGetComponent(out SpriteRenderer spriteRenderer))
            spriteRenderer.color = tint;

        ExternalJsonObjectBinding binding = spawned.GetComponent<ExternalJsonObjectBinding>();
        if (binding == null)
            binding = spawned.AddComponent<ExternalJsonObjectBinding>();

        binding.SourceFormatId = formatId;
        binding.SourceCategory = category;
        binding.SourceIndex = sourceIndex;
        binding.SourceJsonFragment = sourceJsonFragment ?? string.Empty;

        return spawned;
    }

    public static Vector3 PixelRectToWorldCenter(Vector2 pixelPosition, Vector2 pixelSize, float pixelScale)
    {
        float centerX = (pixelPosition.x + pixelSize.x * 0.5f) * pixelScale;
        float centerY = -((pixelPosition.y + pixelSize.y * 0.5f) * pixelScale);
        return new Vector3(centerX, centerY, 0f);
    }

    public static Vector3 ComputeScaleForPixelSize(Vector2 pixelSize, Sprite sprite, float pixelScale)
    {
        if (sprite == null)
            return Vector3.one;

        Vector2 spriteSize = sprite.bounds.size;
        float safeWidth = Mathf.Max(0.0001f, spriteSize.x);
        float safeHeight = Mathf.Max(0.0001f, spriteSize.y);

        return new Vector3(
            Mathf.Max(0.01f, (pixelSize.x * pixelScale) / safeWidth),
            Mathf.Max(0.01f, (pixelSize.y * pixelScale) / safeHeight),
            1f);
    }

    public static float PlatformerPlatformHeightPixels => PlatformerBoxHeightPixels;

    static void PrepareSceneForImport()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerDelegate(
                SelectionEvents.ReplaceSelectionWithObject,
                Enumerable.Empty<GameObject>());
        }

        if (LevelObjectsRoot.Instance != null)
            LevelObjectsRoot.Instance.DestroyAllRootLevelObjects();

        ObjectRegistry.ClearAllForNewLevel();
        LevelProjectSession.ClearProject();
        ExternalLevelJsonImportSession.Clear();
    }

    static Sprite ResolvePlaceholderSprite()
    {
        if (ObjectLibraryManager.Instance != null && ObjectLibraryManager.Instance.DefaultSprite != null)
            return ObjectLibraryManager.Instance.DefaultSprite;

        Texture2D texture = new Texture2D(2, 2);
        Color[] pixels = { Color.white, Color.white, Color.white, Color.white };
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
    }
}
