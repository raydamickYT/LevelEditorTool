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
    private readonly Dictionary<LevelObject, HierarchyObjectItem> items = new();

    private void Awake()
    {
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.RefreshMenu, (Action<IEnumerable<HierarchyChange>>)Refresh);
    }
   
    public void Refresh(IEnumerable<HierarchyChange> hierarchyChangeObjects)
    {
        switch (hierarchyChangeObjects.FirstOrDefault().ChangeType)
        {
            case HierarchyChangeType.Added:

                foreach (HierarchyChange objects in hierarchyChangeObjects)
                {
                    AddItem(objects.LevelObject);
                }
                break;
            default: //removed
                foreach (var item in hierarchyChangeObjects)
                {
                    foreach (HierarchyChange objects in hierarchyChangeObjects)
                    {
                        Clear(objects.LevelObject);
                    }
                }
                break;
        }


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

    private void Clear(LevelObject levelObject)
    {
        if(existingNames.Contains(levelObject.name))
        {
            existingNames.Remove(levelObject.name);
        }
        if (items.TryGetValue(levelObject, out var objectItem))
        {
            Destroy(objectItem.gameObject);
            items.Remove(levelObject);
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
    Removed
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