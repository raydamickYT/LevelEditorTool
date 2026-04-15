#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Property Drawer for FolderPathAttribute to allow folder selection in the Unity Inspector.
/// EXample:
/// [FolderPath]
/// public string folderpath = "Assets/MyFolder"; //you can add a default path if you want, but it will be overridden when you select a new folder.
/// </summary>

[CustomPropertyDrawer(typeof(FolderPathAttribute))]
public class FolderPathDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Verdeel de ruimte in tekstveld + knop
        Rect textRect = position;
        textRect.width -= 80;

        Rect buttonRect = position;
        buttonRect.x += textRect.width + 5;
        buttonRect.width = 75;

        // Tekstveld
        EditorGUI.PropertyField(textRect, property, label);

        // Knop
        if (GUI.Button(buttonRect, "Browse"))
        {
            string startPath = Application.dataPath;

            string path = EditorUtility.OpenFolderPanel("Choose Folder", startPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }

                property.stringValue = path;
            }
        }
    }
}
#endif
