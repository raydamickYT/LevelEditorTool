using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor menu entries for level JSON export/import (expects Play Mode for load).</summary>
public static class LevelProjectEditorMenu
{
    const string MenuRoot = "Level Editor/";

    [MenuItem(MenuRoot + "Export Level Folder…", false, 10)]
    static void ExportLevelJson()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Export level",
                "Enter Play Mode first — the exporter reads objects from the running level scene.",
                "OK");
            return;
        }

        string startDir = System.IO.Path.Combine(Application.dataPath, "..", "Levels");
        if (!System.IO.Directory.Exists(startDir))
            startDir = Application.dataPath;

        string parentFolder = EditorUtility.SaveFolderPanel(
            "Choose parent folder — a new folder will be created here with level.json and assets",
            startDir,
            "");

        if (string.IsNullOrEmpty(parentFolder))
            return;

        string exportFolderName = $"LevelExport_{DateTime.Now:yyyyMMdd_HHmmss}";
        string exportDir = Path.Combine(parentFolder, exportFolderName);
        Directory.CreateDirectory(exportDir);
        string jsonPath = Path.Combine(exportDir, LevelProjectService.DefaultLevelFileName);

        LevelProjectService.SaveLevelToPath(jsonPath, Path.GetFileNameWithoutExtension(exportFolderName));
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Level export",
            "Level saved.\n\n" + jsonPath + "\n\nSprites (if any) are in BundledAssets inside this folder.",
            "OK");
    }

    [MenuItem(MenuRoot + "Import Level JSON…", false, 11)]
    static void ImportLevelJson()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Import level",
                "Start Play Mode first — the level is loaded into the running editor scene.",
                "OK");
            return;
        }

        string startDir = System.IO.Path.Combine(Application.dataPath, "..", "Levels");
        if (!System.IO.Directory.Exists(startDir))
            startDir = Application.dataPath;

        string path = EditorUtility.OpenFilePanel("Select level.json (inside an exported level folder)", startDir, "json");
        if (string.IsNullOrEmpty(path))
            return;

        LevelProjectService.LoadLevelFromPath(path);
        EditorUtility.DisplayDialog("Level import", "Level loaded from:\n" + path, "OK");
    }
}
