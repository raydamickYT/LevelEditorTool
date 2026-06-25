using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Imports levels from pgattic/platformer JSON (boxes, lava, keys, start, end, portals).
/// </summary>
public sealed class PlatformerLevelJsonImporter : IExternalLevelJsonImporter
{
    const string Format = "pgattic.platformer";
    const float KeyMarkerSizePixels = 30f;
    const float StartMarkerSizePixels = 25f;
    const float GoalRadiusPixels = 15f;

    public string FormatId => Format;
    public string DisplayName => "Platformer (pgattic)";

    public bool CanImport(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JObject root = JObject.Parse(json);
            if (root["boxes"] is JArray)
                return true;

            return root["start"] != null && root["end"] != null;
        }
        catch
        {
            return false;
        }
    }

    public ExternalLevelImportResult Import(string json, string sourcePath)
    {
        var result = new ExternalLevelImportResult
        {
            FormatId = FormatId,
            FormatDisplayName = DisplayName,
            SourcePath = sourcePath,
        };

        try
        {
            JObject root = JObject.Parse(json);
            int spawned = 0;

            spawned += ImportBoxes(root);
            spawned += ImportLava(root);
            spawned += ImportKeys(root);
            spawned += ImportStart(root);
            spawned += ImportEnd(root);
            spawned += ImportPortals(root);

            if (root["text"] != null && root["text"].Type != JTokenType.Null)
                result.Warnings.Add("Level hint text is preserved in source JSON but not shown in the editor yet.");

            result.SpawnedObjectCount = spawned;
            result.Success = spawned > 0;

            if (!result.Success)
                result.ErrorMessage = "JSON was recognized as a platformer level, but no spawnable objects were found.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = "Failed to parse platformer JSON: " + ex.Message;
        }

        return result;
    }

    int ImportBoxes(JObject root)
    {
        if (root["boxes"] is not JArray boxes)
            return 0;

        int spawned = 0;
        float platformHeight = ExternalLevelJsonImportService.PlatformerPlatformHeightPixels;

        for (int i = 0; i < boxes.Count; i++)
        {
            if (!TryReadRect(boxes[i], out float x, out float y, out float width, out _))
            {
                if (!TryReadPoint(boxes[i], out x, out y))
                    continue;

                width = 25f;
            }

            GameObject go = ExternalLevelJsonImportService.SpawnImportedObject(
                $"Platform_{i + 1}",
                new Vector2(x, y),
                new Vector2(Mathf.Max(1f, width), platformHeight),
                new Color(0.15f, 0.15f, 0.18f, 1f),
                FormatId,
                "boxes",
                i,
                boxes[i]?.ToString());

            if (go != null)
                spawned++;
        }

        return spawned;
    }

    int ImportLava(JObject root)
    {
        if (root["lava"] is not JArray lava)
            return 0;

        int spawned = 0;
        for (int i = 0; i < lava.Count; i++)
        {
            if (!TryReadRect(lava[i], out float x, out float y, out float width, out float height))
                continue;

            GameObject go = ExternalLevelJsonImportService.SpawnImportedObject(
                $"Lava_{i + 1}",
                new Vector2(x, y),
                new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height)),
                new Color(0.85f, 0.15f, 0.1f, 1f),
                FormatId,
                "lava",
                i,
                lava[i]?.ToString());

            if (go != null)
                spawned++;
        }

        return spawned;
    }

    int ImportKeys(JObject root)
    {
        if (root["keys"] is not JArray keys)
            return 0;

        int spawned = 0;
        float marker = KeyMarkerSizePixels;

        for (int i = 0; i < keys.Count; i++)
        {
            if (!TryReadPoint(keys[i], out float x, out float y))
                continue;

            GameObject go = ExternalLevelJsonImportService.SpawnImportedObject(
                $"Key_{i + 1}",
                new Vector2(x - marker * 0.5f, y - marker * 0.5f),
                new Vector2(marker, marker),
                new Color(0.95f, 0.82f, 0.2f, 1f),
                FormatId,
                "keys",
                i,
                keys[i]?.ToString());

            if (go != null)
                spawned++;
        }

        return spawned;
    }

    int ImportStart(JObject root)
    {
        if (!TryReadPoint(root["start"], out float x, out float y))
            return 0;

        float marker = StartMarkerSizePixels;
        GameObject go = ExternalLevelJsonImportService.SpawnImportedObject(
            "PlayerStart",
            new Vector2(x, y),
            new Vector2(marker, marker * 2f),
            new Color(0.55f, 0.55f, 0.55f, 1f),
            FormatId,
            "start",
            0,
            root["start"]?.ToString());

        return go != null ? 1 : 0;
    }

    int ImportEnd(JObject root)
    {
        if (!TryReadPoint(root["end"], out float x, out float y))
            return 0;

        float diameter = GoalRadiusPixels * 2f;
        GameObject go = ExternalLevelJsonImportService.SpawnImportedObject(
            "Goal",
            new Vector2(x - GoalRadiusPixels, y - GoalRadiusPixels),
            new Vector2(diameter, diameter),
            new Color(0.1f, 0.75f, 0.25f, 1f),
            FormatId,
            "end",
            0,
            root["end"]?.ToString());

        return go != null ? 1 : 0;
    }

    int ImportPortals(JObject root)
    {
        if (root["portals"] is not JArray portals)
            return 0;

        int spawned = 0;
        float marker = KeyMarkerSizePixels;

        for (int i = 0; i < portals.Count; i++)
        {
            if (portals[i] is not JArray pair || pair.Count < 2)
                continue;

            if (TryReadPoint(pair[0], out float x, out float y))
            {
                GameObject entry = ExternalLevelJsonImportService.SpawnImportedObject(
                    $"Portal_{i + 1}_A",
                    new Vector2(x - marker * 0.5f, y - marker * 0.5f),
                    new Vector2(marker, marker),
                    new Color(0.55f, 0.2f, 0.85f, 1f),
                    FormatId,
                    "portals",
                    i,
                    pair.ToString());

                if (entry != null)
                    spawned++;
            }

            if (TryReadPoint(pair[1], out x, out y))
            {
                GameObject exit = ExternalLevelJsonImportService.SpawnImportedObject(
                    $"Portal_{i + 1}_B",
                    new Vector2(x - marker * 0.5f, y - marker * 0.5f),
                    new Vector2(marker, marker),
                    new Color(0.75f, 0.35f, 0.95f, 1f),
                    FormatId,
                    "portals",
                    i,
                    pair.ToString());

                if (exit != null)
                    spawned++;
            }
        }

        return spawned;
    }

    static bool TryReadPoint(JToken token, out float x, out float y)
    {
        x = 0f;
        y = 0f;
        if (token is not JArray array || array.Count < 2)
            return false;

        if (!TryToFloat(array[0], out x) || !TryToFloat(array[1], out y))
            return false;

        return !(float.IsNaN(x) || float.IsNaN(y));
    }

    static bool TryReadRect(JToken token, out float x, out float y, out float width, out float height)
    {
        x = y = width = height = 0f;
        if (token is not JArray array || array.Count < 2)
            return false;

        if (!TryToFloat(array[0], out x) || !TryToFloat(array[1], out y))
            return false;

        width = array.Count > 2 && TryToFloat(array[2], out float w) ? w : 25f;
        height = array.Count > 3 && TryToFloat(array[3], out float h) ? h : ExternalLevelJsonImportService.PlatformerPlatformHeightPixels;
        return true;
    }

    static bool TryToFloat(JToken token, out float value)
    {
        value = 0f;
        if (token == null || token.Type == JTokenType.Null)
            return false;

        try
        {
            value = token.Value<float>();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
