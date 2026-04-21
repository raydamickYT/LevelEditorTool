using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//This is the caretaker of the undo stack, it will be responsible for managing the stack and providing the interface for undo and redo operations.
/// <summary>
/// Should keep track of:
/// - undo stack
/// - redo stack
/// - registering new actions
/// - performing undo and redo operations
/// - clearing the stacks when necessary (e.g., when a new action is performed after undoing)
/// </summary>
public class UndoManager : MonoBehaviour
{
    public bool UndoTestBool;
    private Stack<IUndoableAction> undoStack = new();
    private Stack<IUndoableAction> redoStack = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.Instance.AddDelegateListener(ActionStackEvents.RegisterAction, (Action<IUndoableAction>)PushAction);
        EventManager.Instance.AddUnityEventListener(ActionStackEvents.Undo, Undo);
        EventManager.Instance.AddUnityEventListener(ActionStackEvents.Redo, Redo);
    }

    void Update()
    {
        if(UndoTestBool)
        {
            UndoTestBool = false;
            Undo();
        }
    }

    void OnDestroy()
    {
        Clear();
    }

    void Undo()
    {
        if(undoStack.Count == 0) return;
        
        var action = undoStack.Pop();
        action.Undo();

        redoStack.Push(action);
    }

    void Redo()
    {
        if(redoStack.Count == 0) return;

        var action = redoStack.Pop();
        action.Redo();

        undoStack.Push(action);
    }

    void PushAction(IUndoableAction action)
    {
        undoStack.Push(action);
        redoStack.Clear(); //new history branch, clear redo stack
    }

    void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
    }
}

public static class ActionStackEvents
{
    public const string RegisterAction = "RegisterAction";
    public const string UnregisterAction = "UnregisterAction";
    public const string Undo = "Undo";
    public const string Redo = "Redo";
    public const string Clear = "Clear";    
}
