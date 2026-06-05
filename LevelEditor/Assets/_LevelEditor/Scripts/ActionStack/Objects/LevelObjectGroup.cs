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

    /// <summary>World positions of direct children last time this group was the only selected object (for pivot recenter).</summary>
    Dictionary<int, Vector3> childWorldPosAtLastSoleSelection;

    public class GroupMemento : Memento
    {
        /// <summary>Deep snapshot of children (no live references — safe for cut / clipboard).</summary>
        public List<Memento> ChildMementos { get; }

        public GroupMemento(LevelObjectGroup thisObject)
            : base(thisObject.transform, thisObject.PrefabReference, thisObject.ObjectID, null, null, thisObject.levelObjectGroup) //there's two nulls because this object does not need a sprite nor an assetID
        {
            thisObject.SyncChildrenFromTransform();

            ChildMementos = new List<Memento>();
            foreach (LevelObject child in thisObject.levelObjects)
            {
                if (child == null)
                    continue;

                Memento childMemento = child is LevelObjectGroup childGroup
                    ? new GroupMemento(childGroup)
                    : child.Save();

                // Duplicated / pasted children should follow the new group, not the source group.
                childMemento.LevelObjectGroup = null;
                ChildMementos.Add(childMemento);
            }
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
        if (child.HasParent && child.levelObjectGroup != this) return;
        if (levelObjects.Contains(child)) return;

        if (child.transform.parent != transform)
            child.transform.SetParent(transform, true);

        levelObjects.Add(child);
        child.UpdateParent(this);
        SyncSelectableTargetsFromChildren();
    }

    public void InsertChild(LevelObject child, int siblingIndex)
    {
        if (child == null) return;
        if (levelObjects.Contains(child))
            levelObjects.Remove(child);

        if (child.transform.parent != transform)
            child.transform.SetParent(transform, true);

        int index = Mathf.Clamp(siblingIndex, 0, levelObjects.Count);
        levelObjects.Insert(index, child);
        child.transform.SetSiblingIndex(index);
        child.UpdateParent(this);
        SyncSelectableTargetsFromChildren();
    }

    /// <summary>Rebuilds <see cref="selectableTargetDatas"/> from current <see cref="LevelObjects"/> (used by <see cref="UpdateCenterWithoutMovingChildren"/>).</summary>
    public void SyncSelectableTargetsFromChildren()
    {
        selectableTargetDatas.Clear();
        foreach (LevelObject child in levelObjects)
        {
            if (child == null)
                continue;

            if (child.TryGetComponent(out SelectableObject so) && so.TargetData != null)
                selectableTargetDatas.Add(so.TargetData);
        }
    }

    /// <summary>
    /// When the new selection is exactly one object and it is a <see cref="LevelObjectGroup"/>,
    /// recenters the group pivot if any direct child moved since this group was last sole-selected.
    /// </summary>
    public static void TryUpdatePivotWhenGroupBecomesSoleSelection(HashSet<SelectableTargetData> selection)
    {
        if (selection == null || selection.Count != 1)
            return;

        SelectableTargetData sole = null;
        foreach (SelectableTargetData d in selection)
        {
            sole = d;
            break;
        }

        if (sole?.BaseObject == null)
            return;

        LevelObjectGroup group = sole.BaseObject.GetComponent<LevelObjectGroup>();
        if (group == null)
            return;

        group.SyncSelectableTargetsFromChildren();

        if (group.HaveChildrenMovedSinceLastSoleSelection())
            group.UpdateCenterWithoutMovingChildren();

        group.RecordChildWorldPositionsForNextSoleSelection();
    }

    bool HaveChildrenMovedSinceLastSoleSelection()
    {
        if (childWorldPosAtLastSoleSelection == null || childWorldPosAtLastSoleSelection.Count == 0)
            return false;

        foreach (LevelObject child in levelObjects)
        {
            if (child == null)
                continue;

            if (!childWorldPosAtLastSoleSelection.TryGetValue(child.ObjectID, out Vector3 prev))
                return true;

            if ((child.transform.position - prev).sqrMagnitude > 1e-6f)
                return true;
        }

        return false;
    }

    void RecordChildWorldPositionsForNextSoleSelection()
    {
        childWorldPosAtLastSoleSelection ??= new Dictionary<int, Vector3>();
        childWorldPosAtLastSoleSelection.Clear();

        foreach (LevelObject child in levelObjects)
        {
            if (child == null)
                continue;

            childWorldPosAtLastSoleSelection[child.ObjectID] = child.transform.position;
        }
    }

    public void SyncChildrenFromTransform()
    {
        List<LevelObject> nextChildren = new();
        foreach (Transform childTransform in transform)
        {
            if (childTransform == null || childTransform.gameObject == null)
                continue;

            if (!childTransform.TryGetComponent(out LevelObject component))
                continue;

            component.hierarchyObjectItem = null;

            if (!ObjectRegistry.IsGameObjectRegistered(component.gameObject))
                ObjectRegistry.OnObjectCreated(component);

            nextChildren.Add(component);
        }

        foreach (LevelObject previousChild in levelObjects)
        {
            if (previousChild == null || nextChildren.Contains(previousChild))
                continue;

            previousChild.ClearParent();
        }

        levelObjects.Clear();
        foreach (LevelObject child in nextChildren)
        {
            levelObjects.Add(child);
            child.UpdateParent(this);
        }

        SyncSelectableTargetsFromChildren();
    }

    public void RebuildChildrenFromTransform()
    {
        SyncChildrenFromTransform();
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
        SyncSelectableTargetsFromChildren();
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
    }


}
