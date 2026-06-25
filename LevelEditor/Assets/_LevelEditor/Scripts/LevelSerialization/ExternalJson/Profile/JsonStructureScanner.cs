using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public static class JsonStructureScanner
{
    static readonly HashSet<string> ViewportFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "width", "height", "groundWidth", "mapWidth", "mapHeight", "screenWidth", "screenHeight",
    };

    static readonly HashSet<string> PositionFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "x", "y",
    };

    public static JsonStructureScanResult Scan(string json)
    {
        var result = new JsonStructureScanResult();
        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            JToken root = JToken.Parse(json);
            ScanToken(root, string.Empty, result, depth: 0);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("JSON structure scan failed: " + ex.Message);
        }

        return result;
    }

    static void ScanToken(JToken token, string path, JsonStructureScanResult result, int depth)
    {
        if (token == null || depth > 4)
            return;

        if (token is JObject obj)
        {
            if (!string.IsNullOrEmpty(path) && TryReadPositionObject(obj, out _, out _))
            {
                var single = new JsonSingleObjectCandidate
                {
                    Path = path,
                    InferredShape = InferObjectShape(obj),
                    SampleJson = obj.ToString(Newtonsoft.Json.Formatting.None),
                };
                foreach (JProperty property in obj.Properties())
                    single.FieldNames.Add(property.Name);

                result.SingleObjectCandidates.Add(single);
            }

            foreach (JProperty property in obj.Properties())
            {
                string childPath = string.IsNullOrEmpty(path) ? property.Name : path + "." + property.Name;

                if (property.Value is JValue value
                    && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
                {
                    if (ViewportFieldNames.Contains(property.Name))
                    {
                        result.ScalarFieldCandidates.Add(new JsonScalarFieldCandidate
                        {
                            Path = childPath,
                            FieldName = property.Name,
                            SampleValue = value.Value<float>(),
                        });
                    }
                }

                ScanToken(property.Value, childPath, result, depth + 1);
            }

            return;
        }

        if (token is JArray array)
        {
            if (array.Count == 0)
                return;

            var candidate = AnalyzeArray(path, array);
            if (candidate != null)
                result.ArrayCandidates.Add(candidate);

            if (array[0] is JObject firstObject)
            {
                foreach (JProperty property in firstObject.Properties())
                {
                    if (property.Value is JArray nestedArray && nestedArray.Count > 0)
                    {
                        string nestedPath = string.IsNullOrEmpty(path)
                            ? "[*]." + property.Name
                            : path + "[*]." + property.Name;

                        JsonArrayCandidate nestedCandidate = AnalyzeArray(nestedPath, nestedArray);
                        if (nestedCandidate != null)
                            result.ArrayCandidates.Add(nestedCandidate);
                    }
                }
            }
        }
    }

    static JsonArrayCandidate AnalyzeArray(string path, JArray array)
    {
        if (array == null || array.Count == 0)
            return null;

        JToken first = array[0];
        var candidate = new JsonArrayCandidate
        {
            Path = path,
            ItemCount = array.Count,
            SampleJson = first.ToString(Newtonsoft.Json.Formatting.None),
        };

        if (first is JObject obj)
        {
            candidate.InferredShape = InferObjectShape(obj);
            foreach (JProperty property in obj.Properties())
                candidate.ObjectFieldNames.Add(property.Name);
            return candidate;
        }

        if (first is JArray numberArray)
        {
            candidate.InferredShape = numberArray.Count switch
            {
                2 => ExternalJsonShapeKind.PointArray,
                3 => ExternalJsonShapeKind.RectArray3,
                >= 4 => ExternalJsonShapeKind.RectArray4,
                _ => ExternalJsonShapeKind.PointArray,
            };
            return candidate;
        }

        return null;
    }

    static ExternalJsonShapeKind InferObjectShape(JObject obj)
    {
        bool hasX = HasNumericField(obj, "x");
        bool hasY = HasNumericField(obj, "y");
        bool hasWidth = HasNumericField(obj, "width") || HasNumericField(obj, "w");
        bool hasHeight = HasNumericField(obj, "height") || HasNumericField(obj, "h");

        if (hasX && hasY && (hasWidth || hasHeight))
            return ExternalJsonShapeKind.RectObject;

        if (hasX && hasY)
            return ExternalJsonShapeKind.PointObject;

        return ExternalJsonShapeKind.RectObject;
    }

    static bool HasNumericField(JObject obj, string name)
    {
        if (obj == null || !obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out JToken token))
            return false;

        return token.Type == JTokenType.Integer || token.Type == JTokenType.Float;
    }

    static bool TryReadPositionObject(JObject obj, out float x, out float y)
    {
        x = y = 0f;
        if (obj == null)
            return false;

        bool hasX = TryReadField(obj, "x", out x);
        bool hasY = TryReadField(obj, "y", out y);
        return hasX && hasY;
    }

    static bool TryReadField(JObject obj, string name, out float value)
    {
        value = 0f;
        if (!obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out JToken token))
            return false;

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            value = token.Value<float>();
            return true;
        }

        return false;
    }

    public static ExternalJsonImportProfile BuildSuggestedProfile(JsonStructureScanResult scan, string sourceFileName)
    {
        string baseName = string.IsNullOrEmpty(sourceFileName)
            ? "custom"
            : System.IO.Path.GetFileNameWithoutExtension(sourceFileName);

        var profile = new ExternalJsonImportProfile
        {
            formatId = "profile." + baseName.ToLowerInvariant(),
            displayName = "Mapped: " + baseName,
            pixelScale = ExternalJsonCoordinateUtil.DefaultPixelScale,
        };

        JsonScalarFieldCandidate width = scan.ScalarFieldCandidates.Find(c =>
            c.FieldName.Equals("width", StringComparison.OrdinalIgnoreCase)
            || c.FieldName.Equals("groundWidth", StringComparison.OrdinalIgnoreCase)
            || c.FieldName.Equals("mapWidth", StringComparison.OrdinalIgnoreCase));

        JsonScalarFieldCandidate height = scan.ScalarFieldCandidates.Find(c =>
            c.FieldName.Equals("height", StringComparison.OrdinalIgnoreCase)
            || c.FieldName.Equals("mapHeight", StringComparison.OrdinalIgnoreCase));

        if (width != null)
            profile.viewportWidthPath = width.Path;
        if (height != null)
            profile.viewportHeightPath = height.Path;

        var sources = new List<ExternalJsonObjectSourceProfile>();
        foreach (JsonArrayCandidate arrayCandidate in scan.ArrayCandidates)
        {
            string id = GetLeafName(arrayCandidate.Path);
            sources.Add(new ExternalJsonObjectSourceProfile
            {
                enabled = true,
                id = id,
                displayName = id,
                jsonPath = arrayCandidate.Path,
                isArray = true,
                shape = arrayCandidate.InferredShape,
                xField = "x",
                yField = "y",
                widthField = "width",
                heightField = "height",
            });
        }

        foreach (JsonSingleObjectCandidate single in scan.SingleObjectCandidates)
        {
            string id = GetLeafName(single.Path);
            sources.Add(new ExternalJsonObjectSourceProfile
            {
                enabled = true,
                id = id,
                displayName = id,
                jsonPath = single.Path,
                isArray = false,
                shape = single.InferredShape,
                xField = "x",
                yField = "y",
                widthField = "width",
                heightField = "height",
                defaultWidth = 32f,
                defaultHeight = 32f,
            });
        }

        profile.SetObjectSources(sources);
        return profile;
    }

    static string GetLeafName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "object";

        int dot = path.LastIndexOf('.');
        string leaf = dot >= 0 ? path[(dot + 1)..] : path;
        int bracket = leaf.IndexOf('[');
        if (bracket >= 0)
            leaf = leaf[..bracket];

        return string.IsNullOrEmpty(leaf) ? "object" : leaf;
    }
}
