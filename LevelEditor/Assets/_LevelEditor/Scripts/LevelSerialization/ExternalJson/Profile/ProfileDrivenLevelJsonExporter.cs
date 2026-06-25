using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public sealed class ProfileDrivenLevelJsonExporter : IExternalLevelJsonExporter
{
    public string FormatId => ExternalLevelJsonImportSession.ActiveProfile?.formatId ?? "profile.custom";
    public string DisplayName => ExternalLevelJsonImportSession.ActiveProfile?.displayName ?? "Mapped JSON";

    public bool CanExport()
    {
        return ExternalLevelJsonImportSession.HasActiveProfile
            && CollectExportableObjects().Count > 0;
    }

    public ExternalLevelExportResult Export()
    {
        var result = new ExternalLevelExportResult
        {
            FormatId = FormatId,
            FormatDisplayName = DisplayName,
        };

        ExternalJsonImportProfile profile = ExternalLevelJsonImportSession.ActiveProfile;
        if (profile == null)
        {
            result.Success = false;
            result.ErrorMessage = "No active JSON mapping profile.";
            return result;
        }

        try
        {
            JToken root = ParseExportRoot(profile);
            List<LevelObject> objects = CollectExportableObjects();
            if (objects.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No mapped objects found to export.";
                return result;
            }

            int exported = 0;
            int skipped = 0;

            IEnumerable<IGrouping<string, ExternalJsonObjectSourceProfile>> pathGroups = profile.objectSources
                .Where(source => source != null && source.enabled)
                .GroupBy(source => JsonPathResolver.NormalizePath(source.jsonPath));

            foreach (IGrouping<string, ExternalJsonObjectSourceProfile> group in pathGroups)
            {
                if (ShouldMergeArrayExport(group))
                    exported += ExportMergedArray(root, profile, group, objects, ref skipped, result);
                else
                {
                    foreach (ExternalJsonObjectSourceProfile source in group)
                        exported += ExportSource(root, profile, source, objects, ref skipped, result);
                }
            }

            result.Json = root.ToString(Formatting.Indented);
            result.ExportedObjectCount = exported;
            result.Success = exported > 0;

            if (skipped > 0)
                result.Warnings.Add($"Skipped {skipped} objects that could not be mapped back.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = "Failed to export mapped JSON: " + ex.Message;
        }

        return result;
    }

    static JToken ParseExportRoot(ExternalJsonImportProfile profile)
    {
        if (!string.IsNullOrEmpty(ExternalLevelJsonImportSession.SourceJsonText))
            return JToken.Parse(ExternalLevelJsonImportSession.SourceJsonText);

        bool usesRootArray = profile.objectSources != null
            && profile.objectSources.Any(source =>
                source != null
                && source.enabled
                && source.isArray
                && JsonPathResolver.IsRootPath(source.jsonPath));

        return usesRootArray ? new JArray() : new JObject();
    }

    static bool ShouldMergeArrayExport(IGrouping<string, ExternalJsonObjectSourceProfile> group)
    {
        if (group.Count() <= 1)
            return false;

        ExternalJsonObjectSourceProfile first = group.First();
        if (!first.isArray)
            return false;

        return group.Any(source => !string.IsNullOrWhiteSpace(source.discriminatorField));
    }

    static int ExportMergedArray(
        JToken root,
        ExternalJsonImportProfile profile,
        IEnumerable<ExternalJsonObjectSourceProfile> sources,
        List<LevelObject> objects,
        ref int skipped,
        ExternalLevelExportResult result)
    {
        ExternalJsonObjectSourceProfile first = sources.First();
        string path = JsonPathResolver.NormalizePath(first.jsonPath);

        List<LevelObject> matchedObjects = objects
            .Where(levelObject => sources.Any(source => MatchesSource(levelObject, source)))
            .OrderBy(levelObject => levelObject.transform.GetSiblingIndex())
            .ToList();

        if (matchedObjects.Count == 0)
            return 0;

        JsonPathResolver.EnsureArray(root, path, out JArray array);
        if (array == null)
            return 0;

        array.Clear();

        int exported = 0;

        foreach (LevelObject levelObject in matchedObjects)
        {
            ExternalJsonObjectSourceProfile source = sources.FirstOrDefault(s => MatchesSource(levelObject, s));
            if (source == null)
            {
                skipped++;
                continue;
            }

            if (!TryBuildToken(levelObject, profile, source, out JToken token))
            {
                skipped++;
                continue;
            }

            array.Add(token);
            exported++;
        }

        return exported;
    }

    static int ExportSource(
        JToken root,
        ExternalJsonImportProfile profile,
        ExternalJsonObjectSourceProfile source,
        List<LevelObject> objects,
        ref int skipped,
        ExternalLevelExportResult result)
    {
        List<LevelObject> sourceObjects = objects
            .Where(levelObject => MatchesSource(levelObject, source))
            .OrderBy(levelObject => levelObject.transform.GetSiblingIndex())
            .ToList();

        if (sourceObjects.Count == 0)
            return 0;

        if (source.isArray)
        {
            JsonPathResolver.EnsureArray(root, source.jsonPath, out JArray array);
            if (array == null)
                return 0;

            array.Clear();

            int exported = 0;
            foreach (LevelObject levelObject in sourceObjects)
            {
                if (!TryBuildToken(levelObject, profile, source, out JToken token))
                {
                    skipped++;
                    continue;
                }

                array.Add(token);
                exported++;
            }

            JsonPathResolver.SetToken(root, source.jsonPath, array);
            return exported;
        }

        if (sourceObjects.Count > 1)
            result.Warnings.Add($"Multiple objects mapped to single path '{source.jsonPath}'. Only the first was exported.");

        LevelObject first = sourceObjects[0];
        if (!TryBuildToken(first, profile, source, out JToken singleToken))
        {
            skipped++;
            return 0;
        }

        JsonPathResolver.SetToken(root, source.jsonPath, singleToken);
        return 1;
    }

    static bool MatchesSource(LevelObject levelObject, ExternalJsonObjectSourceProfile source)
    {
        if (levelObject == null || source == null)
            return false;

        if (levelObject.TryGetComponent(out ExternalJsonObjectBinding binding)
            && !string.IsNullOrEmpty(binding.SourceCategory))
        {
            return string.Equals(binding.SourceCategory, source.id, StringComparison.Ordinal)
                || string.Equals(binding.SourceCategory, source.jsonPath, StringComparison.Ordinal);
        }

        return levelObject.name.StartsWith(source.displayName, StringComparison.OrdinalIgnoreCase)
            || levelObject.name.StartsWith(source.id, StringComparison.OrdinalIgnoreCase);
    }

    static bool TryBuildToken(
        LevelObject levelObject,
        ExternalJsonImportProfile profile,
        ExternalJsonObjectSourceProfile source,
        out JToken token)
    {
        token = null;
        if (!ExternalJsonCoordinateUtil.TryGetWorldSpriteBounds(levelObject.gameObject, out Bounds bounds))
            return false;

        float pixelScale = Mathf.Max(0.0001f, profile.pixelScale);
        ExternalJsonCoordinateUtil.WorldBoundsToPixelRect(bounds, pixelScale, out float x, out float y, out float width, out float height);
        Vector2 center = ExternalJsonCoordinateUtil.WorldCenterToPixelPoint(bounds.center, pixelScale);

        if (levelObject.TryGetComponent(out ExternalJsonObjectBinding binding)
            && !string.IsNullOrEmpty(binding.SourceJsonFragment))
        {
            try
            {
                token = JToken.Parse(binding.SourceJsonFragment);
                UpdateTokenFromShape(token, source, x, y, width, height, center);
                return true;
            }
            catch
            {
                // Fall through to rebuild token.
            }
        }

        token = BuildTokenFromShape(source, x, y, width, height, center);
        return token != null;
    }

    static void UpdateTokenFromShape(
        JToken token,
        ExternalJsonObjectSourceProfile source,
        float x,
        float y,
        float width,
        float height,
        Vector2 center)
    {
        switch (source.shape)
        {
            case ExternalJsonShapeKind.PointObject:
                if (token is JObject pointObj)
                {
                    pointObj[source.xField] = ExternalJsonCoordinateUtil.RoundPixel(center.x);
                    pointObj[source.yField] = ExternalJsonCoordinateUtil.RoundPixel(center.y);
                    ApplyDiscriminator(pointObj, source);
                }
                break;

            case ExternalJsonShapeKind.RectObject:
                if (token is JObject rectObj)
                {
                    rectObj[source.xField] = ExternalJsonCoordinateUtil.RoundPixel(x);
                    rectObj[source.yField] = ExternalJsonCoordinateUtil.RoundPixel(y);
                    rectObj[source.widthField] = Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(width));
                    if (rectObj[source.heightField] != null || source.defaultHeight > 0f)
                        rectObj[source.heightField] = Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(height));
                    ApplyDiscriminator(rectObj, source);
                }
                break;

            case ExternalJsonShapeKind.PointArray:
                if (token is JArray pointArray && pointArray.Count >= 2)
                {
                    pointArray[0] = ExternalJsonCoordinateUtil.RoundPixel(center.x);
                    pointArray[1] = ExternalJsonCoordinateUtil.RoundPixel(center.y);
                }
                break;

            case ExternalJsonShapeKind.RectArray3:
                if (token is JArray rect3 && rect3.Count >= 3)
                {
                    rect3[0] = ExternalJsonCoordinateUtil.RoundPixel(x);
                    rect3[1] = ExternalJsonCoordinateUtil.RoundPixel(y);
                    rect3[2] = Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(width));
                }
                break;

            case ExternalJsonShapeKind.RectArray4:
                if (token is JArray rect4 && rect4.Count >= 4)
                {
                    rect4[0] = ExternalJsonCoordinateUtil.RoundPixel(x);
                    rect4[1] = ExternalJsonCoordinateUtil.RoundPixel(y);
                    rect4[2] = Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(width));
                    rect4[3] = Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(height));
                }
                break;
        }
    }

    static JToken BuildTokenFromShape(
        ExternalJsonObjectSourceProfile source,
        float x,
        float y,
        float width,
        float height,
        Vector2 center)
    {
        JToken token = source.shape switch
        {
            ExternalJsonShapeKind.PointObject => new JObject
            {
                [source.xField] = ExternalJsonCoordinateUtil.RoundPixel(center.x),
                [source.yField] = ExternalJsonCoordinateUtil.RoundPixel(center.y),
            },
            ExternalJsonShapeKind.RectObject => new JObject
            {
                [source.xField] = ExternalJsonCoordinateUtil.RoundPixel(x),
                [source.yField] = ExternalJsonCoordinateUtil.RoundPixel(y),
                [source.widthField] = Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(width)),
                [source.heightField] = Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(height)),
            },
            ExternalJsonShapeKind.PointArray => new JArray(
                ExternalJsonCoordinateUtil.RoundPixel(center.x),
                ExternalJsonCoordinateUtil.RoundPixel(center.y)),
            ExternalJsonShapeKind.RectArray3 => new JArray(
                ExternalJsonCoordinateUtil.RoundPixel(x),
                ExternalJsonCoordinateUtil.RoundPixel(y),
                Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(width))),
            ExternalJsonShapeKind.RectArray4 => new JArray(
                ExternalJsonCoordinateUtil.RoundPixel(x),
                ExternalJsonCoordinateUtil.RoundPixel(y),
                Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(width)),
                Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(height))),
            _ => null,
        };

        if (token is JObject obj)
            ApplyDiscriminator(obj, source);

        return token;
    }

    static void ApplyDiscriminator(JObject obj, ExternalJsonObjectSourceProfile source)
    {
        if (obj == null
            || string.IsNullOrWhiteSpace(source.discriminatorField)
            || string.IsNullOrWhiteSpace(source.discriminatorValue))
        {
            return;
        }

        obj[source.discriminatorField] = source.discriminatorValue;
    }

    static List<LevelObject> CollectExportableObjects()
    {
        var exportable = new List<LevelObject>();

        foreach (GameObject go in ObjectRegistry.objects.Values)
        {
            if (go == null || !go.TryGetComponent(out LevelObject levelObject))
                continue;

            if (levelObject is LevelObjectGroup)
                continue;

            if (!go.TryGetComponent(out ExternalJsonObjectBinding _))
                continue;

            exportable.Add(levelObject);
        }

        return exportable;
    }
}
