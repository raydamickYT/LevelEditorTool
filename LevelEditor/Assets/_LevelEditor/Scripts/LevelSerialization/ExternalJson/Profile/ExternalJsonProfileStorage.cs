using System.IO;
using UnityEngine;

public static class ExternalJsonProfileStorage
{
    public const string ProfileSuffix = ".leveleditor-profile.json";

    public static string GetProfilePathForJson(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            return string.Empty;

        string directory = Path.GetDirectoryName(jsonFilePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(jsonFilePath);
        return Path.Combine(directory, baseName + ProfileSuffix);
    }

    public static bool TryLoadProfileForJson(string jsonFilePath, out ExternalJsonImportProfile profile)
    {
        profile = null;
        string profilePath = GetProfilePathForJson(jsonFilePath);
        if (string.IsNullOrEmpty(profilePath) || !File.Exists(profilePath))
            return false;

        try
        {
            string json = File.ReadAllText(profilePath);
            profile = JsonUtility.FromJson<ExternalJsonImportProfile>(json);
            return profile != null && profile.objectSources != null && profile.objectSources.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static void SaveProfileForJson(string jsonFilePath, ExternalJsonImportProfile profile)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath) || profile == null)
            return;

        string profilePath = GetProfilePathForJson(jsonFilePath);
        string directory = Path.GetDirectoryName(profilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(profile, true);
        File.WriteAllText(profilePath, json);
    }
}
