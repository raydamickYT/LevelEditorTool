using UnityEngine;

public class PasteAction : IUndoableAction
{
    string label;
    public PasteAction(string label = "PasteAction")
    {
        this.label = label;
    }
    
    public string DebugLabel => label;

    public void Redo()
    {
        throw new System.NotImplementedException();
    }

    public void Undo()
    {
        throw new System.NotImplementedException();
    }
}
