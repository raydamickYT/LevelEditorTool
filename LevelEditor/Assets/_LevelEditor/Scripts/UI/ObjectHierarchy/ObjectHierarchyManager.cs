using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// this class controls the visual object hierarchy menu. 
/// It'll:
/// - create new buttons for new objects that are created.
/// - remove buttons for objects that are removed
/// - create unique names for new objects.
/// </summary>
public class ObjectHierarchyManager : MonoBehaviour
{
    const int InvalidPointerId = -1;

    private HashSet<string> existingNames = new();
    [SerializeField] private Transform contentParent;
    [SerializeField] private HierarchyObjectItem itemPrefab;
    [SerializeField] private HierarchyParentObjectItem parentPrefab;
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string toolkitRootName = "object-hierarchy-root";
    [SerializeField] private string toolkitContentName = "object-hierarchy-content";
    [SerializeField] private string toolkitCreateGroupButtonName = "create-group-button";
    [SerializeField] private float toolkitIndentWidth = 24f;

    private readonly Dictionary<LevelObject, HierarchyObjectItem> items = new();
    private readonly Dictionary<LevelObject, HierarchyParentObjectItem> parentItems = new();
    private readonly Dictionary<LevelObject, ToolkitHierarchyRow> toolkitRows = new();
    private readonly HashSet<LevelObjectGroup> collapsedToolkitGroups = new();
    private VisualElement toolkitRoot;
    private VisualElement toolkitContent;
    private Button toolkitCreateGroupButton;
    private ToolkitHierarchyRow draggedToolkitRow;
    private VisualElement toolkitDragPreview;
    private Vector2 toolkitDragPreviewOffset;
    private Vector2 toolkitDragStartPosition;
    private int draggedToolkitPointerId = InvalidPointerId;
    private bool isDraggingToolkitRow;


    private void Awake()
    {
        SetupToolkitReferences();
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.RefreshMenu, (Action<IEnumerable<HierarchyChange>>)Refresh);
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.RebuildEntireHierarchy, (Action)RebuildEntireHierarchyFromScene);
    }

    void OnDisable()
    {
        if (toolkitCreateGroupButton != null)
            toolkitCreateGroupButton.clicked -= CreateGroupParentObject;

        ClearToolkitRows();
    }

    /// <summary>
    /// Clears all hierarchy UI and rebuilds it from <see cref="LevelObjectsRoot"/> (used after level import).
    /// </summary>
    public void RebuildEntireHierarchyFromScene()
    {
        SetupToolkitReferences();
        if (UsesToolkit)
        {
            RebuildToolkitHierarchyFromScene();
            return;
        }

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
        ClearToolkitRows();

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
        SetupToolkitReferences();
        if (UsesToolkit)
        {
            RebuildToolkitHierarchyFromScene();
            return;
        }

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

        LevelObjectGroup parentGroup = levelObject.levelObjectGroup;
        if (parentGroup != null && parentItems.TryGetValue(parentGroup, out HierarchyParentObjectItem parentRow))
            parentRow.AttachChildHierarchyItem(item);
    }

    bool UsesToolkit => toolkitContent != null;

    void SetupToolkitReferences()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        VisualElement root = uiDocument != null ? uiDocument.rootVisualElement : null;
        toolkitRoot = root?.Q<VisualElement>(toolkitRootName);
        toolkitContent = root?.Q<VisualElement>(toolkitContentName);

        Button foundButton = root?.Q<Button>(toolkitCreateGroupButtonName);
        if (foundButton == toolkitCreateGroupButton)
            return;

        if (toolkitCreateGroupButton != null)
            toolkitCreateGroupButton.clicked -= CreateGroupParentObject;

        toolkitCreateGroupButton = foundButton;
        if (toolkitCreateGroupButton != null)
            toolkitCreateGroupButton.clicked += CreateGroupParentObject;
    }

    void RebuildToolkitHierarchyFromScene()
    {
        ClearToolkitRows();

        if (LevelObjectsRoot.Instance == null || toolkitContent == null)
            return;

        List<GameObject> roots = LevelObjectsRoot.Instance.GetRootLevelObjectsSnapshot();
        roots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        foreach (GameObject rootGo in roots)
        {
            if (rootGo == null || !rootGo.TryGetComponent(out LevelObject levelObject))
                continue;

            BuildToolkitRowRecursive(levelObject, 0);
        }
    }

    void ClearToolkitRows()
    {
        foreach (ToolkitHierarchyRow row in toolkitRows.Values.ToList())
            row?.Dispose();

        toolkitRows.Clear();
        existingNames.Clear();
        if (toolkitContent != null)
            toolkitContent.Clear();

        draggedToolkitRow = null;
        RemoveToolkitDragPreview();
        draggedToolkitPointerId = InvalidPointerId;
        isDraggingToolkitRow = false;
    }

    void BuildToolkitRowRecursive(LevelObject levelObject, int depth)
    {
        if (levelObject == null || toolkitContent == null || toolkitRows.ContainsKey(levelObject))
            return;

        levelObject.name = GetUniqueHierarchyName(levelObject.name);
        existingNames.Add(levelObject.name);

        float indentWidth = Mathf.Max(24f, toolkitIndentWidth);
        bool isCollapsedGroup = levelObject is LevelObjectGroup rowGroup && collapsedToolkitGroups.Contains(rowGroup);
        ToolkitHierarchyRow row = new ToolkitHierarchyRow(
            levelObject,
            depth,
            indentWidth,
            SelectToolkitRow,
            ToggleToolkitGroup,
            BeginToolkitRowDrag,
            MoveToolkitRowDrag,
            EndToolkitRowDrag,
            isCollapsedGroup);
        toolkitRows[levelObject] = row;
        toolkitContent.Add(row.Root);

        if (!(levelObject is LevelObjectGroup group))
            return;

        if (group.LevelObjects.ToList().Count == 0)
            group.RebuildChildrenFromTransform();

        if (collapsedToolkitGroups.Contains(group))
            return;

        foreach (LevelObject child in group.LevelObjects.ToList())
            BuildToolkitRowRecursive(child, depth + 1);
    }

    void ToggleToolkitGroup(LevelObjectGroup group)
    {
        if (group == null)
            return;

        if (!collapsedToolkitGroups.Add(group))
            collapsedToolkitGroups.Remove(group);

        RebuildToolkitHierarchyFromScene();
    }

    void BeginToolkitRowDrag(ToolkitHierarchyRow row, PointerDownEvent evt)
    {
        if (row == null || row.LevelObject == null || evt.button != 0)
            return;

        draggedToolkitRow = row;
        draggedToolkitPointerId = evt.pointerId;
        toolkitDragStartPosition = ToVector2(evt.position);
        isDraggingToolkitRow = false;
        row.Root.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    void MoveToolkitRowDrag(ToolkitHierarchyRow row, PointerMoveEvent evt)
    {
        if (row == null || row != draggedToolkitRow || evt.pointerId != draggedToolkitPointerId || !row.Root.HasPointerCapture(evt.pointerId))
            return;

        if (!isDraggingToolkitRow)
        {
            Vector2 panelPosition = ToVector2(evt.position);
            if ((panelPosition - toolkitDragStartPosition).sqrMagnitude < 16f)
                return;

            isDraggingToolkitRow = true;
            row.Root.AddToClassList("hierarchy-row-dragging");
            CreateToolkitDragPreview(row, toolkitDragStartPosition);
        }

        ShowToolkitDragPreview(ToVector2(evt.position));
        evt.StopPropagation();
    }

    void EndToolkitRowDrag(ToolkitHierarchyRow row, PointerUpEvent evt)
    {
        if (row == null || row != draggedToolkitRow || evt.pointerId != draggedToolkitPointerId)
            return;

        if (row.Root.HasPointerCapture(evt.pointerId))
            row.Root.ReleasePointer(evt.pointerId);

        row.Root.RemoveFromClassList("hierarchy-row-dragging");
        RemoveToolkitDragPreview();

        if (isDraggingToolkitRow)
        {
            TryDropToolkitRow(row.LevelObject, ToVector2(evt.position));
            evt.StopPropagation();
        }

        draggedToolkitRow = null;
        draggedToolkitPointerId = InvalidPointerId;
        isDraggingToolkitRow = false;
    }

    void CreateToolkitDragPreview(ToolkitHierarchyRow row, Vector2 panelPosition)
    {
        RemoveToolkitDragPreview();

        if (toolkitRoot == null || row == null || row.LevelObject == null)
            return;

        Rect sourceBounds = row.Root.worldBound;
        toolkitDragPreviewOffset = panelPosition - sourceBounds.position;

        toolkitDragPreview = new VisualElement();
        toolkitDragPreview.AddToClassList("hierarchy-row");
        toolkitDragPreview.AddToClassList("hierarchy-row-drag-preview");
        toolkitDragPreview.pickingMode = PickingMode.Ignore;
        toolkitDragPreview.style.position = Position.Absolute;
        toolkitDragPreview.style.marginLeft = 0;
        toolkitDragPreview.style.width = Mathf.Max(120f, sourceBounds.width);
        toolkitDragPreview.style.height = Mathf.Max(20f, sourceBounds.height);

        VisualElement indent = new VisualElement();
        indent.AddToClassList("hierarchy-indent");
        toolkitDragPreview.Add(indent);

        Label foldout = new Label(GetToolkitFoldoutText(row.LevelObject));
        foldout.AddToClassList("hierarchy-foldout-icon");
        toolkitDragPreview.Add(foldout);

        Label name = new Label(row.LevelObject.name);
        name.AddToClassList("hierarchy-name");
        toolkitDragPreview.Add(name);

        toolkitRoot.Add(toolkitDragPreview);
        ShowToolkitDragPreview(panelPosition);
    }

    void ShowToolkitDragPreview(Vector2 panelPosition)
    {
        if (toolkitDragPreview == null)
            return;

        Vector2 previewWorldPosition = panelPosition - toolkitDragPreviewOffset;
        Vector2 previewLocalPosition = previewWorldPosition - toolkitRoot.worldBound.position;

        toolkitDragPreview.style.display = DisplayStyle.Flex;
        toolkitDragPreview.style.left = previewLocalPosition.x;
        toolkitDragPreview.style.top = previewLocalPosition.y;
        toolkitDragPreview.BringToFront();
    }

    void RemoveToolkitDragPreview()
    {
        toolkitDragPreview?.RemoveFromHierarchy();
        toolkitDragPreview = null;
    }

    static Vector2 ToVector2(Vector3 position)
    {
        return new Vector2(position.x, position.y);
    }

    string GetToolkitFoldoutText(LevelObject levelObject)
    {
        if (!(levelObject is LevelObjectGroup group))
            return "";

        return collapsedToolkitGroups.Contains(group) ? ">" : "v";
    }

    void TryDropToolkitRow(LevelObject dragged, Vector2 panelPosition)
    {
        if (dragged == null)
            return;

        ToolkitHierarchyRow targetRow = GetToolkitRowAtPosition(panelPosition, dragged);
        if (targetRow == null)
        {
            if (toolkitContent != null && toolkitContent.worldBound.Contains(panelPosition))
                MoveLevelObjectToContainer(dragged, null, LevelObjectsRoot.Instance.RootTransform.childCount);

            RebuildToolkitHierarchyFromScene();
            return;
        }

        LevelObject target = targetRow.LevelObject;
        if (target == null || target == dragged)
            return;

        if (target is LevelObjectGroup targetGroup
            && panelPosition.x > targetRow.Root.worldBound.xMin + 42f
            && !IsDescendantOf(targetGroup, dragged))
        {
            MoveLevelObjectToContainer(dragged, targetGroup, targetGroup.transform.childCount);
            RebuildToolkitHierarchyFromScene();
            return;
        }

        LevelObjectGroup targetParent = target.levelObjectGroup;
        int targetIndex = target.transform.GetSiblingIndex();
        if (panelPosition.y > targetRow.Root.worldBound.center.y)
            targetIndex++;

        MoveLevelObjectToContainer(dragged, targetParent, targetIndex);
        RebuildToolkitHierarchyFromScene();
    }

    ToolkitHierarchyRow GetToolkitRowAtPosition(Vector2 panelPosition, LevelObject dragged)
    {
        foreach (ToolkitHierarchyRow row in toolkitRows.Values)
        {
            if (row == null || row.LevelObject == dragged)
                continue;

            if (row.Root.worldBound.Contains(panelPosition))
                return row;
        }

        return null;
    }

    void MoveLevelObjectToContainer(LevelObject dragged, LevelObjectGroup newParentGroup, int targetSiblingIndex)
    {
        if (dragged == null)
            return;

        if (dragged is LevelObjectGroup draggedGroup && newParentGroup != null && IsDescendantOf(newParentGroup, draggedGroup))
            return;

        Transform newParentTransform = newParentGroup != null
            ? newParentGroup.transform
            : LevelObjectsRoot.Instance.RootTransform;
        Transform oldParentTransform = dragged.transform.parent;
        int oldSiblingIndex = dragged.transform.GetSiblingIndex();

        if (oldParentTransform == newParentTransform && oldSiblingIndex < targetSiblingIndex)
            targetSiblingIndex--;

        LevelObjectGroup oldParentGroup = dragged.levelObjectGroup;
        if (oldParentGroup != null)
            oldParentGroup.RemoveChild(dragged);
        else
            LevelObjectsRoot.Instance.RemoveChildFromParent(dragged.gameObject);

        if (newParentGroup != null)
        {
            LevelObjectsRoot.Instance.RemoveChildFromParent(dragged.gameObject);
            dragged.transform.SetParent(newParentGroup.transform, true);
            targetSiblingIndex = Mathf.Clamp(targetSiblingIndex, 0, newParentGroup.transform.childCount - 1);
            dragged.transform.SetSiblingIndex(targetSiblingIndex);
            newParentGroup.InsertChild(dragged, targetSiblingIndex);
            newParentGroup.UpdateCenterWithoutMovingChildren();
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

    void SelectToolkitRow(LevelObject levelObject)
    {
        if (levelObject == null)
            return;

        if (levelObject is LevelObjectGroup group)
            group.UpdateCenterWithoutMovingChildren();

        SelectionCommand command = IsCtrlHeld()
            ? SelectionCommand.ToggleSelect
            : SelectionCommand.Select;

        EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, levelObject.gameObject, command);
    }

    static bool IsCtrlHeld()
    {
        return Keyboard.current != null
            && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
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

    sealed class ToolkitHierarchyRow : IDisposable
    {
        readonly LevelObject levelObject;
        readonly SelectableObject selectableObject;
        readonly Action<LevelObject> selectCallback;
        readonly Action<LevelObjectGroup> toggleGroupCallback;
        readonly Action<ToolkitHierarchyRow, PointerDownEvent> beginDragCallback;
        readonly Action<ToolkitHierarchyRow, PointerMoveEvent> moveDragCallback;
        readonly Action<ToolkitHierarchyRow, PointerUpEvent> endDragCallback;
        readonly Label nameLabel;
        readonly Label foldoutIcon;

        public VisualElement Root { get; }
        public LevelObject LevelObject => levelObject;

        public ToolkitHierarchyRow(
            LevelObject levelObject,
            int depth,
            float indentWidth,
            Action<LevelObject> selectCallback,
            Action<LevelObjectGroup> toggleGroupCallback,
            Action<ToolkitHierarchyRow, PointerDownEvent> beginDragCallback,
            Action<ToolkitHierarchyRow, PointerMoveEvent> moveDragCallback,
            Action<ToolkitHierarchyRow, PointerUpEvent> endDragCallback,
            bool isCollapsedGroup)
        {
            this.levelObject = levelObject;
            this.selectCallback = selectCallback;
            this.toggleGroupCallback = toggleGroupCallback;
            this.beginDragCallback = beginDragCallback;
            this.moveDragCallback = moveDragCallback;
            this.endDragCallback = endDragCallback;

            Root = new VisualElement();
            Root.AddToClassList("hierarchy-row");
            Root.style.marginLeft = Mathf.Max(0f, depth) * indentWidth;
            Root.RegisterCallback<ClickEvent>(OnClick);
            Root.RegisterCallback<PointerDownEvent>(OnPointerDown);
            Root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            Root.RegisterCallback<PointerUpEvent>(OnPointerUp);

            VisualElement indent = new VisualElement();
            indent.AddToClassList("hierarchy-indent");
            Root.Add(indent);

            foldoutIcon = new Label(GetFoldoutText(levelObject, isCollapsedGroup));
            foldoutIcon.AddToClassList("hierarchy-foldout-icon");
            foldoutIcon.RegisterCallback<ClickEvent>(OnFoldoutClick);
            Root.Add(foldoutIcon);

            nameLabel = new Label(levelObject != null ? levelObject.name : "Missing Object");
            nameLabel.AddToClassList("hierarchy-name");
            Root.Add(nameLabel);

            if (levelObject != null && levelObject.TryGetComponent(out SelectableObject selectable))
            {
                selectableObject = selectable;
                selectableObject.OnSelectionChanged += UpdateSelectionVisuals;
                UpdateSelectionVisuals();
            }
        }

        public void Dispose()
        {
            Root.UnregisterCallback<ClickEvent>(OnClick);
            Root.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            Root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            Root.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            if (foldoutIcon != null)
                foldoutIcon.UnregisterCallback<ClickEvent>(OnFoldoutClick);
            if (selectableObject != null)
                selectableObject.OnSelectionChanged -= UpdateSelectionVisuals;
        }

        static string GetFoldoutText(LevelObject levelObject, bool isCollapsedGroup)
        {
            if (!(levelObject is LevelObjectGroup))
                return "";

            return isCollapsedGroup ? ">" : "v";
        }

        void OnFoldoutClick(ClickEvent evt)
        {
            if (levelObject is LevelObjectGroup group)
                toggleGroupCallback?.Invoke(group);

            evt.StopPropagation();
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            beginDragCallback?.Invoke(this, evt);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            moveDragCallback?.Invoke(this, evt);
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            endDragCallback?.Invoke(this, evt);
        }

        void OnClick(ClickEvent evt)
        {
            selectCallback?.Invoke(levelObject);
            evt.StopPropagation();
        }

        void UpdateSelectionVisuals()
        {
            if (selectableObject != null && selectableObject.IsSelected)
                Root.AddToClassList("hierarchy-row-selected");
            else
                Root.RemoveFromClassList("hierarchy-row-selected");
        }
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