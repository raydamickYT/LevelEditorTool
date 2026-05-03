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
                        AddParent(group);
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

    private void AddParent(LevelObjectGroup levelObject)
    {
        if (levelObject == null) return;

        if (items.ContainsKey(levelObject)) return;

        levelObject.name = GetUniqueHierarchyName(levelObject.name);

        HierarchyParentObjectItem parentItem = Instantiate(parentPrefab, contentParent);
        parentItem.Initialize(levelObject);

        parentItems.Add(levelObject, parentItem);
        existingNames.Add(levelObject.name);
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
}

public static class ObjectHierarchyEvents
{
    public const string RefreshMenu = "RefreshMenu";
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