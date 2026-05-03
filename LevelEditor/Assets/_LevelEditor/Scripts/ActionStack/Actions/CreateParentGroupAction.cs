using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Undoable action for creating a parent group from the current selected LevelObjects.
/// 
/// Controls:
/// - creating the scene parent object
/// - adding LevelObjectGroup + SelectableObject
/// - moving selected children under the parent
/// - filling the LevelObjectGroup
/// - refreshing the hierarchy UI
/// - replacing the selection with the parent
/// - undoing all of the above in one undo step
/// </summary>
public class CreateParentGroupAction : IUndoableAction, IEditorCommand
{
    private readonly string label;

    private GameObject parentItemObject;
    private LevelObjectGroup parentLevelObject;

    private readonly List<LevelObject> children = new();
    private readonly List<Transform> oldParents = new();
    private readonly List<int> oldSiblingIndexes = new();
    private HashSet<SelectableTargetData> selectedTargetData = new();
    private bool hasCachedInitialState;

    public string DebugLabel => label;

    public CreateParentGroupAction(string label = "Create Parent Group")
    {
        this.label = label;
    }

    public void Execute()
    {
        if (!hasCachedInitialState)
            CacheInitialStateFromCurrentSelection();

        if (children.Count < 2)
        {
            Debug.Log("Select at least 2 objects to create a parent");
            return;
        }

        CreateParentGameObject();
        SelectParent();
        MoveChildrenUnderParent();
        RefreshHierarchyAsAdded();
    }

    public void Redo()
    {
        Execute();
    }

    public void Undo()
    {
        if (parentItemObject == null || parentLevelObject == null)
            return;
        Debug.LogWarning("undo parent creation");
        SelectChildren();
        MoveChildrenBackToOldParents();
        RefreshHierarchyAsRemoved();

        LevelObjectsRoot.Instance.RemoveChildFromParent(parentItemObject);

        LevelObjectSpawner.Despawn(parentItemObject);

        parentItemObject = null;
        parentLevelObject = null;
    }

    private void CacheInitialStateFromCurrentSelection()
    {
        children.Clear();
        oldParents.Clear();
        oldSiblingIndexes.Clear();

        selectedTargetData = EditorBlackBoard.CurrentSelection
        .Where(x => x != null)
        .ToHashSet();

        List<LevelObject> selectedObjects = EditorBlackBoard.CurrentSelectedLevelObjects
            .Where(x => x != null)
            .ToList();

        foreach (LevelObject child in selectedObjects)
        {
            children.Add(child);
            oldParents.Add(child.transform.parent);
            oldSiblingIndexes.Add(child.transform.GetSiblingIndex());
        }

        hasCachedInitialState = true;
    }

    private void CreateParentGameObject()
    {
        parentItemObject = new GameObject("EmptyParent");

        parentItemObject.transform.position = SelectionBoundsCalculator.GetSelectionBoundsCenter(selectedTargetData);

        parentLevelObject = parentItemObject.AddComponent<LevelObjectGroup>();
        parentItemObject.AddComponent<SelectableObject>();

        ObjectRegistry.OnObjectCreated(parentLevelObject);

        LevelObjectsRoot.Instance.AddLevelObject(parentItemObject);
    }

    private void MoveChildrenUnderParent()
    {
        parentLevelObject.ClearChildren();

        foreach (LevelObject child in children)
        {
            if (child == null)
                continue;
            // Debug.Log("adding child to new parent" + parentLevelObject.name);

            LevelObjectsRoot.Instance.RemoveChildFromParent(child.gameObject);

            child.transform.SetParent(parentItemObject.transform, true);
            parentLevelObject.AddChild(child);
        }
        
        if (selectedTargetData.Count > 0)
            parentLevelObject.SaveSelectableTargetData(selectedTargetData);
    }

    private void MoveChildrenBackToOldParents()
    {
        parentLevelObject.ClearChildren();

        for (int i = 0; i < children.Count; i++)
        {
            LevelObject child = children[i];

            if (child == null)
                continue;

            child.transform.SetParent(oldParents[i], true);
            child.transform.SetSiblingIndex(oldSiblingIndexes[i]);

            LevelObjectsRoot.Instance.AddLevelObject(child.gameObject);
        }
    }

    private void SelectParent()
    {
        EventManager.Instance.TriggerDelegate(
            SelectionEvents.ReplaceSelectionWithObject,
            new List<GameObject> { parentItemObject }
        );
    }

    private void SelectChildren()
    {
        EventManager.Instance.TriggerDelegate(
            SelectionEvents.ReplaceSelectionWithObject,
            children
                .Where(x => x != null)
                .Select(x => x.gameObject)
                .ToList()
        );
    }

    private void RefreshHierarchyAsAdded()
    {
        EventManager.Instance.TriggerDelegate(
            ObjectHierarchyEvents.RefreshMenu,
            new List<HierarchyChange>
            {
                new HierarchyChange(parentLevelObject, HierarchyChangeType.AddedParent)
            }
        );
    }

    private void RefreshHierarchyAsRemoved()
    {
        EventManager.Instance.TriggerDelegate(
            ObjectHierarchyEvents.RefreshMenu,
            new List<HierarchyChange>
            {
                new HierarchyChange(parentLevelObject, HierarchyChangeType.Removed)
            }
        );
    }
}