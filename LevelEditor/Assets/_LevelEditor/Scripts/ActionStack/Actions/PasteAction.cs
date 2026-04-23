using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PasteAction : IUndoableAction, IEditorCommand
{
    //before paste
    string label;
    List<LevelObject.Memento> ObjectsToPaste;

    //after paste
    List<GameObject> instantiatedGameObjects = new();
    public PasteAction(List<LevelObject.Memento> clipBoardObjects, string label = "PasteAction")
    {
        this.label = label;
        ObjectsToPaste = clipBoardObjects;
    }
    public string DebugLabel => label;

    public void Execute() //pasting
    {
        instantiatedGameObjects.Clear();

        foreach (var item in ObjectsToPaste)
        {
            GameObject instantiatedGameObject = GameObject.Instantiate(item.PrefabReference, item.Position, item.Rotation).gameObject;

            instantiatedGameObject.transform.SetParent(item.parent, true);
            instantiatedGameObject.transform.localScale = item.Scale;

            var lvlObject = instantiatedGameObject.GetComponent<LevelObject>();
            if (lvlObject == null) continue;

            lvlObject.PrefabReference = item.PrefabReference;

            ObjectRegistry.OnObjectCreated(lvlObject);

            instantiatedGameObjects.Add(instantiatedGameObject);
        }
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

        instantiatedGameObjects.Clear();
    }
}
