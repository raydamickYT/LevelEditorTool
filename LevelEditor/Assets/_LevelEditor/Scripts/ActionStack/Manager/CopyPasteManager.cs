using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CopyPasteManager : MonoBehaviour
{
    private List<LevelObject.Memento> clipBoard = new();
    private List<GameObject> copiedGameObjects = new();
    private HashSet<LevelObject> selectedObjects = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.Instance.AddDelegateListener(ShortcutBindingEvents.OnCommandTriggered, (Action<EditorCommand>)HandleCommand);
        EventManager.Instance.AddDelegateListener(SelectionEvents.OnSelectionChanged, (Action<HashSet<SelectableTargetData>>)UpdateCache);
    }

    private void HandleCommand(EditorCommand command)
    {
        switch (command)
        {
            case EditorCommand.Copy:
                Copy();
                break;

            case EditorCommand.Paste:
                Paste();
                break;
        }
    }

    private void Copy()
    {
        clipBoard.Clear();

        foreach (var item in selectedObjects)
        {
            if (item == null) continue;

            clipBoard.Add(item.Save());
            copiedGameObjects.Add(item.gameObject);
        }
    }


    private void Paste()
    {
        var pasteAction = new PasteAction(clipBoard, copiedGameObjects);

        pasteAction.Execute();

        EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, pasteAction);
    }

    void UpdateCache(HashSet<SelectableTargetData> data)
    {
        selectedObjects.Clear();

        if (data == null || data.Count == 0) //this is necessary since this function will also be called if the selection is emptied.
        {
            return;
        }

        foreach (var item in data)
        {
            if (item.BaseObject == null)
            {
                Debug.Log("BaseObject is null");
                continue;
            }

            var LvlObj = item.BaseObject.GetComponent<LevelObject>();
            if (LvlObj == null) continue;

            selectedObjects.Add(LvlObj);
        }
    }
}


