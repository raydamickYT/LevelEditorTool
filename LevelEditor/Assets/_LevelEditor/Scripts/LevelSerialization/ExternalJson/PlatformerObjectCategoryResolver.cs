using System;
using System.IO;

public static class PlatformerObjectCategoryResolver
{
    public const string FormatId = "pgattic.platformer";

    public static bool TryResolveCategory(LevelObject levelObject, out string category)
    {
        category = string.Empty;
        if (levelObject == null)
            return false;

        if (levelObject.TryGetComponent(out ExternalJsonObjectBinding binding)
            && !string.IsNullOrWhiteSpace(binding.SourceCategory))
        {
            category = binding.SourceCategory;
            return true;
        }

        if (TryResolveFromName(levelObject.gameObject.name, out category))
            return true;

        if (!string.IsNullOrEmpty(levelObject.AssetID))
        {
            ImportedAssetMetaData asset = AssetStorageService.GetAssetByID(levelObject.AssetID);
            if (asset != null && TryResolveFromName(asset.FileName, out category))
                return true;
        }

        return false;
    }

    static bool TryResolveFromName(string rawName, out string category)
    {
        category = string.Empty;
        if (string.IsNullOrWhiteSpace(rawName))
            return false;

        string name = Path.GetFileNameWithoutExtension(rawName).ToLowerInvariant();

        if (name.Contains("platform") || name.StartsWith("box"))
        {
            category = "boxes";
            return true;
        }

        if (name.Contains("lava"))
        {
            category = "lava";
            return true;
        }

        if (name.Contains("key"))
        {
            category = "keys";
            return true;
        }

        if (name.Contains("goal") || name.Contains("end"))
        {
            category = "end";
            return true;
        }

        if (name.Contains("start") || name.Contains("player"))
        {
            category = "start";
            return true;
        }

        if (name.Contains("portal"))
        {
            category = "portals";
            return true;
        }

        return false;
    }

    public static bool TryParsePortalSuffix(string objectName, out int portalIndex, out bool isPointB)
    {
        portalIndex = -1;
        isPointB = false;

        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        string name = objectName;
        int underscoreA = name.LastIndexOf("_A", StringComparison.OrdinalIgnoreCase);
        int underscoreB = name.LastIndexOf("_B", StringComparison.OrdinalIgnoreCase);

        if (underscoreB >= 0 && name.Length - underscoreB == 2)
        {
            isPointB = true;
            name = name.Substring(0, underscoreB);
        }
        else if (underscoreA >= 0 && name.Length - underscoreA == 2)
        {
            isPointB = false;
            name = name.Substring(0, underscoreA);
        }

        const string prefix = "Portal_";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string indexPart = name.Substring(prefix.Length);
        return int.TryParse(indexPart, out portalIndex);
    }
}
