using System.Collections.Generic;
using TMPro;
using UnityEngine;
/// <summary>
/// This class is used to undo the user action: spawn object in scene.
/// To do that, this class needs to know:
/// - which object's memento it's spawning/despawning
///
/// </summary>
public class SpawnObjectAction : IUndoableAction, IEditorCommand
{
    string label;
    string assetID;
    GameObject spawnedObject;
    GameObject prefabGameObject;
    LevelObject.Memento spawnedState;
    bool hasExecuted = false;
    public SpawnObjectAction(GameObject gameObject, GameObject prefab, string assetID, string label = "SpawnObject")
    {
        prefabGameObject = prefab;
        spawnedObject = gameObject;
        this.label = label;
        this.assetID = assetID;
    }
    public string DebugLabel => label;

    public void Execute()
    {
        if (hasExecuted) return;

        if (spawnedObject == null) { Debug.LogWarning("No Spawned object found"); return; }

        if (!spawnedObject.TryGetComponent(out LevelObject levelObject))
        {
            levelObject = spawnedObject.AddComponent<LevelObject>();
        }

        levelObject.PrefabReference = prefabGameObject;

        if (!string.IsNullOrEmpty(assetID))
            levelObject.AssetID = assetID;

        spawnedState = levelObject?.Save();

        LevelObjectsRoot.Instance.AddObjectToLevelObjectRoot(spawnedObject);
        ObjectRegistry.OnObjectCreated(levelObject);

        //object hierarchy menu
        var change = new HierarchyChange(levelObject, HierarchyChangeType.Added);
        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RefreshMenu, new List<HierarchyChange> { change });

        hasExecuted = true;
    }

    public void Redo()
    {
        spawnedObject = LevelObjectSpawner.Spawn(spawnedState, true);
    }

    public void Undo()
    {
        if (spawnedObject == null) return;

        LevelObjectSpawner.Despawn(spawnedObject);
        spawnedObject = null;
    }
}
