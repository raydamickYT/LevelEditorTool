using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Persists recently opened Level Editor project folders for the File menu.
/// </summary>
public static class LevelProjectRecentList
{
    const int MaxEntries = 5;
    const string FileName = "recent_projects.json";

    [Serializable]
    sealed class RecentProjectsFile
    {
        public List<string> projectDirectories = new();
    }

    static string StoragePath
    {
        get
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UserData", FileName));
#else
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "UserData", FileName));
#endif
        }
    }

    public static IReadOnlyList<string> GetRecentProjectDirectories()
    {
        RecentProjectsFile data = Load();
        PruneMissing(data);
        return data.projectDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToList();
    }

    public static void RegisterProjectDirectory(string projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory))
            return;

        string fullPath = Path.GetFullPath(projectDirectory);
        if (!Directory.Exists(fullPath))
            return;

        if (!LevelProjectService.TryGetLevelJsonPath(fullPath, out _))
            return;

        RecentProjectsFile data = Load();
        data.projectDirectories.RemoveAll(path =>
            string.Equals(Path.GetFullPath(path), fullPath, StringComparison.OrdinalIgnoreCase));

        data.projectDirectories.Insert(0, fullPath);

        if (data.projectDirectories.Count > MaxEntries)
            data.projectDirectories.RemoveRange(MaxEntries, data.projectDirectories.Count - MaxEntries);

        Save(data);
    }

    public static void RemoveMissingEntries()
    {
        RecentProjectsFile data = Load();
        if (PruneMissing(data))
            Save(data);
    }

    static bool PruneMissing(RecentProjectsFile data)
    {
        if (data?.projectDirectories == null)
            return false;

        int before = data.projectDirectories.Count;
        data.projectDirectories = data.projectDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(path => Directory.Exists(path) && LevelProjectService.TryGetLevelJsonPath(path, out _))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return data.projectDirectories.Count != before;
    }

    static RecentProjectsFile Load()
    {
        if (!File.Exists(StoragePath))
            return new RecentProjectsFile();

        try
        {
            string json = File.ReadAllText(StoragePath, Encoding.UTF8);
            RecentProjectsFile data = JsonUtility.FromJson<RecentProjectsFile>(json);
            return data?.projectDirectories != null ? data : new RecentProjectsFile();
        }
        catch
        {
            return new RecentProjectsFile();
        }
    }

    static void Save(RecentProjectsFile data)
    {
        if (data == null)
            return;

        string directory = Path.GetDirectoryName(StoragePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(StoragePath, JsonUtility.ToJson(data, true), Encoding.UTF8);
    }
}
