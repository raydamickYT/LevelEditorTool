using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HierarchyReparentAction : IUndoableAction
{
    const bool EnableDebugLogs = false;

    struct HierarchyState
    {
        public int TargetID;
        public int ParentID;
        public int SiblingIndex;
        public LevelObject Target;
        public LevelObject Parent;
        public Transform ParentTransform;
    }

    readonly List<HierarchyState> beforeStates = new List<HierarchyState>();
    readonly List<HierarchyState> afterStates = new List<HierarchyState>();
    readonly List<int> targetIDs = new List<int>();

    public HierarchyReparentAction(LevelObject target)
        : this(target != null ? new[] { target } : null)
    {
    }

    public HierarchyReparentAction(IEnumerable<LevelObject> targets)
    {
        if (targets == null)
            return;

        foreach (LevelObject target in targets)
        {
            if (target == null)
                continue;

            targetIDs.Add(target.ObjectID);
            beforeStates.Add(CaptureState(target));
        }

        LogStates("capture before", beforeStates);
    }

    public string DebugLabel => targetIDs.Count > 1 ? "Reparent Multiple Objects" : "Reparent Object";

    public void CaptureAfterState()
    {
        afterStates.Clear();
        foreach (int targetID in targetIDs)
            afterStates.Add(CaptureState(ObjectRegistry.GetLevelObject(targetID)));

        LogStates("capture after", afterStates);
    }

    public bool HasChanged()
    {
        if (beforeStates.Count != afterStates.Count)
            return true;

        for (int i = 0; i < beforeStates.Count; i++)
        {
            if (beforeStates[i].Parent != afterStates[i].Parent
                || beforeStates[i].ParentTransform != afterStates[i].ParentTransform
                || beforeStates[i].ParentID != afterStates[i].ParentID
                || beforeStates[i].SiblingIndex != afterStates[i].SiblingIndex)
            {
                return true;
            }
        }

        return false;
    }

    public void Undo()
    {
        LogStates("undo apply", beforeStates);
        ApplyStates(beforeStates);
    }

    public void Redo()
    {
        LogStates("redo apply", afterStates);
        ApplyStates(afterStates);
    }

    static HierarchyState CaptureState(LevelObject levelObject)
    {
        HierarchyState state = new HierarchyState
        {
            TargetID = levelObject != null ? levelObject.ObjectID : -1,
            ParentID = -1,
            SiblingIndex = 0,
            Target = levelObject,
            Parent = null,
            ParentTransform = levelObject != null ? levelObject.transform.parent : null
        };

        if (levelObject == null)
            return state;

        LevelObject parent = levelObject.levelObjectGroup != null
            ? levelObject.levelObjectGroup
            : GetLevelObjectParent(levelObject);

        if (parent != null)
        {
            state.ParentID = parent.ObjectID;
            state.Parent = parent;
            state.ParentTransform = parent.transform;

            if (parent is LevelObjectGroup group)
            {
                List<LevelObject> children = group.LevelObjects.ToList();
                int groupIndex = children.IndexOf(levelObject);
                state.SiblingIndex = groupIndex >= 0 ? groupIndex : levelObject.transform.GetSiblingIndex();
            }
            else
                state.SiblingIndex = levelObject.transform.GetSiblingIndex();
        }
        else
            state.SiblingIndex = levelObject.transform.GetSiblingIndex();

        return state;
    }

    static void ApplyStates(IEnumerable<HierarchyState> states)
    {
        if (states == null)
            return;

        List<HierarchyState> orderedStates = states
            .OrderBy(state => state.ParentID)
            .ThenBy(state => state.SiblingIndex)
            .ToList();

        foreach (HierarchyState state in orderedStates)
        {
            LevelObject target = state.Target != null
                ? state.Target
                : ObjectRegistry.GetLevelObject(state.TargetID);
            if (target == null)
                continue;

            LevelObject parent = state.Parent != null
                ? state.Parent
                : state.ParentID >= 0
                    ? ObjectRegistry.GetLevelObject(state.ParentID)
                    : null;

            if (parent == null
                && state.ParentTransform != null
                && state.ParentTransform.TryGetComponent(out LevelObject parentFromTransform))
            {
                parent = parentFromTransform;
            }

            if (EnableDebugLogs)
            {
                string targetName = target != null ? target.name : "null";
                string parentName = parent != null
                    ? parent.name
                    : state.ParentTransform != null
                        ? state.ParentTransform.name + " (transform)"
                        : "ROOT";
                Debug.Log($"[HierarchyReparentAction] apply {targetName} -> {parentName} @ {state.SiblingIndex}");
            }

            MoveLevelObjectToContainer(target, parent, state.SiblingIndex);
        }

        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RebuildEntireHierarchy);
    }

    static void MoveLevelObjectToContainer(LevelObject dragged, LevelObject newParent, int targetSiblingIndex)
    {
        if (dragged == null)
            return;

        if (newParent != null && (newParent == dragged || IsDescendantOf(newParent, dragged)))
            return;

        LevelObjectGroup oldParentGroup = dragged.levelObjectGroup;
        if (oldParentGroup != null)
            oldParentGroup.RemoveChild(dragged);
        else
            LevelObjectsRoot.Instance.RemoveChildFromParent(dragged.gameObject);

        LevelObjectGroup newParentGroup = newParent as LevelObjectGroup;
        if (newParent != null)
        {
            LevelObjectsRoot.Instance.RemoveChildFromParent(dragged.gameObject);
            dragged.transform.SetParent(newParent.transform, true);
            targetSiblingIndex = Mathf.Clamp(targetSiblingIndex, 0, newParent.transform.childCount - 1);
            dragged.transform.SetSiblingIndex(targetSiblingIndex);

            if (newParentGroup != null)
            {
                newParentGroup.InsertChild(dragged, targetSiblingIndex);
                newParentGroup.UpdateCenterWithoutMovingChildren();
            }
            else
                dragged.ClearParent();
        }
        else
        {
            LevelObjectsRoot.Instance.AddObjectToLevelObjectRoot(dragged.gameObject);
            targetSiblingIndex = Mathf.Clamp(targetSiblingIndex, 0, LevelObjectsRoot.Instance.RootTransform.childCount - 1);
            dragged.transform.SetSiblingIndex(targetSiblingIndex);
            LevelObjectsRoot.Instance.RebuildRootObjectsFromTransform();
        }

        if (oldParentGroup != null && oldParentGroup != newParentGroup)
            oldParentGroup.UpdateCenterWithoutMovingChildren();
    }

    static LevelObject GetLevelObjectParent(LevelObject levelObject)
    {
        if (levelObject == null || levelObject.transform.parent == null)
            return null;

        if (levelObject.transform.parent.TryGetComponent(out LevelObject parent))
            return parent;

        return null;
    }

    static void LogStates(string label, List<HierarchyState> states)
    {
        if (!EnableDebugLogs)
            return;

        if (states == null)
        {
            Debug.Log($"[HierarchyReparentAction] {label}: null states");
            return;
        }

        Debug.Log($"[HierarchyReparentAction] {label}: count={states.Count}");
        foreach (HierarchyState state in states)
        {
            string targetName = state.Target != null
                ? state.Target.name
                : "id:" + state.TargetID;
            string parentName = state.Parent != null
                ? state.Parent.name
                : state.ParentTransform != null
                    ? state.ParentTransform.name + " (transform)"
                    : "ROOT";

            Debug.Log($"[HierarchyReparentAction] {label}: {targetName} parent={parentName} parentId={state.ParentID} sibling={state.SiblingIndex}");
        }
    }

    static bool IsDescendantOf(LevelObject possibleDescendant, LevelObject possibleAncestor)
    {
        if (possibleDescendant == null || possibleAncestor == null)
            return false;

        Transform current = possibleDescendant.transform.parent;
        while (current != null)
        {
            if (current == possibleAncestor.transform)
                return true;

            current = current.parent;
        }

        return false;
    }
}
