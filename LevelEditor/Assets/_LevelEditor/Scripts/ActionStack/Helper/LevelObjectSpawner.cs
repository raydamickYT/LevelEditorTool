using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class LevelObjectSpawner
{
    /// <summary>Spawns a root memento and any nested group children (clipboard / undo / paste).</summary>
    public static GameObject SpawnMementoWithDescendants(LevelObject.Memento memento, bool preserveObjectId, bool deferHierarchyNotification = false)
    {
        LevelObjectGroup parentGroup = null;
        Transform parentTransform = null;
        if (memento != null && memento.LevelObjectGroup != null)
        {
            parentGroup = memento.LevelObjectGroup;
            parentTransform = parentGroup.transform;
        }

        return SpawnRecursive(memento, parentTransform, parentGroup, preserveObjectId, deferHierarchyNotification);
    }

    static GameObject SpawnRecursive(LevelObject.Memento memento, Transform parentTransform, LevelObjectGroup parentGroup, bool preserveObjectId, bool deferHierarchyNotification)
    {
        if (memento == null)
            return null;

        bool isParent = memento is LevelObjectGroup.GroupMemento;
        // Defer hierarchy refresh until after AddChild — otherwise levelObject.levelObjectGroup is still null and UI rows parent to Content instead of ChildContainer.
        GameObject go = Spawn(memento, preserveObjectId, parentTransform, isParent, deferHierarchyNotification: true);
        if (go == null)
            return null;

        if (!go.TryGetComponent(out LevelObject levelObject))
            return go;

        if (parentGroup != null && !ReferenceEquals(levelObject, parentGroup))
            parentGroup.AddChild(levelObject);

        if (!deferHierarchyNotification)
            NotifyHierarchyForSpawned(levelObject, isParent);

        if (memento is LevelObjectGroup.GroupMemento gm && levelObject is LevelObjectGroup childGroup && gm.ChildMementos != null)
        {
            foreach (LevelObject.Memento childMem in gm.ChildMementos)
                SpawnRecursive(childMem, childGroup.transform, childGroup, preserveObjectId, deferHierarchyNotification);
        }

        return go;
    }

    /// <summary>Raises <see cref="ObjectHierarchyEvents.RefreshMenu"/> for a spawned object (after <see cref="LevelObjectGroup.AddChild"/> when applicable).</summary>
    public static void NotifyHierarchyForSpawned(LevelObject levelObject, bool isParent)
    {
        if (levelObject == null)
            return;

        HierarchyChangeType type = isParent ? HierarchyChangeType.AddedParent : HierarchyChangeType.Added;
        var change = new HierarchyChange(levelObject, type);
        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RefreshMenu, new List<HierarchyChange> { change });
    }

    public static GameObject Spawn(LevelObject.Memento memento, bool preserveObjectID = false, Transform parent = null, bool isParent = false, bool deferHierarchyNotification = false)
    {
        if (memento == null)
        {
            Debug.LogError("Cannot spawn LevelObject: missing memento.");
            return null;
        }

        bool isGroupMemento = memento is LevelObjectGroup.GroupMemento;
        GameObject spawnedObject;

        if (isGroupMemento)
        {
            // Clipboard / cut: PrefabReference often points at the live scene object — after Destroy it is gone.
            // Rebuild an empty parent from memento transform data (same idea as level JSON load).
            spawnedObject = CreateGroupShellFromMemento((LevelObjectGroup.GroupMemento)memento, parent);
        }
        else
        {
            GameObject spawnPrefab = ResolveSpawnPrefab(memento);
            if (spawnPrefab == null)
            {
                Debug.LogError("Cannot spawn LevelObject: missing prefab reference.");
                return null;
            }

            spawnedObject = Object.Instantiate(
                spawnPrefab,
                memento.Position,
                memento.Rotation);
        }

        spawnedObject.SetActive(true);
        spawnedObject.hideFlags = HideFlags.None;

        if (!isGroupMemento)
        {
            if (parent != null)
                spawnedObject.transform.SetParent(parent, true);
            else
                LevelObjectsRoot.Instance.AddObjectToLevelObjectRoot(spawnedObject);

            spawnedObject.transform.localScale = memento.Scale;

            ApplySpriteFromMemento(spawnedObject, memento);
        }

        LevelObject levelObject = spawnedObject.GetComponent<LevelObject>();
        if (levelObject == null)
        {
            Debug.LogWarning("Spawned object has no LevelObject component: " + spawnedObject.name);
            return spawnedObject;
        }

        if (!string.IsNullOrEmpty(memento.ObjectName))
            spawnedObject.name = memento.ObjectName;

        levelObject.PrefabReference = isGroupMemento
            ? spawnedObject
            : ResolveSpawnPrefab(memento);

        if (!string.IsNullOrEmpty(memento.AssetID))
            levelObject.AssetID = memento.AssetID;

        levelObject.HasCollision = memento.HasCollision;
        levelObject.ApplyCollisionState();
        levelObject.ApplySortingOrder(memento);

        if (preserveObjectID)
        {
            levelObject.ObjectID = memento.ObjectID;
            ObjectRegistry.RegisterObject(spawnedObject, memento.ObjectID);
        }
        else
        {
            ObjectRegistry.OnObjectCreated(levelObject);
        }

        if (!deferHierarchyNotification)
            NotifyHierarchyForSpawned(levelObject, isParent);

        return spawnedObject;
    }

    static GameObject CreateGroupShellFromMemento(LevelObjectGroup.GroupMemento gm, Transform parent)
    {
        string groupName = string.IsNullOrWhiteSpace(gm.ObjectName) ? "EmptyParent" : gm.ObjectName;
        GameObject go = new GameObject(groupName);
        go.AddComponent<LevelObjectGroup>();
        go.AddComponent<SelectableObject>();

        Transform t = go.transform;
        if (parent == null)
        {
            LevelObjectsRoot.Instance.AddObjectToLevelObjectRoot(go);
            t.SetPositionAndRotation(gm.Position, gm.Rotation);
            t.localScale = gm.Scale;
        }
        else
        {
            t.SetParent(parent, false);
            t.SetPositionAndRotation(gm.Position, gm.Rotation);
            t.localScale = gm.Scale;
        }

        LevelObjectGroup group = go.GetComponent<LevelObjectGroup>();
        group.PrefabReference = go;

        return go;
    }

    static GameObject ResolveSpawnPrefab(LevelObject.Memento memento)
    {
        if (ObjectLibraryManager.Instance != null)
        {
            GameObject template = ObjectLibraryManager.Instance.SpawnPrefabTemplate;
            if (template != null)
                return template;
        }

        return memento?.PrefabReference;
    }

    static void ApplySpriteFromMemento(GameObject spawnedObject, LevelObject.Memento memento)
    {
        if (spawnedObject == null || memento == null)
            return;

        if (!spawnedObject.TryGetComponent(out SpriteRenderer spriteRenderer))
            return;

        Sprite spriteToUse = memento.Sprite;
        if (spriteToUse == null && !string.IsNullOrEmpty(memento.AssetID))
            spriteToUse = AssetRuntimeLoader.LoadSpriteByAssetID(memento.AssetID);

        if (spriteToUse == null)
        {
            Debug.LogWarning($"No sprite found for spawned object. AssetID: {memento.AssetID}");
            return;
        }

        spriteRenderer.sprite = spriteToUse;
    }

    public static void Despawn(GameObject obj, bool scheduleHierarchyRebuild = true)
    {
        if (obj == null)
            return;

        LevelObject[] tree = obj.GetComponentsInChildren<LevelObject>(true);
        System.Array.Sort(tree, (a, b) => GetTransformDepth(b.transform).CompareTo(GetTransformDepth(a.transform)));

        foreach (LevelObject lo in tree)
        {
            if (lo == null)
                continue;

            ObjectRegistry.DeregisterObject(lo);
        }

        LevelObjectsRoot.Instance.RemoveChildFromParent(obj);
        Object.Destroy(obj);

        if (scheduleHierarchyRebuild)
            ObjectHierarchyManager.ScheduleRebuildEntireHierarchy();
    }

    static int GetTransformDepth(Transform t)
    {
        int d = 0;
        while (t != null)
        {
            d++;
            t = t.parent;
        }

        return d;
    }
}
