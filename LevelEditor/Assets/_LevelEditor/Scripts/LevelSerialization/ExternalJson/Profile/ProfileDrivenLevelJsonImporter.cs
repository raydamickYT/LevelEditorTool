using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class ProfileDrivenLevelJsonImporter
{
    public const string ProfileFormatPrefix = "profile.";

    public static ExternalLevelImportResult Import(
        ExternalJsonImportProfile profile,
        string json,
        string sourcePath)
    {
        var result = new ExternalLevelImportResult
        {
            FormatId = profile?.formatId ?? "profile.custom",
            FormatDisplayName = profile?.displayName ?? "Mapped JSON",
            SourcePath = sourcePath,
        };

        if (profile == null || profile.objectSources == null || profile.objectSources.Length == 0)
        {
            result.Success = false;
            result.ErrorMessage = "Import profile has no object sources configured.";
            return result;
        }

        try
        {
            JToken root = JToken.Parse(json);
            int spawned = 0;
            float pixelScale = Mathf.Max(0.0001f, profile.pixelScale);

            foreach (ExternalJsonObjectSourceProfile source in profile.objectSources)
            {
                if (source == null || !source.enabled)
                    continue;

                if (!JsonPathResolver.IsRootPath(source.jsonPath) && string.IsNullOrWhiteSpace(source.jsonPath))
                    continue;

                spawned += ImportSource(root, profile, source, pixelScale, result);
            }

            ApplyViewportFromProfile(root, profile);

            result.SpawnedObjectCount = spawned;
            result.Success = spawned > 0;

            if (!result.Success)
                result.ErrorMessage = "Mapped JSON was parsed, but no spawnable objects were found.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = "Failed to import mapped JSON: " + ex.Message;
        }

        return result;
    }

    static int ImportSource(
        JToken root,
        ExternalJsonImportProfile profile,
        ExternalJsonObjectSourceProfile source,
        float pixelScale,
        ExternalLevelImportResult result)
    {
        if (!JsonPathResolver.TryResolve(root, source.jsonPath, out JToken container))
        {
            result.Warnings.Add($"Path not found: {source.jsonPath}");
            return 0;
        }

        int spawned = 0;

        if (source.isArray)
        {
            if (container is not JArray array)
            {
                result.Warnings.Add($"Expected array at {source.jsonPath}");
                return 0;
            }

            for (int i = 0; i < array.Count; i++)
            {
                if (!MatchesDiscriminator(array[i], source))
                    continue;

                spawned += TrySpawnToken(array[i], profile, source, pixelScale, i);
            }

            return spawned;
        }

        if (!MatchesDiscriminator(container, source))
            return 0;

        return TrySpawnToken(container, profile, source, pixelScale, 0);
    }

    static bool MatchesDiscriminator(JToken token, ExternalJsonObjectSourceProfile source)
    {
        if (string.IsNullOrWhiteSpace(source.discriminatorField)
            || string.IsNullOrWhiteSpace(source.discriminatorValue))
        {
            return true;
        }

        if (token is not JObject obj)
            return false;

        if (!obj.TryGetValue(source.discriminatorField, StringComparison.OrdinalIgnoreCase, out JToken valueToken))
            return false;

        string value = valueToken.Type == JTokenType.String
            ? valueToken.Value<string>()
            : valueToken.ToString();

        return string.Equals(value, source.discriminatorValue, StringComparison.OrdinalIgnoreCase);
    }

    static int TrySpawnToken(
        JToken token,
        ExternalJsonImportProfile profile,
        ExternalJsonObjectSourceProfile source,
        float pixelScale,
        int sourceIndex)
    {
        if (!TryReadShape(token, source, out Vector2 position, out Vector2 size))
            return 0;

        string objectName = $"{source.displayName}_{sourceIndex + 1}";
        Color tint = CategoryColorUtil.GetColorForCategory(source.id);

        GameObject go = ExternalLevelJsonImportService.SpawnImportedObject(
            objectName,
            position,
            size,
            tint,
            profile.formatId,
            source.id,
            sourceIndex,
            token.ToString(Newtonsoft.Json.Formatting.None),
            source,
            pixelScale);

        return go != null ? 1 : 0;
    }

    static bool TryReadShape(
        JToken token,
        ExternalJsonObjectSourceProfile source,
        out Vector2 position,
        out Vector2 size)
    {
        position = Vector2.zero;
        size = new Vector2(Mathf.Max(1f, source.defaultWidth), Mathf.Max(1f, source.defaultHeight));

        switch (source.shape)
        {
            case ExternalJsonShapeKind.PointObject:
                return TryReadObjectPoint(token as JObject, source, out position, out size);

            case ExternalJsonShapeKind.RectObject:
                return TryReadObjectRect(token as JObject, source, out position, out size);

            case ExternalJsonShapeKind.PointArray:
                return TryReadArrayPoint(token as JArray, out position, out size, source);

            case ExternalJsonShapeKind.RectArray3:
                return TryReadArrayRect3(token as JArray, out position, out size, source);

            case ExternalJsonShapeKind.RectArray4:
                return TryReadArrayRect4(token as JArray, out position, out size);

            default:
                return false;
        }
    }

    static bool TryReadObjectPoint(JObject obj, ExternalJsonObjectSourceProfile source, out Vector2 position, out Vector2 size)
    {
        position = Vector2.zero;
        size = new Vector2(Mathf.Max(1f, source.defaultWidth), Mathf.Max(1f, source.defaultHeight));

        if (obj == null)
            return false;

        if (!TryReadObjectField(obj, source.xField, out float x) || !TryReadObjectField(obj, source.yField, out float y))
            return false;

        position = new Vector2(x, y);
        return true;
    }

    static bool TryReadObjectRect(JObject obj, ExternalJsonObjectSourceProfile source, out Vector2 position, out Vector2 size)
    {
        if (!TryReadObjectPoint(obj, source, out position, out size))
            return false;

        float width = source.defaultWidth;
        float height = source.defaultHeight;

        if (TryReadObjectField(obj, source.widthField, out float w) || TryReadObjectField(obj, "w", out w))
            width = w;

        if (TryReadObjectField(obj, source.heightField, out float h) || TryReadObjectField(obj, "h", out h))
            height = h;

        size = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
        return true;
    }

    static bool TryReadArrayPoint(JArray array, out Vector2 position, out Vector2 size, ExternalJsonObjectSourceProfile source)
    {
        position = Vector2.zero;
        size = new Vector2(Mathf.Max(1f, source.defaultWidth), Mathf.Max(1f, source.defaultHeight));

        if (array == null || array.Count < 2)
            return false;

        position = new Vector2(ReadNumber(array[0]), ReadNumber(array[1]));
        return true;
    }

    static bool TryReadArrayRect3(JArray array, out Vector2 position, out Vector2 size, ExternalJsonObjectSourceProfile source)
    {
        position = Vector2.zero;
        size = new Vector2(Mathf.Max(1f, source.defaultWidth), Mathf.Max(1f, source.defaultHeight));

        if (array == null || array.Count < 3)
            return false;

        position = new Vector2(ReadNumber(array[0]), ReadNumber(array[1]));
        size = new Vector2(Mathf.Max(1f, ReadNumber(array[2])), Mathf.Max(1f, source.defaultHeight));
        return true;
    }

    static bool TryReadArrayRect4(JArray array, out Vector2 position, out Vector2 size)
    {
        position = Vector2.zero;
        size = Vector2.one;

        if (array == null || array.Count < 4)
            return false;

        position = new Vector2(ReadNumber(array[0]), ReadNumber(array[1]));
        size = new Vector2(Mathf.Max(1f, ReadNumber(array[2])), Mathf.Max(1f, ReadNumber(array[3])));
        return true;
    }

    static bool TryReadObjectField(JObject obj, string fieldName, out float value)
    {
        value = 0f;
        if (obj == null || string.IsNullOrWhiteSpace(fieldName))
            return false;

        if (!obj.TryGetValue(fieldName, StringComparison.OrdinalIgnoreCase, out JToken token))
            return false;

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            value = token.Value<float>();
            return true;
        }

        return false;
    }

    static float ReadNumber(JToken token)
    {
        if (token == null)
            return 0f;

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            return token.Value<float>();

        return 0f;
    }

    static void ApplyViewportFromProfile(JToken root, ExternalJsonImportProfile profile)
    {
        float width = 0f;
        float height = 0f;
        bool hasWidth = !string.IsNullOrWhiteSpace(profile.viewportWidthPath)
            && JsonPathResolver.TryResolveScalar(root, profile.viewportWidthPath, out width);

        bool hasHeight = !string.IsNullOrWhiteSpace(profile.viewportHeightPath)
            && JsonPathResolver.TryResolveScalar(root, profile.viewportHeightPath, out height);

        if (!hasWidth && !hasHeight)
            return;

        LevelViewportFrameState state = LevelViewportFrameState.Instance;
        state.Enabled = true;
        state.PixelX = 0f;
        state.PixelY = 0f;
        state.PixelScale = Mathf.Max(0.0001f, profile.pixelScale);

        if (hasWidth)
            state.PixelWidth = Mathf.Max(1f, width);

        if (hasHeight)
            state.PixelHeight = Mathf.Max(1f, height);
    }
}

static class CategoryColorUtil
{
    static readonly Color[] Palette =
    {
        new(0.85f, 0.25f, 0.25f, 1f),
        new(0.25f, 0.65f, 0.95f, 1f),
        new(0.35f, 0.8f, 0.45f, 1f),
        new(0.95f, 0.75f, 0.2f, 1f),
        new(0.7f, 0.4f, 0.9f, 1f),
        new(0.95f, 0.5f, 0.2f, 1f),
    };

    public static Color GetColorForCategory(string category)
    {
        if (string.IsNullOrEmpty(category))
            return Color.white;

        int hash = Mathf.Abs(category.GetHashCode());
        return Palette[hash % Palette.Length];
    }
}
