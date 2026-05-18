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

        static LevelJsonAutoReimportWatcher()
        {
            EditorApplication.focusChanged += OnEditorFocusChanged;
        }

        static void OnEditorFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                return;

            EditorApplication.delayCall += CheckImportedLevels;
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
                        clearExistingImportRoot: true,
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
