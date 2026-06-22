using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    /// <summary>
    /// Reads <c>level_editor_tool_link.json</c> written by the Level Editor Tool when a Unity project is linked.
    /// </summary>
    static class LevelEditorToolLinkReader
    {
        public const string ManifestFileName = "level_editor_tool_link.json";
        public const string ManifestAssetFolder = "Assets/LevelEditorJsonImporter";

        [Serializable]
        public sealed class ToolLinkManifest
        {
            public string linkedAtUtc = "";
            public string activeLevelJsonPath = "";
            public long activeLevelJsonUtcTicks;
        }

        public static bool TryReadCurrentProject(out ToolLinkManifest manifest, out string manifestAbsolutePath)
        {
            manifest = null;
            manifestAbsolutePath = null;

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                return false;

            manifestAbsolutePath = Path.Combine(projectRoot, ManifestAssetFolder, ManifestFileName);
            if (!File.Exists(manifestAbsolutePath))
                return false;

            try
            {
                manifest = JsonUtility.FromJson<ToolLinkManifest>(File.ReadAllText(manifestAbsolutePath, Encoding.UTF8));
                return manifest != null && !string.IsNullOrWhiteSpace(manifest.linkedAtUtc);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not read {ManifestFileName}: {ex.Message}");
                return false;
            }
        }

        public static bool TryGetLinkedAtLocalTime(out DateTime linkedAtLocal, out string formatted)
        {
            linkedAtLocal = default;
            formatted = "";

            if (!TryReadCurrentProject(out ToolLinkManifest manifest, out _))
                return false;

            if (!DateTime.TryParse(manifest.linkedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime linkedAtUtc))
                return false;

            linkedAtLocal = linkedAtUtc.ToLocalTime();
            formatted = linkedAtLocal.ToString("g");
            return true;
        }
    }
}
