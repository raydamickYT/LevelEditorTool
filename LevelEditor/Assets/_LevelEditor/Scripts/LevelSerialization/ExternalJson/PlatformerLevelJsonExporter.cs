using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Exports the current scene to pgattic/platformer level JSON.
/// </summary>
public sealed class PlatformerLevelJsonExporter : IExternalLevelJsonExporter
{
    const float PlatformHeightPixels = 10f;

    public string FormatId => PlatformerObjectCategoryResolver.FormatId;
    public string DisplayName => "Platformer (pgattic)";

    public bool CanExport()
    {
        if (ExternalLevelJsonImportSession.HasActiveImport
            && string.Equals(ExternalLevelJsonImportSession.FormatId, FormatId, StringComparison.Ordinal))
        {
            return true;
        }

        return CollectExportableObjects().Count > 0;
    }

    public ExternalLevelExportResult Export()
    {
        var result = new ExternalLevelExportResult
        {
            FormatId = FormatId,
            FormatDisplayName = DisplayName,
        };

        try
        {
            List<LevelObject> objects = CollectExportableObjects();
            if (objects.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No platformer objects found to export. Use platform/lava/key/goal/start/portal sprites or import a platformer JSON first.";
                return result;
            }

            JObject root = BuildRootObject(objects, result);
            result.Json = root.ToString(Formatting.Indented);
            result.ExportedObjectCount = objects.Count;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = "Failed to export platformer JSON: " + ex.Message;
        }

        return result;
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

            if (PlatformerObjectCategoryResolver.TryResolveCategory(levelObject, out _))
                exportable.Add(levelObject);
        }

        return exportable;
    }

    static JObject BuildRootObject(List<LevelObject> objects, ExternalLevelExportResult result)
    {
        var boxes = new JArray();
        var lava = new JArray();
        var keys = new JArray();
        var portalPairs = new SortedDictionary<int, (JArray pointA, JArray pointB, bool hasA, bool hasB)>();
        var ungroupedPortalPoints = new List<JArray>();

        JArray start = null;
        JArray end = null;
        int skipped = 0;

        foreach (LevelObject levelObject in objects.OrderBy(lo => lo.transform.GetSiblingIndex()))
        {
            if (!PlatformerObjectCategoryResolver.TryResolveCategory(levelObject, out string category))
            {
                skipped++;
                continue;
            }

            GameObject go = levelObject.gameObject;
            if (!ExternalJsonCoordinateUtil.TryGetWorldSpriteBounds(go, out Bounds bounds))
            {
                skipped++;
                continue;
            }

            ExternalJsonCoordinateUtil.WorldBoundsToPixelRect(
                bounds,
                ExternalJsonCoordinateUtil.DefaultPixelScale,
                out float x,
                out float y,
                out float width,
                out float height);

            Vector2 center = ExternalJsonCoordinateUtil.WorldCenterToPixelPoint(bounds.center);

            switch (category)
            {
                case "boxes":
                    boxes.Add(new JArray(
                        ExternalJsonCoordinateUtil.RoundPixel(x),
                        ExternalJsonCoordinateUtil.RoundPixel(y),
                        Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(width))));
                    break;

                case "lava":
                    lava.Add(new JArray(
                        ExternalJsonCoordinateUtil.RoundPixel(x),
                        ExternalJsonCoordinateUtil.RoundPixel(y),
                        Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(width)),
                        Mathf.Max(1, ExternalJsonCoordinateUtil.RoundPixel(height))));
                    break;

                case "keys":
                    keys.Add(new JArray(
                        ExternalJsonCoordinateUtil.RoundPixel(center.x),
                        ExternalJsonCoordinateUtil.RoundPixel(center.y)));
                    break;

                case "start":
                    if (start == null)
                    {
                        start = new JArray(
                            ExternalJsonCoordinateUtil.RoundPixel(x),
                            ExternalJsonCoordinateUtil.RoundPixel(y));
                    }
                    else
                    {
                        result.Warnings.Add("Multiple start markers found; only the first was exported.");
                    }

                    break;

                case "end":
                    if (end == null)
                    {
                        end = new JArray(
                            ExternalJsonCoordinateUtil.RoundPixel(center.x),
                            ExternalJsonCoordinateUtil.RoundPixel(center.y));
                    }
                    else
                    {
                        result.Warnings.Add("Multiple goal markers found; only the first was exported.");
                    }

                    break;

                case "portals":
                {
                    var point = new JArray(
                        ExternalJsonCoordinateUtil.RoundPixel(center.x),
                        ExternalJsonCoordinateUtil.RoundPixel(center.y));

                    if (TryAssignPortalPoint(go, point, portalPairs))
                        break;

                    ungroupedPortalPoints.Add(point);
                    break;
                }

                default:
                    skipped++;
                    break;
            }
        }

        if (skipped > 0)
            result.Warnings.Add($"{skipped} scene object(s) could not be mapped to platformer JSON fields.");

        var portals = new JArray();
        foreach (KeyValuePair<int, (JArray pointA, JArray pointB, bool hasA, bool hasB)> pair in portalPairs)
        {
            if (!pair.Value.hasA || !pair.Value.hasB)
            {
                result.Warnings.Add($"Portal {pair.Key + 1} is incomplete; both endpoints are required.");
                continue;
            }

            portals.Add(new JArray(pair.Value.pointA, pair.Value.pointB));
        }

        for (int i = 0; i < ungroupedPortalPoints.Count; i += 2)
        {
            if (i + 1 >= ungroupedPortalPoints.Count)
            {
                result.Warnings.Add("A portal marker has no pair; add a second portal sprite to complete it.");
                break;
            }

            portals.Add(new JArray(ungroupedPortalPoints[i], ungroupedPortalPoints[i + 1]));
        }

        var root = new JObject
        {
            ["version"] = 1,
            ["boxes"] = boxes,
            ["lava"] = lava,
            ["keys"] = keys,
            ["portals"] = portals,
            ["text"] = ResolveLevelText(),
        };

        if (start != null)
            root["start"] = start;
        else
            root["start"] = new JArray();

        if (end != null)
            root["end"] = end;
        else
            root["end"] = new JArray();

        return root;
    }

    static bool TryAssignPortalPoint(
        GameObject go,
        JArray point,
        SortedDictionary<int, (JArray pointA, JArray pointB, bool hasA, bool hasB)> portalPairs)
    {
        int portalIndex = 0;
        bool isPointB = false;

        if (PlatformerObjectCategoryResolver.TryParsePortalSuffix(go.name, out portalIndex, out isPointB))
        {
            portalIndex = Mathf.Max(0, portalIndex - 1);
            AssignPortalPairPoint(portalPairs, portalIndex, point, isPointB);
            return true;
        }

        if (go.TryGetComponent(out ExternalJsonObjectBinding binding) && binding.SourceIndex >= 0)
        {
            bool pointB = go.name.EndsWith("_B", StringComparison.OrdinalIgnoreCase);
            AssignPortalPairPoint(portalPairs, binding.SourceIndex, point, pointB);
            return true;
        }

        return false;
    }

    static void AssignPortalPairPoint(
        SortedDictionary<int, (JArray pointA, JArray pointB, bool hasA, bool hasB)> portalPairs,
        int portalIndex,
        JArray point,
        bool isPointB)
    {
        if (!portalPairs.TryGetValue(portalIndex, out var pair))
            pair = (null, null, false, false);

        if (isPointB)
        {
            pair.pointB = point;
            pair.hasB = true;
        }
        else if (!pair.hasA)
        {
            pair.pointA = point;
            pair.hasA = true;
        }
        else
        {
            pair.pointB = point;
            pair.hasB = true;
        }

        portalPairs[portalIndex] = pair;
    }

    static string ResolveLevelText()
    {
        if (!ExternalLevelJsonImportSession.HasActiveImport
            || !string.Equals(ExternalLevelJsonImportSession.FormatId, PlatformerObjectCategoryResolver.FormatId, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        try
        {
            JObject source = JObject.Parse(ExternalLevelJsonImportSession.SourceJsonText);
            return source["text"]?.Type == JTokenType.String ? source["text"].Value<string>() ?? string.Empty : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
