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
        if (levelObject != null)
            objects.Remove(levelObject.ObjectID);

        Debug.Log("object removed " + levelObject.ObjectID);
    }

    public static LevelObject GetLevelObject(int id)
    {
        if (!objects.TryGetValue(id, out GameObject levelObject)) return null;
        return levelObject.GetComponent<LevelObject>();
    }
}
