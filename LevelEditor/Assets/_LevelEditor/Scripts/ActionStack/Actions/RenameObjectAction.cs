using UnityEngine;

public sealed class RenameObjectAction : IUndoableAction
{
    readonly int targetId;
    readonly string beforeName;
    readonly string afterName;

    public string DebugLabel => "Rename Object";

    public RenameObjectAction(LevelObject target, string newName)
    {
        targetId = target.ObjectID;
        beforeName = target.gameObject.name;
        afterName = newName;
        Apply(afterName);
    }

    public void Undo() => Apply(beforeName);

    public void Redo() => Apply(afterName);

    void Apply(string name)
    {
        LevelObject target = ObjectRegistry.GetLevelObject(targetId);
        if (target == null || ObjectHierarchyManager.Instance == null)
            return;

        ObjectHierarchyManager.Instance.ApplyObjectDisplayName(target, name);
    }
}
