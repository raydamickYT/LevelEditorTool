using System;
using System.Collections.Generic;
using UnityEngine;

public class CopyPasteManager : MonoBehaviour
{
    private List<LevelObject.Memento> clipBoard;
    private HashSet<LevelObject> selectedObjects;
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
            var levelObject = item.GetComponent<LevelObject>();
            if (levelObject == null) continue;

            clipBoard.Add(levelObject.Save());
        }
    }


    private void Paste()
    {

    }

    void UpdateCache(HashSet<SelectableTargetData> data)
    {
        foreach (var item in data)
        {
            var LvlObj = item.BaseObject.GetComponent<LevelObject>();
            selectedObjects.Add(LvlObj);
        }
    }
}


