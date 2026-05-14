using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// this class controls the visual object hierarchy menu. 
/// It'll:
/// - create new buttons for new objects that are created.
/// - remove buttons for objects that are removed
/// - create unique names for new objects.
/// </summary>
public class ObjectHierarchyManager : MonoBehaviour
{
    private HashSet<string> existingNames = new();
    [SerializeField] private Transform contentParent;
    [SerializeField] private HierarchyObjectItem itemPrefab;
    [SerializeField] private HierarchyParentObjectItem parentPrefab;
    private readonly Dictionary<LevelObject, HierarchyObjectItem> items = new();
    private readonly Dictionary<LevelObject, HierarchyParentObjectItem> parentItems = new();


    private void Awake()
    {
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.RefreshMenu, (Action<IEnumerable<HierarchyChange>>)Refresh);
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.RebuildEntireHierarchy, (Action)RebuildEntireHierarchyFromScene);
    }

    /// <summary>
    /// Clears all hierarchy UI and rebuilds it from <see cref="LevelObjectsRoot"/> (used after level import).
    /// </summary>
    public void RebuildEntireHierarchyFromScene()
    {
        ClearAllHierarchyVisuals();

        if (LevelObjectsRoot.Instance == null || contentParent == null)
            return;

        List<GameObject> roots = LevelObjectsRoot.Instance.GetRootLevelObjectsSnapshot();
        roots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        foreach (GameObject rootGo in roots)
        {
            if (rootGo == null)
                continue;
            LevelObject lo = rootGo.GetComponent<LevelObject>();
            if (lo == null)
                continue;

            if (lo is LevelObjectGroup group)
                AddParent(group, contentParent);
            else
                AddItem(lo);
        }
    }

    void ClearAllHierarchyVisuals()
    {
        foreach (KeyValuePair<LevelObject, HierarchyParentObjectItem> kv in parentItems.ToList())
        {
            if (kv.Value != null)
                kv.Value.ReleaseChildren(contentParent);
        }

        foreach (KeyValuePair<LevelObject, HierarchyObjectItem> kv in items.ToList())
        {
            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
        }

        items.Clear();

        foreach (KeyValuePair<LevelObject, HierarchyParentObjectItem> kv in parentItems.ToList())
        {
            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
        }

        parentItems.Clear();
        existingNames.Clear();

        ClearHierarchyItemRefsOnAllLevelObjects();
    }

    static void ClearHierarchyItemRefsOnAllLevelObjects()
    {
        foreach (LevelObject lo in UnityEngine.Object.FindObjectsByType<LevelObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (lo != null)
                lo.hierarchyObjectItem = null;
        }
    }

    public void Refresh(IEnumerable<HierarchyChange> hierarchyChangeObjects)
    {
        if (hierarchyChangeObjects == null)
            return;

        List<HierarchyChange> changes = hierarchyChangeObjects.ToList();

        if (changes.Count == 0)
            return;

        switch (changes.First().ChangeType)
        {
            case HierarchyChangeType.Added:
                foreach (HierarchyChange change in changes)
                {
                    AddItem(change.LevelObject);
                }
                break;

            case HierarchyChangeType.AddedParent:
                foreach (HierarchyChange change in changes)
                {
                    if (change.LevelObject is LevelObjectGroup group)
                        AddParent(group, contentParent);
                    else
                        Debug.LogWarning($"{change.LevelObject.name} is not a levelObjectGroup");
                }
                break;

            case HierarchyChangeType.Removed:
                foreach (HierarchyChange change in changes)
                {
                    Clear(change.LevelObject);
                }
                break;
        }
    }

    //for creating the parent button in the menu
    //**this is called by a UIbutton!**
    public void CreateGroupParentObject()
    {
        CreateParentGroupAction action = new CreateParentGroupAction();

        action.Execute();

        EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, action);
    }

    private void AddItem(LevelObject levelObject)
    {
        if (levelObject == null) return;

        if (items.ContainsKey(levelObject)) return;

        levelObject.name = GetUniqueHierarchyName(levelObject.name);

        HierarchyObjectItem item = Instantiate(itemPrefab, contentParent);

        item.Initialize(levelObject);

        items.Add(levelObject, item);
        existingNames.Add(levelObject.name);
    }

    private void AddParent(LevelObjectGroup group, Transform rowParent)
    {
        if (group == null) return;
        if (parentItems.ContainsKey(group)) return;

        Transform parentForRow = rowParent != null ? rowParent : contentParent;

        group.name = GetUniqueHierarchyName(group.name);

        HierarchyParentObjectItem parentItem = Instantiate(parentPrefab, parentForRow);
        parentItems.Add(group, parentItem);
        existingNames.Add(group.name);

        if (group.LevelObjects.ToList().Count == 0)
            group.RebuildChildrenFromTransform();

        List<HierarchyParentObjectItem> stagedNested = new();

        foreach (LevelObject child in group.LevelObjects.ToList())
        {
            if (child is LevelObjectGroup childGroup)
            {
                AddParent(childGroup, contentParent);
                if (parentItems.TryGetValue(childGroup, out HierarchyParentObjectItem nestedRow))
                    stagedNested.Add(nestedRow);
            }
            else
                GetOrCreateChild(child, contentParent);
        }

        parentItem.SetStagedNestedGroupRows(stagedNested);
        parentItem.Initialize(group);
    }

    private void Clear(LevelObject levelObject)
    {
        if (levelObject == null) return;

        if (existingNames.Contains(levelObject.name))
        {
            existingNames.Remove(levelObject.name);
        }

        //normal objects
        if (items.TryGetValue(levelObject, out var objectItem))
        {
            Destroy(objectItem.gameObject);
            items.Remove(levelObject);
            return;
        }

        //parent objects
        if (parentItems.TryGetValue(levelObject, out HierarchyParentObjectItem parentItem))
        {
            parentItem.ReleaseChildren(contentParent);

            Destroy(parentItem.gameObject);
            parentItems.Remove(levelObject);
            return;
        }
    }


    private string GetUniqueHierarchyName(string baseName)
    {
        if (!existingNames.Contains(baseName))
            return baseName;

        int index = 1;
        string candidateName;

        do
        {
            candidateName = $"{baseName} ({index})";
            index++;
        }
        while (existingNames.Contains(candidateName));

        return candidateName;
    }

    private HierarchyObjectItem GetOrCreateChild(LevelObject levelObject, Transform parent)
    {
        if (levelObject == null)
            return null;

        if (items.TryGetValue(levelObject, out HierarchyObjectItem existingItem))
        {
            if (existingItem != null)
            {
                levelObject.hierarchyObjectItem = existingItem;
                return existingItem;
            }

            items.Remove(levelObject);
        }

        HierarchyObjectItem item = Instantiate(itemPrefab, parent);
        item.Initialize(levelObject);

        items.Add(levelObject, item);
        existingNames.Add(levelObject.name);

        return item;
    }
}

public static class ObjectHierarchyEvents
{
    public const string RefreshMenu = "RefreshMenu";

    /// <summary>Parameterless: rebuild the whole hierarchy list from the scene.</summary>
    public const string RebuildEntireHierarchy = "RebuildEntireHierarchy";
}


public enum HierarchyChangeType
{
    Added,
    Removed,
    AddedParent
}

public struct HierarchyChange
{
    public readonly LevelObject LevelObject;
    public readonly HierarchyChangeType ChangeType;
    public HierarchyChange(LevelObject levelObject, HierarchyChangeType changeType)
    {
        LevelObject = levelObject;
        ChangeType = changeType;
    }
}