using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CopyPasteManager : MonoBehaviour
{
    private List<LevelObject.Memento> clipBoard = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.Instance.AddDelegateListener(ShortcutBindingEvents.OnCommandTriggered, (Action<EditorCommand>)HandleCommand);
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
            case EditorCommand.Duplicate:
                Duplicate();
                break;
            case EditorCommand.Cut:
                Cut();
                break;
        }
    }

    private void Copy()
    {
        //if nothing is selected dont copy
        if (!EditorBlackBoard.HasSelection && !EditorBlackBoard.HasMultiSelection) return;

        clipBoard.Clear();



        foreach (var item in EditorBlackBoard.CurrentSelectedLevelObjects)
        {
            if (item == null) continue;

            clipBoard.Add(item.Save());
        }
    }


    private void Paste()
    {
        if (clipBoard.Count == 0) return;

        List<GameObject> selectionBeforePaste = new();

        foreach (var item in EditorBlackBoard.CurrentSelectedLevelObjects)
        {
            if (item == null) continue;

            selectionBeforePaste.Add(item.gameObject);
        }

        var pasteAction = new PasteAction(new List<LevelObject.Memento>(clipBoard), selectionBeforePaste);

        pasteAction.Execute();

        EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, pasteAction);
    }

    void Cut()
    {
        Copy();
        EventManager.Instance.TriggerDelegate(ShortcutBindingEvents.OnCommandTriggered, EditorCommand.Delete);
    }
    void Duplicate()
    {
        Copy();
        Paste();
    }
}


