using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Remembers the last Unity project linked in the tool (session-wide fallback when a level folder has no own link file).
/// </summary>
public static class LevelEditorGlobalUnityLink
{
    const string FileName = "global_unity_project_link.json";

    [Serializable]
    sealed class GlobalUnityLinkFile
    {
        public string unityProjectRoot = "";
        public string linkedAtUtc = "";
    }

    static string GlobalLinkPath
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

    public static void Save(string absoluteUnityProjectRoot)
    {
        if (string.IsNullOrWhiteSpace(absoluteUnityProjectRoot))
            return;

        string directory = Path.GetDirectoryName(GlobalLinkPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        GlobalUnityLinkFile link = new()
        {
            unityProjectRoot = Path.GetFullPath(absoluteUnityProjectRoot).Replace('\\', '/'),
            linkedAtUtc = DateTime.UtcNow.ToString("o"),
        };

        File.WriteAllText(GlobalLinkPath, JsonUtility.ToJson(link, true), Encoding.UTF8);
    }

    public static string TryResolveGlobalLink()
    {
        if (!File.Exists(GlobalLinkPath))
            return null;

        try
        {
            GlobalUnityLinkFile link = JsonUtility.FromJson<GlobalUnityLinkFile>(File.ReadAllText(GlobalLinkPath, Encoding.UTF8));
            if (link == null || string.IsNullOrWhiteSpace(link.unityProjectRoot))
                return null;

            return Directory.Exists(link.unityProjectRoot) ? Path.GetFullPath(link.unityProjectRoot) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
