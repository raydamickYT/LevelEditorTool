using System.IO;
using UnityEngine;

public static class ExternalJsonProfileStorage
{
    public const string ProjectProfileFolderName = "LevelFormatProfiles";
    public const string ProjectProfileFileName = "level-format-profile.json";
    public const string ProfileSuffix = ".leveleditor-profile.json";

    public static string GetProjectProfilePath(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory))
            return string.Empty;

        return Path.Combine(
            Path.GetFullPath(projectDirectory),
            ProjectProfileFolderName,
            ProjectProfileFileName);
    }

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
        if (TryLoadProjectProfile(out profile))
            return true;

        // Legacy fallback: older builds wrote the mapping next to the external JSON.
        return TryLoadProfileAtPath(GetProfilePathForJson(jsonFilePath), out profile);
    }

    public static bool TryLoadProjectProfile(out ExternalJsonImportProfile profile)
    {
        profile = null;
        if (!LevelProjectSession.HasOpenProject)
            return false;

        return TryLoadProfileAtPath(GetProjectProfilePath(LevelProjectSession.CurrentProjectDirectory), out profile);
    }

    static bool TryLoadProfileAtPath(string profilePath, out ExternalJsonImportProfile profile)
    {
        profile = null;
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
        SaveProjectProfile(profile);
    }

    public static void SaveProjectProfile(ExternalJsonImportProfile profile)
    {
        if (profile == null || !LevelProjectSession.HasOpenProject)
            return;

        SaveProfileAtPath(GetProjectProfilePath(LevelProjectSession.CurrentProjectDirectory), profile);
    }

    static void SaveProfileAtPath(string profilePath, ExternalJsonImportProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profilePath) || profile == null)
            return;

        string directory = Path.GetDirectoryName(profilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(profile, true);
        File.WriteAllText(profilePath, json);
    }

    public static bool DeleteProjectProfile()
    {
        if (!LevelProjectSession.HasOpenProject)
            return false;

        string profilePath = GetProjectProfilePath(LevelProjectSession.CurrentProjectDirectory);
        if (string.IsNullOrEmpty(profilePath) || !File.Exists(profilePath))
            return false;

        File.Delete(profilePath);
        return true;
    }
}
