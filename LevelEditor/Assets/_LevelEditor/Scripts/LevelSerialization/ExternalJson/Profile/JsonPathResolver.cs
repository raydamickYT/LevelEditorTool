using System;
using Newtonsoft.Json.Linq;

public static class JsonPathResolver
{
    public const string RootPath = "$";

    public static bool IsRootPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        return string.Equals(path.Trim(), RootPath, StringComparison.Ordinal)
            || string.Equals(path.Trim(), "$root", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizePath(string path)
    {
        return IsRootPath(path) ? RootPath : path.Trim();
    }

    public static bool TryResolve(JToken root, string path, out JToken token)
    {
        token = null;
        if (root == null)
            return false;

        if (IsRootPath(path))
        {
            token = root;
            return true;
        }

        if (root is not JObject rootObject)
            return false;

        JToken current = rootObject;
        string[] segments = path.Split('.');

        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            if (string.IsNullOrEmpty(segment))
                continue;

            int bracketIndex = segment.IndexOf('[');
            string propertyName = bracketIndex >= 0 ? segment[..bracketIndex] : segment;

            if (current is not JObject obj
                || !obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out current))
            {
                return false;
            }

            if (bracketIndex >= 0)
            {
                if (segment.Contains("[*]"))
                {
                    if (current is not JArray array || array.Count == 0)
                        return false;

                    current = array[0];
                    string remainder = segment[(segment.IndexOf(']') + 1)..];
                    if (remainder.StartsWith("."))
                        remainder = remainder[1..];

                    if (!string.IsNullOrEmpty(remainder))
                    {
                        string nestedPath = remainder;
                        for (int j = i + 1; j < segments.Length; j++)
                            nestedPath += "." + segments[j];

                        if (current is JObject nestedObject)
                            return TryResolve(nestedObject, nestedPath, out token);
                    }
                }
                else
                {
                    int close = segment.IndexOf(']');
                    string indexText = segment.Substring(bracketIndex + 1, close - bracketIndex - 1);
                    if (!int.TryParse(indexText, out int index))
                        return false;

                    if (current is not JArray indexedArray || index < 0 || index >= indexedArray.Count)
                        return false;

                    current = indexedArray[index];
                }
            }
        }

        token = current;
        return current != null;
    }

    public static bool TryResolveScalar(JToken root, string path, out float value)
    {
        value = 0f;
        if (!TryResolve(root, path, out JToken token))
            return false;

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            value = token.Value<float>();
            return true;
        }

        return false;
    }

    public static void WriteScalar(JToken root, string path, float value)
    {
        if (root == null || string.IsNullOrWhiteSpace(path) || IsRootPath(path))
            return;

        if (root is not JObject rootObject)
            return;

        string[] segments = path.Split('.');
        JObject current = rootObject;

        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            bool isLast = i == segments.Length - 1;

            if (isLast)
            {
                current[segment] = value;
                return;
            }

            if (!current.TryGetValue(segment, StringComparison.OrdinalIgnoreCase, out JToken next)
                || next is not JObject nextObject)
            {
                nextObject = new JObject();
                current[segment] = nextObject;
            }

            current = nextObject;
        }
    }

    public static void EnsureArray(JToken root, string path, out JArray array)
    {
        array = null;
        if (root == null)
            return;

        if (IsRootPath(path))
        {
            if (root is JArray rootArray)
            {
                array = rootArray;
                return;
            }

            array = new JArray();
            return;
        }

        if (root is not JObject rootObject)
            return;

        if (!TryResolve(rootObject, path, out JToken token) || token is not JArray existing)
        {
            existing = new JArray();
            SetToken(rootObject, path, existing);
        }

        array = existing;
    }

    public static JToken SetToken(JToken root, string path, JToken value)
    {
        if (root == null)
            return value;

        if (IsRootPath(path))
            return value;

        if (root is not JObject rootObject)
            return root;

        string[] segments = path.Split('.');
        JObject current = rootObject;

        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            bool isLast = i == segments.Length - 1;

            if (isLast)
            {
                current[segment] = value;
                return root;
            }

            if (!current.TryGetValue(segment, StringComparison.OrdinalIgnoreCase, out JToken next)
                || next is not JObject nextObject)
            {
                nextObject = new JObject();
                current[segment] = nextObject;
            }

            current = nextObject;
        }

        return root;
    }
}
