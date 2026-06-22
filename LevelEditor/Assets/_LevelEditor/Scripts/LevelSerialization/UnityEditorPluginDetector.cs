using System.IO;

/// <summary>
/// Detects whether the Level Editor JSON Importer plugin is installed in a Unity project folder.
/// </summary>
public static class UnityEditorPluginDetector
{
    public const string PluginFolderName = "LevelEditorJsonImporter";
    public const string PluginMarkerFileName = "LevelJsonSceneImporter.cs";

    /// <summary>Optional URL shown in the missing-plugin dialog. Leave empty to omit.</summary>
    public const string DocumentationUrl = "";

    public static bool IsValidUnityProjectRoot(string projectFolderPath)
    {
        if (string.IsNullOrWhiteSpace(projectFolderPath))
            return false;

        return Directory.Exists(Path.Combine(projectFolderPath, "Assets"));
    }

    public static bool IsPluginInstalled(string unityProjectRoot)
    {
        if (!IsValidUnityProjectRoot(unityProjectRoot))
            return false;

        string assetsRoot = Path.Combine(Path.GetFullPath(unityProjectRoot), "Assets");
        foreach (string file in Directory.GetFiles(assetsRoot, PluginMarkerFileName, SearchOption.AllDirectories))
        {
            string normalized = file.Replace('\\', '/');
            if (normalized.Contains("/" + PluginFolderName + "/", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static string GetSuggestedPluginInstallPath(string unityProjectRoot)
    {
        if (string.IsNullOrWhiteSpace(unityProjectRoot))
            return $"Assets/Scripts/{PluginFolderName}";

        return Path.Combine(unityProjectRoot, "Assets", "Scripts", PluginFolderName)
            .Replace('\\', '/');
    }

    public static string BuildMissingPluginDetails(string unityProjectRoot)
    {
        string suggestedPath = GetSuggestedPluginInstallPath(unityProjectRoot);
        string details =
            "Install the Level Editor JSON Importer plugin in your Unity project:\n\n" +
            "1. Copy the \"" + PluginFolderName + "\" folder into your Unity project's Assets folder.\n" +
            "   Suggested location:\n   " + suggestedPath + "\n\n" +
            "2. Open the Unity project and wait for scripts to compile.\n\n" +
            "3. Return here and choose Link Unity Project again.\n\n" +
            "You can keep using the tool, but game assets from that Unity project will not load until linking succeeds.";

        if (!string.IsNullOrWhiteSpace(DocumentationUrl))
            details += "\n\nDocumentation:\n" + DocumentationUrl;

        return details;
    }
}
