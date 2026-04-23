using System;
using System.Collections.Generic;
using UnityEngine;

public class CopyPasteManager : MonoBehaviour
{
    private List<LevelObject.Memento> clipBoard;
    private HashSet<LevelObject> selectedObjects = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.Instance.AddDelegateListener(ShortcutBindingEvents.OnCommandTriggered, (Action<EditorCommand>)HandleCommand);
        EventManager.Instance.AddDelegateListener(SelectionEvents.OnSelectionChanged, (Action<HashSet<SelectableTargetData>>)UpdateCache);
    }

    private void HandleCommand(EditorCommand command)
    {
        Debug.Log("command: " + command.ToString());
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
        Debug.Log("copy");
        clipBoard.Clear();

        foreach (var item in selectedObjects)
        {
            var levelObject = item.GetComponent<LevelObject>();
            if (levelObject == null) continue;

            clipBoard.Add(levelObject.Save());
        }
    }


    private void Paste()
    {
        Debug.Log("paste");

        var pasteAction = new PasteAction(clipBoard);
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


