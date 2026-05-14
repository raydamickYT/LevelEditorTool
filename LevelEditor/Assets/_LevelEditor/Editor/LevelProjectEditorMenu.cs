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

        ExportLevelFolderWindow.Show("");
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
