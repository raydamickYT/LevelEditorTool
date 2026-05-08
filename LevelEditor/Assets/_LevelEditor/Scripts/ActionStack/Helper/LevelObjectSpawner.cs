using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class LevelObjectSpawner
{
    public static GameObject Spawn(LevelObject.Memento memento, bool preserveObjectID = false, Transform parent = null, bool isParent = false)
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


        Sprite spriteToUse = null;

        if (!string.IsNullOrEmpty(memento.AssetID))
        {
            spriteToUse = AssetRuntimeLoader.LoadSpriteByAssetID(memento.AssetID);

        }

        if (spriteToUse == null)
        {
            spriteToUse = memento.Sprite;
        }

        if (spriteToUse != null && spawnedObject.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.sprite = spriteToUse;

            //box collider setup
            spawnedObject.TryGetComponent(out BoxCollider2D boxCollider2D);
            
            Bounds bounds = spriteRenderer.sprite.bounds;
            boxCollider2D.size = bounds.size;
            boxCollider2D.offset = bounds.center;
        }
        else
        {
            Debug.LogWarning($"No sprite found for spawned object. AssetID: {memento.AssetID}");
        }


        spawnedObject.SetActive(true);
        spawnedObject.hideFlags = HideFlags.None;

        //Add a parent if given
        if (parent != null)
            spawnedObject.transform.SetParent(parent, true);
        else
            LevelObjectsRoot.Instance.AddObjectToLevelObjectRoot(spawnedObject);

        spawnedObject.transform.localScale = memento.Scale;

        LevelObject levelObject = spawnedObject.GetComponent<LevelObject>();
        if (levelObject == null)
        {
            Debug.LogWarning("Spawned object has no LevelObject component: " + spawnedObject.name);
            return spawnedObject;
        }

        levelObject.PrefabReference = memento.PrefabReference;

        if (!string.IsNullOrEmpty(memento.AssetID))
            levelObject.AssetID = memento.AssetID;

        if (preserveObjectID)
        {
            levelObject.ObjectID = memento.ObjectID;
            ObjectRegistry.RegisterObject(spawnedObject, memento.ObjectID);
        }
        else
        {
            ObjectRegistry.OnObjectCreated(levelObject);
        }

        //object hierarchy menu
        Debug.LogWarning("this is a parent " + isParent);
        HierarchyChangeType type = isParent ? HierarchyChangeType.AddedParent : HierarchyChangeType.Added;
        var change = new HierarchyChange(levelObject, type);

        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RefreshMenu, new List<HierarchyChange> { change });

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

        //object hierarchy menu
        var change = new HierarchyChange(levelObject, HierarchyChangeType.Removed);
        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RefreshMenu, new List<HierarchyChange> { change });

        LevelObjectsRoot.Instance.RemoveChildFromParent(obj);

        Object.Destroy(obj);
    }
}
