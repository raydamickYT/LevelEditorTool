using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This action is used to trigger multiple actions at the same time. For example the user deletes multiple gameobjects at the same time and we need to undo that
/// this composite action will allow us to do that
/// </summary>
public class CompositeAction : IUndoableAction
{
    private readonly List<IUndoableAction> actions;
    private readonly string debugLabel;

    public CompositeAction(IEnumerable<IUndoableAction> actions, string debugLabel = "Composite Action")
    {
        this.actions = actions.ToList();
        this.debugLabel = debugLabel;
    }

    public string DebugLabel => debugLabel;

    public void Undo()
    {
        for (int i = actions.Count - 1; i >= 0; i--)
        {
            actions[i].Undo();
        }
    }

    public void Redo()
    {
        for (int i = 0; i < actions.Count; i++)
        {
            actions[i].Redo();
        }
    }
}
