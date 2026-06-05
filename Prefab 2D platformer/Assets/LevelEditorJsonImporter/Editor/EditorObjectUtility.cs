using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    static class EditorObjectUtility
    {
        public static T[] FindObjectsInOpenScenes<T>() where T : Object
        {
#if UNITY_2022_2_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }
    }
}
