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
        instantiatedGameObjects.Clear();

        foreach (var item in ObjectsToPaste)
        {
            var itemPos = new Vector3(item.Position.x, item.Position.y, item.Position.z);
            GameObject instantiatedGameObject = GameObject.Instantiate(item.PrefabReference, itemPos, item.Rotation).gameObject;

            instantiatedGameObject.transform.SetParent(item.parent, true);
            instantiatedGameObject.transform.localScale = item.Scale;

            var lvlObject = instantiatedGameObject.GetComponent<LevelObject>();
            if (lvlObject == null) continue;

            lvlObject.PrefabReference = item.PrefabReference;

            ObjectRegistry.OnObjectCreated(lvlObject);

            instantiatedGameObjects.Add(instantiatedGameObject);
        }

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, instantiatedGameObjects);
    }

    public void Redo()
    {
        Execute();
    }

    public void Undo()
    {
        foreach (var item in instantiatedGameObjects)
        {
            GameObject.Destroy(item);
        }

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, previousSelection); //reset the selection to earlier selected Items.

        instantiatedGameObjects.Clear();
    }
}
