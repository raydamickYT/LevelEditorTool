using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// This action controlls the pasting of objects it's supposed to control:
/// - a list of what to paste
/// - a list of what was selected before
/// - creating the objects it needs to paste
/// - removing those objects
/// - telling the selection controller that the selection needs to be updated
/// 
/// </summary>
public class PasteAction : IUndoableAction, IEditorCommand
{
    //before paste
    string label;
    List<LevelObject.Memento> ObjectsToPaste;
    private readonly List<LevelObject.Memento> pastedStates = new();
    private List<GameObject> previousSelection = new();

    //after paste
    List<GameObject> instantiatedGameObjects = new();

    public PasteAction(List<LevelObject.Memento> clipBoardObjects, List<GameObject> previousSel, string label = "PasteAction")
    {
        this.label = label;
        ObjectsToPaste = clipBoardObjects;
        previousSelection = previousSel;

    }

    public string DebugLabel => label;

    public void Execute() //pasting
    {
        if (ObjectsToPaste.Count == 0)
            Debug.LogWarning("no objects to paste");


        instantiatedGameObjects.Clear();

        bool isRedo = pastedStates.Count > 0;

        IEnumerable<LevelObject.Memento> statesToSpawn = isRedo ? pastedStates : ObjectsToPaste; //if redo true: assign pasted states, if redo false: assign objectsToPaste
        // Debug.LogWarning("objects to paste:" + statesToSpawn.ToList().Count);

        //todo: Check if it is a child with a parent and if so if it isn't already pasted (we can do this by checking if it's in the list we create)

        foreach (LevelObject.Memento item in statesToSpawn)
        {
            GameObject instantiatedGameObject = LevelObjectSpawner.SpawnMementoWithDescendants(item, isRedo, deferHierarchyNotification: true);
            if (instantiatedGameObject == null)
                continue;

            instantiatedGameObjects.Add(instantiatedGameObject);

            if (!isRedo)
            {
                LevelObject levelObject = instantiatedGameObject.GetComponent<LevelObject>();
                if (levelObject != null)
                    pastedStates.Add(levelObject.Save()); //and finally (if this were a group) saving the parent
            }
        }

        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.ScheduleRebuildEntireHierarchy);


        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, instantiatedGameObjects);
    }

    public void Redo()
    {
        Execute();
    }

    public void Undo()
    {
        foreach (var item in instantiatedGameObjects)
            LevelObjectSpawner.Despawn(item, scheduleHierarchyRebuild: false);

        ObjectHierarchyManager.ScheduleRebuildEntireHierarchy();

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, previousSelection); //reset the selection to earlier selected Items.

        instantiatedGameObjects.Clear();
    }
}
