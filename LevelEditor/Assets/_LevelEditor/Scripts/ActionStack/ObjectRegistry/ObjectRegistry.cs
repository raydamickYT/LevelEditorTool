using System;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectRegistry
{
    public static Dictionary<int, GameObject> objects = new();
    private static int currentID = 0;

    public static void OnObjectCreated(LevelObject levelObject)
    {
        if (levelObject == null)
            Debug.LogWarning("No level object");

        objects.Add(currentID, levelObject.gameObject);
        levelObject.ObjectID = currentID;
        currentID++;
    }

    public static void RegisterObject(GameObject levelObject, int id)
    {
        if (levelObject == null)
            Debug.LogWarning("No level object");

        if (!objects.ContainsKey(id))
            objects.Add(id, levelObject);

        else
        {
            if (objects[id].gameObject != null)
                Debug.LogWarning($"{objects[id].gameObject} has the same objectID as {levelObject.name}");

            objects[id] = levelObject;
        }
    }

    public static void DeregisterObject(LevelObject levelObject)
    {
        if (levelObject == null)
            return;

        // Only remove this object's own entry. A deferred OnDestroy of an old object must never evict a
        // freshly spawned object that reused the same ObjectID after ClearAllForNewLevel reset the counter.
        if (objects.TryGetValue(levelObject.ObjectID, out GameObject stored) && stored == levelObject.gameObject)
            objects.Remove(levelObject.ObjectID);
    }

    public static void ClearAllForNewLevel()
    {
        objects.Clear();
        currentID = 0;
    }

    /// <summary>True if this GameObject is already in the registry (e.g. after level spawn — avoid assigning a second ID).</summary>
    public static bool IsGameObjectRegistered(GameObject go)
    {
        if (go == null)
            return false;

        foreach (KeyValuePair<int, GameObject> kv in objects)
        {
            if (kv.Value == go)
                return true;
        }

        return false;
    }

    public static LevelObject GetLevelObject(int id)
    {
        if (!objects.TryGetValue(id, out GameObject levelObject)) return null;
        return levelObject.GetComponent<LevelObject>();
    }
}
