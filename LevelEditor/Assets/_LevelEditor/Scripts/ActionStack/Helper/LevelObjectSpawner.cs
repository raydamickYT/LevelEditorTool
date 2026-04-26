using System.Runtime.InteropServices;
using UnityEngine;

public static class LevelObjectSpawner
{
    public static GameObject Spawn(LevelObject.Memento memento, bool preserveObjectID = false)
    {
        if (memento == null || memento.PrefabReference == null)
        {
            Debug.LogError("Cannot spawn LevelObject: missing memento or prefab reference.");
            return null;
        }

        GameObject spawnedObject = Object.Instantiate(
            memento.PrefabReference,
            memento.Position,
            memento.Rotation
        );

        LevelObjectsRoot.Instance.AddLevelObject(spawnedObject);
        spawnedObject.transform.localScale = memento.Scale;

        LevelObject levelObject = spawnedObject.GetComponent<LevelObject>();
        if (levelObject == null)
        {
            Debug.LogWarning("Spawned object has no LevelObject component: " + spawnedObject.name);
            return spawnedObject;
        }

        levelObject.PrefabReference = memento.PrefabReference;

        if (preserveObjectID)
        {
            levelObject.ObjectID = memento.ObjectID;
            ObjectRegistry.RegisterObject(spawnedObject, memento.ObjectID);
        }
        else
        {
            ObjectRegistry.OnObjectCreated(levelObject);
        }

        return spawnedObject;
    }

    public static void Despawn(GameObject obj)
    {
        if (obj == null)
            return;

        LevelObject levelObject = obj.GetComponent<LevelObject>();

        if (levelObject != null)
        {
            ObjectRegistry.DeregisterObject(levelObject);
        }

        LevelObjectsRoot.Instance.RemoveChildFromParent(obj);

        Object.Destroy(obj);
    }
}
