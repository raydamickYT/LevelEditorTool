using UnityEngine;

public interface IUndoableAction
{
    string DebugLabel { get; }
    void Undo();
    void Redo();
}
