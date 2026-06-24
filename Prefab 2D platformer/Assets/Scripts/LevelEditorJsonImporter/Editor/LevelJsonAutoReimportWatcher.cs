using System;
using System.IO;
using LevelEditorJsonImporter;
using UnityEditor;
using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    [InitializeOnLoad]
    public static class LevelJsonAutoReimportWatcher
    {
        static bool isChecking;
        static string lastProcessedToolLinkSignature = "";

        static LevelJsonAutoReimportWatcher()
        {
            EditorApplication.focusChanged += OnEditorFocusChanged;
        }

        static void OnEditorFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                return;

            EditorApplication.delayCall += () =>
            {
                CheckToolLinkedLevel();
                CheckImportedLevels();
            };
        }

        static void CheckToolLinkedLevel()
        {
            if (!LevelEditorToolLinkReader.TryReadCurrentProject(out LevelEditorToolLinkReader.ToolLinkManifest manifest, out _))
                return;

            string signature = BuildToolLinkSignature(manifest);
            if (string.Equals(signature, lastProcessedToolLinkSignature, StringComparison.Ordinal))
                return;

            lastProcessedToolLinkSignature = signature;

            string levelJsonPath = manifest.activeLevelJsonPath;
            if (string.IsNullOrWhiteSpace(levelJsonPath) || !File.Exists(levelJsonPath))
                return;

            long levelTicks = manifest.activeLevelJsonUtcTicks > 0
                ? manifest.activeLevelJsonUtcTicks
                : LevelJsonSceneImporter.GetLevelJsonUtcTicks(levelJsonPath);

            if (levelTicks <= 0)
                return;

            LevelJsonImportSource existingSource = FindImportSourceForPath(levelJsonPath);
            if (existingSource != null && existingSource.lastImportedUtcTicks >= levelTicks)
                return;

            const string importRootName = "Imported Level";
            bool clearExistingRoot = existingSource == null;

            Debug.Log($"Level Editor Tool updated the active level. Auto-importing '{levelJsonPath}'.");
            LevelJsonSceneImporter.ImportFromPath(
                levelJsonPath,
                importRootName,
                clearExistingImportRoot: clearExistingRoot,
                selectImportedRoot: true,
                logResult: true);
        }

        static string BuildToolLinkSignature(LevelEditorToolLinkReader.ToolLinkManifest manifest)
        {
            if (manifest == null)
                return "";

            return string.Join("|",
                manifest.activeLevelJsonPath ?? "",
                manifest.activeLevelJsonUtcTicks.ToString(),
                manifest.activeLevelUpdatedAtUtc ?? "");
        }

        static LevelJsonImportSource FindImportSourceForPath(string levelJsonPath)
        {
            if (string.IsNullOrWhiteSpace(levelJsonPath))
                return null;

            string fullPath = Path.GetFullPath(levelJsonPath);
            LevelJsonImportSource[] sources = Resources.FindObjectsOfTypeAll<LevelJsonImportSource>();
            foreach (LevelJsonImportSource source in sources)
            {
                if (source == null || EditorUtility.IsPersistent(source))
                    continue;

                if (string.IsNullOrWhiteSpace(source.levelJsonPath))
                    continue;

                if (string.Equals(Path.GetFullPath(source.levelJsonPath), fullPath, StringComparison.OrdinalIgnoreCase))
                    return source;
            }

            return null;
        }

        static void CheckImportedLevels()
        {
            if (isChecking)
                return;

            isChecking = true;
            try
            {
                LevelJsonImportSource[] sources = Resources.FindObjectsOfTypeAll<LevelJsonImportSource>();
                foreach (LevelJsonImportSource source in sources)
                {
                    if (source == null
                        || EditorUtility.IsPersistent(source)
                        || !source.autoReimportOnUnityFocus
                        || string.IsNullOrWhiteSpace(source.levelJsonPath)
                        || !File.Exists(source.levelJsonPath))
                    {
                        continue;
                    }

                    long currentTicks = LevelJsonSceneImporter.GetLevelJsonUtcTicks(source.levelJsonPath);
                    if (currentTicks <= 0 || currentTicks <= source.lastImportedUtcTicks)
                        continue;

                    string levelJsonPath = source.levelJsonPath;
                    string importRootName = string.IsNullOrWhiteSpace(source.importRootName)
                        ? source.gameObject.name
                        : source.importRootName;

                    Debug.Log($"Level JSON changed. Reimporting '{levelJsonPath}'.");
                    LevelJsonSceneImporter.ImportFromPath(
                        levelJsonPath,
                        importRootName,
                        clearExistingImportRoot: false,
                        selectImportedRoot: false,
                        logResult: false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                isChecking = false;
            }
        }
    }
}
