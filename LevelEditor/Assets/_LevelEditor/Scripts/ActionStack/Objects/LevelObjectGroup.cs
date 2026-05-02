using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelObjectGroup : LevelObject
{
    public override bool IsGroup => true;
    private List<LevelObject> levelObjects = new();
    public IEnumerable<LevelObject> LevelObjects => levelObjects;

    public class GroupMemento : Memento
    {
        public List<LevelObject> Children { get; }

        public GroupMemento(LevelObjectGroup group)
            : base(group.transform, group.PrefabReference, group.ObjectID)
        {
            Children = group.levelObjects.ToList();
        }
    }

    public void AddChild(LevelObject child)
    {
        if (child == null) return;
        if (levelObjects.Contains(child)) return;

        levelObjects.Add(child);
    }

    public void RemoveChild(LevelObject child)
    {
        if (child == null) return;

        levelObjects.Remove(child);
    }
    public void ClearChildren()
    {
        levelObjects.Clear();
    }

    public override Memento Save()
    {
        return new GroupMemento(this);
    }

    public override void Restore(Memento m)
    {
        base.Restore(m);

        if (m is not GroupMemento groupMemento)
            return;

        levelObjects.Clear();

        foreach (LevelObject child in groupMemento.Children)
        {
            AddChild(child);
        }
    }
}
