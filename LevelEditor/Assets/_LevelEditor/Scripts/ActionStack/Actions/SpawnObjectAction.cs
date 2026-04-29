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
    GameObject spawnedObject;
    GameObject prefabGameObject;
    LevelObject.Memento spawnedState;
    bool hasExecuted = false;
    public SpawnObjectAction(GameObject gameObject, GameObject prefab, string label = "SpawnObject")
    {
        prefabGameObject = prefab;
        spawnedObject = gameObject;
        this.label = label;
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

        spawnedState = levelObject?.Save();

        LevelObjectsRoot.Instance.AddLevelObject(spawnedObject);
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
