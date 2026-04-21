using UnityEngine;


/// <summary>
/// this class should be called whenever the transform of an object is changed. responsible for
/// - saving before and after states
/// - executing undo and redo operations on the target object
/// </summary>
public class TransformAction : IUndoableAction
{
    LevelObject target;
    LevelObject.Memento beforeState;
    LevelObject.Memento afterState;

    public TransformAction(LevelObject target)
    {
        this.target = target;
        beforeState = target.Save();
    }

    public string DebugLabel => target.name;

    public void CaptureAfterState()
    {
        afterState = target.Save();
    }

    public void Redo()
    {
        target.Restore(afterState);
    }

    public void Undo()
    {
        target.Restore(beforeState);
    }

    public bool HasChanged()
    {
        if (beforeState.Position != afterState.Position)
        {
            Debug.Log("Position changed");
            return true;
        }
        if (beforeState.Rotation != afterState.Rotation)
        {
            Debug.Log("Rotation changed");
            return true;
        }
        if (beforeState.Scale != afterState.Scale)
        {
            Debug.Log("Scale changed");
            return true;
        }
        return false;
    }
}
