using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes a small manifest into the linked Unity project so the editor plugin can detect tool linkage.
/// </summary>
public static class LevelEditorToolLinkManifest
{
    public const string ManifestFileName = "level_editor_tool_link.json";
    public const string ManifestRelativeFolder = "Assets/LevelEditorJsonImporter";

    [Serializable]
    sealed class ToolLinkManifest
    {
        public string linkedAtUtc = "";
        public string activeLevelJsonPath = "";
        public long activeLevelJsonUtcTicks;
        public string activeLevelUpdatedAtUtc = "";
    }

    public static void WriteLinkEstablished(string unityProjectRoot)
    {
        WriteToUnityProject(unityProjectRoot, updateLinkTimestamp: true);
    }

    public static void WriteActiveLevel(string unityProjectRoot)
    {
        WriteToUnityProject(unityProjectRoot, updateLinkTimestamp: false);
    }

    /// <summary>Writes the tool's currently open level into the linked Unity project manifest.</summary>
    public static void WriteActiveLevelIfLinked()
    {
        if (TryResolveLinkedUnityProjectRoot(out string unityProjectRoot))
            WriteActiveLevel(unityProjectRoot);
    }

    public static bool TryResolveLinkedUnityProjectRoot(out string unityProjectRoot)
    {
        if (LevelEditorUnityLinkSession.HasLinkedUnityProject)
        {
            unityProjectRoot = LevelEditorUnityLinkSession.LinkedUnityProjectRoot;
            return true;
        }

        if (!LevelProjectSession.HasOpenProject)
        {
            unityProjectRoot = null;
            return false;
        }

        unityProjectRoot = UnityProjectRootResolver.ResolveLinkedRoot(LevelProjectSession.CurrentProjectDirectory);
        if (string.IsNullOrEmpty(unityProjectRoot))
            unityProjectRoot = LevelEditorGlobalUnityLink.TryResolveGlobalLink();

        return !string.IsNullOrEmpty(unityProjectRoot)
            && Directory.Exists(unityProjectRoot)
            && UnityEditorPluginDetector.IsPluginInstalled(unityProjectRoot);
    }

    static void WriteToUnityProject(string unityProjectRoot, bool updateLinkTimestamp)
    {
        if (string.IsNullOrWhiteSpace(unityProjectRoot) || !UnityEditorPluginDetector.IsPluginInstalled(unityProjectRoot))
            return;

        string manifestDirectory = Path.Combine(unityProjectRoot, ManifestRelativeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(manifestDirectory);

        string manifestPath = Path.Combine(manifestDirectory, ManifestFileName);
        ToolLinkManifest manifest = ReadManifest(manifestPath) ?? new ToolLinkManifest();

        if (updateLinkTimestamp || string.IsNullOrWhiteSpace(manifest.linkedAtUtc))
            manifest.linkedAtUtc = DateTime.UtcNow.ToString("o");

        manifest.activeLevelJsonPath = LevelProjectSession.CurrentLevelJsonPath ?? "";
        manifest.activeLevelUpdatedAtUtc = DateTime.UtcNow.ToString("o");

        if (!string.IsNullOrWhiteSpace(manifest.activeLevelJsonPath)
            && File.Exists(manifest.activeLevelJsonPath))
        {
            manifest.activeLevelJsonUtcTicks = File.GetLastWriteTimeUtc(manifest.activeLevelJsonPath).Ticks;
        }
        else
        {
            manifest.activeLevelJsonUtcTicks = 0;
        }

        File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true), Encoding.UTF8);
    }

    static ToolLinkManifest ReadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            return JsonUtility.FromJson<ToolLinkManifest>(File.ReadAllText(manifestPath, Encoding.UTF8));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
