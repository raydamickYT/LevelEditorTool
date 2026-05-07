using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelObjectGroup : LevelObject
{
    public override bool IsGroup => true;
    private List<LevelObject> levelObjects = new();
    public IEnumerable<LevelObject> LevelObjects => levelObjects;
    private List<SelectableTargetData> selectableTargetDatas = new();
    public IEnumerable<SelectableTargetData> SelectableTargetDatas => selectableTargetDatas;

    public class GroupMemento : Memento
    {
        public List<LevelObject> Children { get; }

        public GroupMemento(LevelObjectGroup thisObject)
            : base(thisObject.transform, thisObject.PrefabReference, thisObject.ObjectID, null, null, thisObject.levelObjectGroup) //there's two nulls because this object does not need a sprite nor an assetID
        {
            Children = thisObject.levelObjects.ToList();
        }
    }

    void Start()
    {
        if (this.PrefabReference == null)
        {
            PrefabReference = this.gameObject;
        }
    }

    public void AddChild(LevelObject child)
    {
        if (child == null) return;
        if (child.HasParent) return;
        if (levelObjects.Contains(child)) return;


        levelObjects.Add(child);
        child.UpdateParent(this);
    }

    public void RebuildChildrenFromTransform()
    {
        ClearChildren();
        foreach (Transform chidTransform in transform)
        {
            if (chidTransform.TryGetComponent(out LevelObject component))
            {
                component.hierarchyObjectItem = null;
                ObjectRegistry.OnObjectCreated(component);
                levelObjects.Add(component);
            }

            if (component.transform.TryGetComponent(out SelectableObject selectableObject))
            {
                var data = selectableObject.TargetData;
                if (data != null)
                    selectableTargetDatas.Add(data);
            }


            component.UpdateParent(this);
        }
    }

    public void SaveSelectableTargetData(IEnumerable<SelectableTargetData> data)
    {
        if (data == null) return;

        selectableTargetDatas = data.Where(x => x != null).ToList();

        if (selectableTargetDatas.Count == 0)
            Debug.LogWarning("no selectableTargetData was found");
    }

    public void RemoveChild(LevelObject child)
    {
        if (child == null) return;

        levelObjects.Remove(child);
        child.ClearParent();
    }

    public void ClearChildren()
    {
        if (selectableTargetDatas.Count >= 1)
            selectableTargetDatas.Clear();

        if (levelObjects.Count == 0) return;
        foreach (LevelObject child in levelObjects)
        {
            child.ClearParent();
        }
        levelObjects.Clear();
    }

    public override Memento Save()
    {
        return new GroupMemento(this);
    }

    //only used when reassigning selection of the group and the pivot has been changed
    public void UpdateCenterWithoutMovingChildren()
    {
        if (selectableTargetDatas == null || selectableTargetDatas.Count == 0) return;

        var newPos = SelectionBoundsCalculator.GetSelectionBoundsCenter(selectableTargetDatas.ToHashSet());
        if (Vector3.Distance(newPos, transform.position) < 0.001f)
            return;


        List<LevelObject> children = LevelObjects
            .Where(x => x != null)
            .ToList();

        List<Vector3> worldPositions = new();
        List<Quaternion> worldRotations = new();
        List<Vector3> worldScales = new();


        foreach (LevelObject child in children)
        {
            Transform childTransform = child.transform;

            worldPositions.Add(childTransform.position);
            worldRotations.Add(childTransform.rotation);
            worldScales.Add(childTransform.lossyScale);
        }

        transform.position = newPos;

        for (int i = 0; i < children.Count; i++)
        {
            Transform childTransform = children[i].transform;

            childTransform.position = worldPositions[i];
            childTransform.rotation = worldRotations[i];

            SetGlobalScale(childTransform, worldScales[i]);
        }
    }

    private void SetGlobalScale(Transform target, Vector3 globalScale)
    {
        Transform parent = target.parent;

        if (parent == null)
        {
            target.localScale = globalScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;

        target.localScale = new Vector3(
            parentScale.x != 0 ? globalScale.x / parentScale.x : target.localScale.x,
            parentScale.y != 0 ? globalScale.y / parentScale.y : target.localScale.y,
            parentScale.z != 0 ? globalScale.z / parentScale.z : target.localScale.z
        );
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
