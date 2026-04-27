using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This action will allow us to undo the deletion of a gameobject
/// it needs to posses:
/// - said gameobject's info before it got removed
/// 
/// and it will:
/// - create a copy of that object on undo and regiser it to the <see cref="ObjectRegistry"/>
/// </summary>
public class DeleteAction : IUndoableAction, IEditorCommand
{
    List<GameObject> instantiatedGameObjects = new();
    List<LevelObject.Memento> beforeState;
    string debugLabel;

    public DeleteAction(IEnumerable<LevelObject.Memento> target, string debugLabel = "DeleteAction")
    {
        this.debugLabel = debugLabel;
        beforeState = target.ToList();
    }
    public string DebugLabel => debugLabel;

    public void Execute()
    {
        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, Enumerable.Empty<GameObject>());

        foreach (var state in beforeState)
        {
            LevelObject target = ObjectRegistry.GetLevelObject(state.ObjectID);

            if (target == null)
            {
                Debug.LogWarning($"DeleteAction: object not found: {state.ObjectID}");
                continue;
            }

            LevelObjectSpawner.Despawn(target.gameObject);
        }

        instantiatedGameObjects.Clear();
    }

    public void Redo()
    {
        Execute();
    }

    public void Undo()
    {
        if (instantiatedGameObjects.Count == 0)
        {
            foreach (var state in beforeState)
            {
                instantiatedGameObjects.Add(LevelObjectSpawner.Spawn(state, true));
            }
        }

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, new List<GameObject>(instantiatedGameObjects)); //reset the selection to earlier selected Items.
    }
}
