using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Play Mode: choose project folder name under a parent directory, then export level.json + assets.</summary>
public sealed class ExportLevelFolderWindow : EditorWindow
{
    string parentFolder = "";
    string projectFolderName = "";

    public static void Show(string parentDirectory)
    {
        ExportLevelFolderWindow w = CreateInstance<ExportLevelFolderWindow>();
        w.parentFolder = parentDirectory ?? "";
        w.projectFolderName = $"Level_{DateTime.Now:yyyyMMdd_HHmmss}";
        w.titleContent = new GUIContent("Export level folder");
        w.minSize = new Vector2(420, 140);
        w.ShowUtility();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Parent folder", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(string.IsNullOrEmpty(parentFolder) ? "(not chosen)" : parentFolder, GUILayout.Height(18));

        if (GUILayout.Button("Choose parent folder…", GUILayout.Height(22)))
        {
            string start = string.IsNullOrEmpty(parentFolder) ? Application.dataPath : parentFolder;
            string picked = EditorUtility.SaveFolderPanel("Choose parent folder for export", start, "");
            if (!string.IsNullOrEmpty(picked))
                parentFolder = picked;
        }

        EditorGUILayout.Space(6);
        projectFolderName = EditorGUILayout.TextField("Project folder name", projectFolderName);

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(parentFolder) || string.IsNullOrWhiteSpace(projectFolderName)))
        {
            if (GUILayout.Button("Export", GUILayout.Height(26)))
                DoExport();
        }
    }

    void DoExport()
    {
        string safeName = SanitizeFolderName(projectFolderName);
        string exportDir = Path.Combine(parentFolder, safeName);
        if (Directory.Exists(exportDir))
        {
            if (!EditorUtility.DisplayDialog(
                    "Folder exists",
                    $"The folder already exists:\n{exportDir}\n\nOverwrite level.json and bundled assets inside it?",
                    "Overwrite",
                    "Cancel"))
                return;
        }
        else
            Directory.CreateDirectory(exportDir);

        string jsonPath = Path.Combine(exportDir, LevelProjectService.DefaultLevelFileName);
        LevelProjectService.SaveLevelToPath(jsonPath, safeName);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Level export",
            "Level saved.\n\n" + jsonPath,
            "OK");
        Close();
    }

    static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "LevelExport";

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        name = name.Trim();
        return string.IsNullOrEmpty(name) ? "LevelExport" : name;
    }
}
