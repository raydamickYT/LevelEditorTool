using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LevelEditorJsonImporter;
using UnityEditor;
using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    /// <summary>
    /// Matches <see cref="UnityProjectRootResolver"/> link file format from the Level Editor Tool.
    /// </summary>
    static class LevelEditorImportPathResolver
    {
        public const string LinkFileName = "unity_project_link.json";

        [Serializable]
        sealed class UnityProjectLinkFile
        {
            public string unityProjectRoot = "";
        }

        public static void EnsureUnityProjectLinked(string levelDirectory, IEnumerable<LevelEditorAssetMetaData> assets)
        {
            if (string.IsNullOrWhiteSpace(levelDirectory) || assets == null)
                return;

            List<LevelEditorAssetMetaData> missing = assets
                .Where(NeedsExternalResolve)
                .Where(asset => !CanResolveInCurrentProject(asset))
                .Where(asset => string.IsNullOrEmpty(TryResolveExternalAbsolutePath(levelDirectory, asset)))
                .Distinct()
                .ToList();

            if (missing.Count == 0)
                return;

            string details = string.Join(
                "\n",
                missing.Take(8).Select(asset => asset.AssetRelativePath));

            if (missing.Count > 8)
                details += $"\n... and {missing.Count - 8} more";

            bool picked = EditorUtility.DisplayDialog(
                "Locate Unity project",
                "Some level assets are not in this Unity project and the saved project path is missing or from another computer.\n\n" +
                "Select the Unity project folder that contains the original Assets (it must contain an Assets subfolder).",
                "Browse...",
                "Skip");

            if (!picked)
            {
                Debug.LogWarning("Level import: skipped linking external Unity project. Some assets may be missing.");
                return;
            }

            string folder = EditorUtility.OpenFolderPanel("Locate Unity project for this level", levelDirectory, "");
            if (string.IsNullOrWhiteSpace(folder))
                return;

            if (!Directory.Exists(Path.Combine(folder, "Assets")))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Unity project",
                    "The selected folder must be a Unity project root (it should contain an Assets folder).",
                    "OK");
                return;
            }

            SaveLinkedRoot(levelDirectory, folder);
            Debug.Log($"Level import: linked external Unity project to:\n{folder}");
        }

        public static bool CanResolveInCurrentProject(LevelEditorAssetMetaData asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.AssetRelativePath))
                return false;

            string path = asset.AssetRelativePath.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return false;

            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null;
        }

        public static string TryResolveExternalAbsolutePath(string levelDirectory, LevelEditorAssetMetaData asset)
        {
            if (!NeedsExternalResolve(asset))
                return null;

            string projectRoot = ResolveSourceProjectRoot(levelDirectory, asset.SourceProjectRoot);
            if (string.IsNullOrEmpty(projectRoot))
                return null;

            string absolutePath = Path.GetFullPath(Path.Combine(
                projectRoot,
                asset.AssetRelativePath.Replace('/', Path.DirectorySeparatorChar)));

            return File.Exists(absolutePath) ? absolutePath : null;
        }

        public static GameObject LoadExternalPrefab(string absolutePrefabPath)
        {
            if (string.IsNullOrWhiteSpace(absolutePrefabPath) || !File.Exists(absolutePrefabPath))
                return null;

            try
            {
                return PrefabUtility.LoadPrefabContents(absolutePrefabPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not load prefab from external path:\n{absolutePrefabPath}\n{ex.Message}");
                return null;
            }
        }

        public static Sprite LoadExternalSprite(string absoluteAssetPath, LevelEditorAssetMetaData asset)
        {
            if (string.IsNullOrWhiteSpace(absoluteAssetPath) || !File.Exists(absoluteAssetPath))
                return null;

            string extension = Path.GetExtension(absoluteAssetPath).ToLowerInvariant();
            if (extension is not (".png" or ".jpg" or ".jpeg"))
                return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(absoluteAssetPath);
                Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    return null;
                }

                texture.filterMode = FilterMode.Point;
                Rect rect = new(0, 0, texture.width, texture.height);
                if (asset != null && asset.SpriteRectWidth > 0f && asset.SpriteRectHeight > 0f)
                {
                    rect = new Rect(asset.SpriteRectX, asset.SpriteRectY, asset.SpriteRectWidth, asset.SpriteRectHeight);
                }

                float ppu = asset != null && asset.PixelsPerUnit > 0f ? asset.PixelsPerUnit : 100f;
                return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), ppu);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not load sprite from external path:\n{absoluteAssetPath}\n{ex.Message}");
                return null;
            }
        }

        static bool NeedsExternalResolve(LevelEditorAssetMetaData asset)
        {
            return asset != null
                && !string.IsNullOrWhiteSpace(asset.AssetRelativePath)
                && asset.AssetRelativePath.Replace('\\', '/').StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        static string ResolveSourceProjectRoot(string levelDirectory, string storedSourceProjectRoot)
        {
            if (!string.IsNullOrWhiteSpace(storedSourceProjectRoot))
            {
                string fromStored = ResolveStoredRootPath(levelDirectory, storedSourceProjectRoot);
                if (!string.IsNullOrEmpty(fromStored))
                    return fromStored;
            }

            return ResolveLinkedRoot(levelDirectory);
        }

        static string ResolveLinkedRoot(string levelDirectory)
        {
            string linkPath = Path.Combine(levelDirectory, LinkFileName);
            if (!File.Exists(linkPath))
                return null;

            UnityProjectLinkFile link = JsonUtility.FromJson<UnityProjectLinkFile>(File.ReadAllText(linkPath, Encoding.UTF8));
            if (link == null || string.IsNullOrWhiteSpace(link.unityProjectRoot))
                return null;

            return ResolveStoredRootPath(levelDirectory, link.unityProjectRoot);
        }

        static void SaveLinkedRoot(string levelDirectory, string absoluteUnityProjectRoot)
        {
            string fullRoot = Path.GetFullPath(absoluteUnityProjectRoot);
            string stored = TryMakeRelativePath(levelDirectory, fullRoot) ?? fullRoot;

            UnityProjectLinkFile link = new() { unityProjectRoot = stored.Replace('\\', '/') };
            File.WriteAllText(
                Path.Combine(levelDirectory, LinkFileName),
                JsonUtility.ToJson(link, true),
                Encoding.UTF8);
        }

        static string ResolveStoredRootPath(string levelDirectory, string storedRoot)
        {
            if (string.IsNullOrWhiteSpace(storedRoot))
                return null;

            storedRoot = storedRoot.Trim();
            if (Path.IsPathRooted(storedRoot))
                return Directory.Exists(storedRoot) ? Path.GetFullPath(storedRoot) : null;

            string combined = Path.GetFullPath(Path.Combine(
                levelDirectory,
                storedRoot.Replace('/', Path.DirectorySeparatorChar)));

            return Directory.Exists(combined) ? combined : null;
        }

        static string TryMakeRelativePath(string baseDirectory, string absolutePath)
        {
            try
            {
                string relative = Path.GetRelativePath(
                    Path.GetFullPath(baseDirectory),
                    Path.GetFullPath(absolutePath));

                if (string.IsNullOrWhiteSpace(relative)
                    || relative.StartsWith("..", StringComparison.Ordinal)
                    || Path.IsPathRooted(relative))
                {
                    return null;
                }

                return relative.Replace('\\', '/');
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
