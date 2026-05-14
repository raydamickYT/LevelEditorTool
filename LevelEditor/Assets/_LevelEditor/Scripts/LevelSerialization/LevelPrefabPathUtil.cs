using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

//Resolves prefab assets to/from GUIDs in the Unity Editor
public static class LevelPrefabPathUtil
{
    public static string GetPrefabAssetGuid(GameObject prefabOrInstance)
    {
#if UNITY_EDITOR
        if (prefabOrInstance == null)
            return string.Empty;

        GameObject assetObject = prefabOrInstance;
        if (PrefabUtility.IsPartOfPrefabInstance(prefabOrInstance))
        {
            UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabOrInstance);
            assetObject = source as GameObject;
        }

        if (assetObject == null)
            return string.Empty;

        string path = AssetDatabase.GetAssetPath(assetObject);
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        return AssetDatabase.AssetPathToGUID(path);
#else
        return string.Empty;
#endif
    }

    public static GameObject LoadPrefabByGuid(string guid)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(guid))
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
            return null;

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
        return null;
#endif
    }
}
