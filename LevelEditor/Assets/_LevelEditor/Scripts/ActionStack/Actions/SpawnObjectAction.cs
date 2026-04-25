using UnityEngine;

public class SpawnObjectAction : IUndoableAction, IEditorCommand
{
    string label;
    public SpawnObjectAction(string label = "SpawnObject")
    {
        this.label = label;
    }
    public string DebugLabel => label;

    public void Execute()
    {
        throw new System.NotImplementedException();
    }

    public void Redo()
    {
        throw new System.NotImplementedException();
    }

    public void Undo()
    {
        throw new System.NotImplementedException();
    }
}
