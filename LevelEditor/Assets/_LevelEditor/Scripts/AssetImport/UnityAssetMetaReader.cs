using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

public static class UnityAssetMetaReader
{
    static readonly Regex SpritePixelsToUnitsPattern = new(
        @"spritePixelsToUnits:\s*(\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryGetSpritePixelsPerUnit(string assetPath, out float pixelsPerUnit)
    {
        pixelsPerUnit = 100f;
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        string metaPath = assetPath + ".meta";
        if (!File.Exists(metaPath))
            return false;

        Match match = SpritePixelsToUnitsPattern.Match(File.ReadAllText(metaPath));
        if (!match.Success
            || !float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            || parsed <= 0f)
        {
            return false;
        }

        pixelsPerUnit = parsed;
        return true;
    }
}
