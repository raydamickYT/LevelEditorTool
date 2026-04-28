using System;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;

public class ObjectHierarchyManager : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private HierarchyObjectItem itemPrefab;
    private readonly Dictionary<LevelObject, HierarchyObjectItem> items = new();

    private void Awake()
    {
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.RefreshMenu, (Action<IEnumerable<LevelObject>>)Refresh);
    }
    private void OnEnable()
    {
        LevelObject[] sceneObjects = FindObjectsByType<LevelObject>(FindObjectsSortMode.None);

        Refresh(sceneObjects);
    }

    public void Refresh(IEnumerable<LevelObject> sceneObjects)
    {
        Clear();

        foreach (LevelObject levelObject in sceneObjects)
        {
            AddItem(levelObject);
        }
    }

    private void AddItem(LevelObject levelObject)
    {
        HierarchyObjectItem item = Instantiate(itemPrefab, contentParent);
        item.Initialize(levelObject);

        items.Add(levelObject, item);
    }

    private void Clear()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        items.Clear();
    }
}

public static class ObjectHierarchyEvents
{
    public const string RefreshMenu = "RefreshMenu";
}
