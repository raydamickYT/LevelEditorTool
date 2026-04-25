using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            float offset = 0.5f;
            var offSetPos = new Vector3(item.Position.x + offset, item.Position.y + offset, item.Position.z); //this is just to give a better visual of whats been pasted
            GameObject instantiatedGameObject = GameObject.Instantiate(item.PrefabReference, offSetPos, item.Rotation).gameObject;

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
