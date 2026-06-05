using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LevelEditorJsonImporter;
using UnityEditor;
using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    /// <summary>
    /// Copies sprites bundled with a level project into <see cref="UnitySpritesFolder"/> so Unity can import them.
    /// Used for sprites imported in the Level Editor Tool from outside the Unity project.
    /// </summary>
    static class LevelEditorBundledSpriteImporter
    {
        public const string UnitySpritesFolder = "Assets/LevelEditorImported/Sprites";

        const string ProjectAssetsSpritesFolder = "ProjectAssets/Sprites";
        const string BundledAssetsSpritesFolder = "BundledAssets/Sprites";

        static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg"
        };

        public static Sprite ImportOrLoadSprite(string levelDirectory, LevelEditorAssetMetaData asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.AssetID))
                return null;

            if (!string.Equals(asset.AssetType, LevelEditorImportedAssetTypes.Sprite, StringComparison.OrdinalIgnoreCase))
                return null;

            string unityAssetPath = GetUnityAssetPath(asset);
            Sprite existing = LevelJsonSceneImporter.LoadSpriteAtPath(unityAssetPath, asset);
            if (existing != null)
                return existing;

            string sourcePath = ResolveBundledSourcePath(levelDirectory, asset);
            if (string.IsNullOrEmpty(sourcePath))
                return null;

            EnsureUnitySpritesFolderExists();

            string destinationAbsolutePath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", unityAssetPath.Replace('/', Path.DirectorySeparatorChar)));

            string destinationDirectory = Path.GetDirectoryName(destinationAbsolutePath);
            if (!string.IsNullOrEmpty(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            string sourceFullPath = Path.GetFullPath(sourcePath);
            if (!sourceFullPath.Equals(destinationAbsolutePath, StringComparison.OrdinalIgnoreCase))
            {
                byte[] bytes = File.ReadAllBytes(sourceFullPath);
                File.WriteAllBytes(destinationAbsolutePath, bytes);
            }

            AssetDatabase.ImportAsset(unityAssetPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteTextureImporter(unityAssetPath, asset);

            asset.AssetRelativePath = unityAssetPath;
            return LevelJsonSceneImporter.LoadSpriteAtPath(unityAssetPath, asset);
        }

        public static string GetUnityAssetPath(LevelEditorAssetMetaData asset)
        {
            string extension = GetSourceExtension(asset);
            return $"{UnitySpritesFolder}/{asset.AssetID}{extension}".Replace('\\', '/');
        }

        static string ResolveBundledSourcePath(string levelDirectory, LevelEditorAssetMetaData asset)
        {
            if (asset == null)
                return null;

            List<string> candidates = new();

            if (!string.IsNullOrWhiteSpace(asset.LocalFilePath))
            {
                if (Path.IsPathRooted(asset.LocalFilePath))
                    candidates.Add(asset.LocalFilePath);
                else if (!string.IsNullOrEmpty(levelDirectory))
                    candidates.Add(Path.Combine(levelDirectory, asset.LocalFilePath));
            }

            if (!string.IsNullOrEmpty(levelDirectory))
            {
                string extension = GetSourceExtension(asset);
                candidates.Add(Path.Combine(levelDirectory, ProjectAssetsSpritesFolder, asset.AssetID + extension));
                candidates.Add(Path.Combine(levelDirectory, BundledAssetsSpritesFolder, asset.AssetID + extension));
            }

            if (!string.IsNullOrWhiteSpace(asset.OriginalFilePath))
            {
                if (Path.IsPathRooted(asset.OriginalFilePath))
                    candidates.Add(asset.OriginalFilePath);
                else if (!string.IsNullOrEmpty(levelDirectory))
                    candidates.Add(Path.Combine(levelDirectory, asset.OriginalFilePath));
            }

            foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                string fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath) && IsSupportedImage(fullPath))
                    return fullPath;
            }

            return null;
        }

        static string GetSourceExtension(LevelEditorAssetMetaData asset)
        {
            foreach (string path in new[] { asset?.LocalFilePath, asset?.FileName, asset?.OriginalFilePath })
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                string extension = Path.GetExtension(path);
                if (IsSupportedExtension(extension))
                    return extension.ToLowerInvariant();
            }

            return ".png";
        }

        static bool IsSupportedImage(string path)
        {
            return IsSupportedExtension(Path.GetExtension(path));
        }

        static bool IsSupportedExtension(string extension)
        {
            return !string.IsNullOrWhiteSpace(extension) && SupportedExtensions.Contains(extension);
        }

        static void EnsureUnitySpritesFolderExists()
        {
            if (AssetDatabase.IsValidFolder(UnitySpritesFolder))
                return;

            const string rootFolder = "Assets/LevelEditorImported";
            if (!AssetDatabase.IsValidFolder(rootFolder))
                AssetDatabase.CreateFolder("Assets", "LevelEditorImported");

            AssetDatabase.CreateFolder(rootFolder, "Sprites");
        }

        static void ConfigureSpriteTextureImporter(string unityAssetPath, LevelEditorAssetMetaData asset)
        {
            TextureImporter importer = AssetImporter.GetAtPath(unityAssetPath) as TextureImporter;
            if (importer == null)
                return;

            float pixelsPerUnit = ResolvePixelsPerUnit(asset);

            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;

            SpriteTextureImporterUtility.ApplyPhysicsShapeImportSettings(importer);
            importer.SaveAndReimport();
        }

        static float ResolvePixelsPerUnit(LevelEditorAssetMetaData asset)
        {
            if (asset != null
                && !string.IsNullOrWhiteSpace(asset.SourceProjectRoot)
                && !string.IsNullOrWhiteSpace(asset.AssetRelativePath))
            {
                string sourceAssetPath = Path.Combine(asset.SourceProjectRoot, asset.AssetRelativePath);
                if (TryReadPixelsPerUnitFromMeta(sourceAssetPath + ".meta", out float sourcePixelsPerUnit))
                    return sourcePixelsPerUnit;
            }

            return asset != null && asset.PixelsPerUnit > 0f ? asset.PixelsPerUnit : 100f;
        }

        static bool TryReadPixelsPerUnitFromMeta(string metaPath, out float pixelsPerUnit)
        {
            pixelsPerUnit = 100f;
            if (string.IsNullOrWhiteSpace(metaPath) || !File.Exists(metaPath))
                return false;

            Match match = Regex.Match(
                File.ReadAllText(metaPath),
                @"spritePixelsToUnits:\s*(\d+(?:\.\d+)?)",
                RegexOptions.CultureInvariant);

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
}
