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
    public static ObjectHierarchyManager Instance { get; private set; }

    const int InvalidPointerId = -1;
    const float ParentDropXOffset = 42f;
    const float RowEdgeDropZoneHeight = 5f;

    private HashSet<string> existingNames = new();
    [SerializeField] private Transform contentParent;
    [SerializeField] private HierarchyObjectItem itemPrefab;
    [SerializeField] private HierarchyParentObjectItem parentPrefab;
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string toolkitRootName = "object-hierarchy-root";
    [SerializeField] private string toolkitContentName = "object-hierarchy-content";
    [SerializeField] private string toolkitCreateGroupButtonName = "create-group-button";
    [SerializeField] private string toolkitCollapseButtonName = "object-hierarchy-collapse-button";
    [SerializeField] private float toolkitIndentWidth = 24f;
    [SerializeField] float expandedHierarchyTop = 300f;
    [SerializeField] float collapsedHierarchyHeight = 30f;

    private readonly Dictionary<LevelObject, HierarchyObjectItem> items = new();
    private readonly Dictionary<LevelObject, HierarchyParentObjectItem> parentItems = new();
    private readonly Dictionary<LevelObject, ToolkitHierarchyRow> toolkitRows = new();
    private readonly HashSet<LevelObject> collapsedToolkitParents = new();
    private VisualElement toolkitRoot;
    private VisualElement toolkitContent;
    private Button toolkitCreateGroupButton;
    private Button toolkitCollapseButton;
    private ToolkitHierarchyRow draggedToolkitRow;
    private readonly List<LevelObject> toolkitDraggedLevelObjects = new();
    private VisualElement toolkitDragPreview;
    private VisualElement toolkitDropIndicator;
    private Vector2 toolkitDragPreviewOffset;
    private Vector2 toolkitDragStartPosition;
    private int draggedToolkitPointerId = InvalidPointerId;
    private bool isDraggingToolkitRow;
    private bool isToolkitHierarchyCollapsed;
    private LevelObject toolkitRangeSelectionAnchor;

    struct ToolkitDropTarget
    {
        public bool IsValid;
        public bool IsChildDrop;
        public LevelObject Parent;
        public int SiblingIndex;
        public ToolkitHierarchyRow IndicatorRow;
        public bool InsertAfterIndicatorRow;
    }


    private bool hierarchyRebuildScheduled;


    private void Awake()
    {
        Instance = this;
        SetupToolkitReferences();
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.RefreshMenu, (Action<IEnumerable<HierarchyChange>>)Refresh);
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.RebuildEntireHierarchy, (Action)RebuildEntireHierarchyFromScene);
        EventManager.Instance.AddDelegateListener(ObjectHierarchyEvents.ScheduleRebuildEntireHierarchy, (Action)ScheduleRebuildEntireHierarchyInternal);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void ScheduleRebuildEntireHierarchy()
    {
        if (Instance == null)
        {
            EventManager.Instance?.TriggerDelegate(ObjectHierarchyEvents.RebuildEntireHierarchy);
            return;
        }

        Instance.ScheduleRebuildEntireHierarchyInternal();
    }

    void ScheduleRebuildEntireHierarchyInternal()
    {
        if (hierarchyRebuildScheduled)
            return;

        hierarchyRebuildScheduled = true;
        StartCoroutine(RebuildEntireHierarchyNextFrame());
    }

    System.Collections.IEnumerator RebuildEntireHierarchyNextFrame()
    {
        yield return null;
        hierarchyRebuildScheduled = false;
        RebuildEntireHierarchyFromScene();
    }

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument != null && uiDocument.rootVisualElement != null)
            SetupToolkitReferences();
        else
            StartCoroutine(SetupToolkitWhenReady());
    }

    System.Collections.IEnumerator SetupToolkitWhenReady()
    {
        yield return null;
        SetupToolkitReferences();
    }

    void OnDisable()
    {
        if (toolkitCreateGroupButton != null)
            toolkitCreateGroupButton.clicked -= CreateGroupParentObject;

        if (toolkitCollapseButton != null)
            toolkitCollapseButton.clicked -= ToggleToolkitHierarchyCollapsed;

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
        if (toolkitCreateGroupButton != null)
            toolkitCreateGroupButton.clicked -= CreateGroupParentObject;

        toolkitCreateGroupButton = foundButton;
        if (toolkitCreateGroupButton != null)
            toolkitCreateGroupButton.clicked += CreateGroupParentObject;

        SetupToolkitCollapseButton(root);
    }

    void SetupToolkitCollapseButton(VisualElement documentRoot)
    {
        Button foundButton = documentRoot?.Q<Button>(toolkitCollapseButtonName);
        if (foundButton == toolkitCollapseButton)
        {
            ApplyToolkitHierarchyCollapsedState();
            return;
        }

        if (toolkitCollapseButton != null)
            toolkitCollapseButton.clicked -= ToggleToolkitHierarchyCollapsed;

        toolkitCollapseButton = foundButton;
        if (toolkitCollapseButton == null)
            return;

        toolkitCollapseButton.clicked += ToggleToolkitHierarchyCollapsed;
        ApplyToolkitHierarchyCollapsedState();
    }

    void ToggleToolkitHierarchyCollapsed()
    {
        isToolkitHierarchyCollapsed = !isToolkitHierarchyCollapsed;
        ApplyToolkitHierarchyCollapsedState();
    }

    void ApplyToolkitHierarchyCollapsedState()
    {
        if (toolkitRoot == null)
            return;

        if (isToolkitHierarchyCollapsed)
        {
            RemoveToolkitDragPreview();
            RemoveToolkitDropIndicator();
            toolkitRoot.AddToClassList("object-hierarchy-collapsed");
            toolkitRoot.style.top = StyleKeyword.Auto;
            toolkitRoot.style.bottom = 0f;
            toolkitRoot.style.height = collapsedHierarchyHeight;
        }
        else
        {
            toolkitRoot.RemoveFromClassList("object-hierarchy-collapsed");
            toolkitRoot.style.top = expandedHierarchyTop;
            toolkitRoot.style.bottom = 0f;
            toolkitRoot.style.height = StyleKeyword.Auto;
        }

        if (toolkitCollapseButton != null)
            toolkitCollapseButton.text = isToolkitHierarchyCollapsed ? "Show" : "Hide";
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
        toolkitDraggedLevelObjects.Clear();
        RemoveToolkitDragPreview();
        RemoveToolkitDropIndicator();
        draggedToolkitPointerId = InvalidPointerId;
        isDraggingToolkitRow = false;
    }

    void BuildToolkitRowRecursive(LevelObject levelObject, int depth)
    {
        if (levelObject == null || toolkitContent == null || toolkitRows.ContainsKey(levelObject))
            return;

        if (levelObject.gameObject == null)
            return;

        levelObject.name = GetUniqueHierarchyName(levelObject.name);
        existingNames.Add(levelObject.name);

        float indentWidth = Mathf.Max(24f, toolkitIndentWidth);
        List<LevelObject> children = GetDirectLevelObjectChildren(levelObject);
        bool hasChildren = children.Count > 0;
        bool isCollapsedParent = hasChildren && collapsedToolkitParents.Contains(levelObject);
        ToolkitHierarchyRow row = new ToolkitHierarchyRow(
            levelObject,
            depth,
            indentWidth,
            SelectToolkitRow,
            ToggleToolkitParent,
            BeginToolkitRowDrag,
            MoveToolkitRowDrag,
            EndToolkitRowDrag,
            hasChildren,
            isCollapsedParent);
        toolkitRows[levelObject] = row;
        toolkitContent.Add(row.Root);

        if (!hasChildren || collapsedToolkitParents.Contains(levelObject))
            return;

        foreach (LevelObject child in children)
            BuildToolkitRowRecursive(child, depth + 1);
    }

    void ToggleToolkitParent(LevelObject parent)
    {
        if (parent == null)
            return;

        if (!collapsedToolkitParents.Add(parent))
            collapsedToolkitParents.Remove(parent);

        RebuildToolkitHierarchyFromScene();
    }

    void BeginToolkitRowDrag(ToolkitHierarchyRow row, PointerDownEvent evt)
    {
        if (row == null || row.LevelObject == null || evt.button != 0)
            return;

        draggedToolkitRow = row;
        toolkitDraggedLevelObjects.Clear();
        toolkitDraggedLevelObjects.AddRange(GetToolkitDraggedLevelObjects(row.LevelObject));
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
            SetToolkitRowsDragging(true);
            CreateToolkitDragPreview(row, toolkitDragStartPosition);
        }

        ShowToolkitDragPreview(ToVector2(evt.position));
        UpdateToolkitDropIndicator(toolkitDraggedLevelObjects, ToVector2(evt.position));
        evt.StopPropagation();
    }

    void EndToolkitRowDrag(ToolkitHierarchyRow row, PointerUpEvent evt)
    {
        if (row == null || row != draggedToolkitRow || evt.pointerId != draggedToolkitPointerId)
            return;

        if (row.Root.HasPointerCapture(evt.pointerId))
            row.Root.ReleasePointer(evt.pointerId);

        SetToolkitRowsDragging(false);
        RemoveToolkitDragPreview();
        RemoveToolkitDropIndicator();

        if (isDraggingToolkitRow)
        {
            TryDropToolkitRows(toolkitDraggedLevelObjects, ToVector2(evt.position));
            row.SuppressNextClick();
            evt.StopPropagation();
        }

        draggedToolkitRow = null;
        toolkitDraggedLevelObjects.Clear();
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

        Label name = new Label(GetToolkitDragPreviewLabel(row.LevelObject));
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

    string GetToolkitDragPreviewLabel(LevelObject fallback)
    {
        if (toolkitDraggedLevelObjects.Count <= 1)
            return fallback != null ? fallback.name : "Missing Object";

        return toolkitDraggedLevelObjects.Count + " objects";
    }

    void UpdateToolkitDropIndicator(List<LevelObject> draggedObjects, Vector2 panelPosition)
    {
        ToolkitDropTarget target;
        if (!TryGetToolkitDropTarget(draggedObjects, panelPosition, out target) || target.IsChildDrop)
        {
            RemoveToolkitDropIndicator();
            return;
        }

        ShowToolkitDropIndicator(target);
    }

    void ShowToolkitDropIndicator(ToolkitDropTarget target)
    {
        if (toolkitRoot == null || toolkitContent == null)
            return;

        if (toolkitDropIndicator == null)
        {
            toolkitDropIndicator = new VisualElement();
            toolkitDropIndicator.AddToClassList("hierarchy-drop-indicator");
            toolkitDropIndicator.pickingMode = PickingMode.Ignore;
            toolkitDropIndicator.style.position = Position.Absolute;
            toolkitRoot.Add(toolkitDropIndicator);
        }

        Rect anchorBounds = target.IndicatorRow != null
            ? target.IndicatorRow.Root.worldBound
            : toolkitContent.worldBound;

        float indicatorWorldY = target.InsertAfterIndicatorRow
            ? anchorBounds.yMax + 1f
            : anchorBounds.yMin - 1f;
        float indicatorWorldX = target.IndicatorRow != null
            ? anchorBounds.xMin
            : toolkitContent.worldBound.xMin;
        float indicatorWidth = Mathf.Max(60f, toolkitContent.worldBound.xMax - indicatorWorldX - 4f);

        Vector2 localPosition = new Vector2(indicatorWorldX, indicatorWorldY) - toolkitRoot.worldBound.position;
        toolkitDropIndicator.style.display = DisplayStyle.Flex;
        toolkitDropIndicator.style.left = localPosition.x;
        toolkitDropIndicator.style.top = localPosition.y;
        toolkitDropIndicator.style.width = indicatorWidth;
        toolkitDropIndicator.BringToFront();
    }

    void RemoveToolkitDropIndicator()
    {
        toolkitDropIndicator?.RemoveFromHierarchy();
        toolkitDropIndicator = null;
    }

    static Vector2 ToVector2(Vector3 position)
    {
        return new Vector2(position.x, position.y);
    }

    string GetToolkitFoldoutText(LevelObject levelObject)
    {
        if (levelObject == null || GetDirectLevelObjectChildren(levelObject).Count == 0)
            return "";

        return collapsedToolkitParents.Contains(levelObject) ? ">" : "v";
    }

    void TryDropToolkitRows(List<LevelObject> draggedObjects, Vector2 panelPosition)
    {
        if (draggedObjects == null || draggedObjects.Count == 0)
            return;

        ToolkitDropTarget dropTarget;
        if (TryGetToolkitDropTarget(draggedObjects, panelPosition, out dropTarget))
        {
            HierarchyReparentAction reparentAction = new HierarchyReparentAction(draggedObjects);

            MoveLevelObjectsToContainer(draggedObjects, dropTarget.Parent, dropTarget.SiblingIndex);

            RegisterHierarchyReparentAction(reparentAction);
            RebuildToolkitHierarchyFromScene();
        }
    }

    void RegisterHierarchyReparentAction(HierarchyReparentAction reparentAction)
    {
        if (reparentAction == null)
            return;

        reparentAction.CaptureAfterState();
        bool hasChanged = reparentAction.HasChanged();
        if (hasChanged)
            EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, reparentAction);
    }

    bool TryGetToolkitDropTarget(List<LevelObject> draggedObjects, Vector2 panelPosition, out ToolkitDropTarget dropTarget)
    {
        dropTarget = new ToolkitDropTarget();

        if (draggedObjects == null || draggedObjects.Count == 0 || toolkitContent == null)
            return false;

        ToolkitHierarchyRow targetRow = GetToolkitRowAtPosition(panelPosition, draggedObjects);
        if (targetRow == null)
        {
            return TryGetGapDropTarget(draggedObjects, panelPosition, out dropTarget);
        }

        LevelObject target = targetRow.LevelObject;
        if (target == null || IsDraggedObject(target, draggedObjects))
            return false;

        if (IsInvalidDropTarget(target, draggedObjects))
            return false;

        Rect rowBounds = targetRow.Root.worldBound;
        bool isTopEdge = panelPosition.y <= rowBounds.yMin + RowEdgeDropZoneHeight;
        bool isBottomEdge = panelPosition.y >= rowBounds.yMax - RowEdgeDropZoneHeight;
        bool wantsChildDrop = panelPosition.x > rowBounds.xMin + ParentDropXOffset;

        if (wantsChildDrop && !isTopEdge && !isBottomEdge)
        {
            dropTarget.IsValid = true;
            dropTarget.IsChildDrop = true;
            dropTarget.Parent = target;
            dropTarget.SiblingIndex = target.transform.childCount;
            return true;
        }

        LevelObject targetParent = GetLevelObjectParent(target);
        int targetIndex = target.transform.GetSiblingIndex();
        bool insertAfter = isBottomEdge || (!isTopEdge && panelPosition.y > rowBounds.center.y);
        if (insertAfter)
            targetIndex++;

        dropTarget.IsValid = true;
        dropTarget.IsChildDrop = false;
        dropTarget.Parent = targetParent;
        dropTarget.SiblingIndex = targetIndex;
        dropTarget.IndicatorRow = targetRow;
        dropTarget.InsertAfterIndicatorRow = insertAfter;
        return true;
    }

    bool TryGetGapDropTarget(List<LevelObject> draggedObjects, Vector2 panelPosition, out ToolkitDropTarget dropTarget)
    {
        dropTarget = new ToolkitDropTarget();

        if (draggedObjects == null || draggedObjects.Count == 0 || toolkitContent == null || !toolkitContent.worldBound.Contains(panelPosition))
            return false;

        ToolkitHierarchyRow nextRow = null;
        ToolkitHierarchyRow lastRow = null;
        foreach (ToolkitHierarchyRow row in GetToolkitRowsInVisualOrder())
        {
            if (row == null || IsDraggedObject(row.LevelObject, draggedObjects))
                continue;

            if (lastRow == null || row.Root.worldBound.yMax > lastRow.Root.worldBound.yMax)
                lastRow = row;

            if (row.Root.worldBound.yMin >= panelPosition.y)
            {
                nextRow = row;
                break;
            }
        }

        if (nextRow != null)
        {
            LevelObject nextObject = nextRow.LevelObject;
            if (nextObject == null || IsInvalidDropTarget(nextObject, draggedObjects))
                return false;

            dropTarget.IsValid = true;
            dropTarget.IsChildDrop = false;
            dropTarget.Parent = GetLevelObjectParent(nextObject);
            dropTarget.SiblingIndex = nextObject.transform.GetSiblingIndex();
            dropTarget.IndicatorRow = nextRow;
            dropTarget.InsertAfterIndicatorRow = false;
            return true;
        }

        dropTarget.IsValid = true;
        dropTarget.IsChildDrop = false;
        dropTarget.Parent = null;
        dropTarget.SiblingIndex = LevelObjectsRoot.Instance.RootTransform.childCount;
        dropTarget.IndicatorRow = lastRow;
        dropTarget.InsertAfterIndicatorRow = true;
        return true;
    }

    List<ToolkitHierarchyRow> GetToolkitRowsInVisualOrder()
    {
        List<ToolkitHierarchyRow> rows = toolkitRows.Values
            .Where(x => x != null && x.Root != null)
            .ToList();
        rows.Sort((a, b) => a.Root.worldBound.yMin.CompareTo(b.Root.worldBound.yMin));
        return rows;
    }

    ToolkitHierarchyRow GetToolkitRowAtPosition(Vector2 panelPosition, List<LevelObject> draggedObjects)
    {
        foreach (ToolkitHierarchyRow row in toolkitRows.Values)
        {
            if (row == null || IsDraggedObject(row.LevelObject, draggedObjects))
                continue;

            if (row.Root.worldBound.Contains(panelPosition))
                return row;
        }

        return null;
    }

    List<LevelObject> GetToolkitDraggedLevelObjects(LevelObject draggedObject)
    {
        List<LevelObject> visibleSelected = GetToolkitRowsInVisualOrder()
            .Select(row => row.LevelObject)
            .Where(levelObject => levelObject != null
                && EditorBlackBoard.CurrentSelectedLevelObjects.Contains(levelObject))
            .ToList();

        if (draggedObject == null || !visibleSelected.Contains(draggedObject))
            return draggedObject != null
                ? new List<LevelObject> { draggedObject }
                : new List<LevelObject>();

        HashSet<LevelObject> selectedSet = visibleSelected.ToHashSet();
        return visibleSelected
            .Where(levelObject => !HasSelectedAncestor(levelObject, selectedSet))
            .ToList();
    }

    void SetToolkitRowsDragging(bool isDragging)
    {
        foreach (LevelObject levelObject in toolkitDraggedLevelObjects)
        {
            if (levelObject == null || !toolkitRows.TryGetValue(levelObject, out ToolkitHierarchyRow row))
                continue;

            if (isDragging)
                row.Root.AddToClassList("hierarchy-row-dragging");
            else
                row.Root.RemoveFromClassList("hierarchy-row-dragging");
        }
    }

    void MoveLevelObjectsToContainer(List<LevelObject> draggedObjects, LevelObject newParent, int targetSiblingIndex)
    {
        if (draggedObjects == null || draggedObjects.Count == 0)
            return;

        List<LevelObject> orderedObjects = draggedObjects
            .Where(levelObject => levelObject != null)
            .ToList();

        int insertIndex = targetSiblingIndex;
        foreach (LevelObject levelObject in orderedObjects)
        {
            MoveLevelObjectToContainer(levelObject, newParent, insertIndex);
            insertIndex++;
        }
    }

    static bool IsDraggedObject(LevelObject levelObject, List<LevelObject> draggedObjects)
    {
        return levelObject != null
            && draggedObjects != null
            && draggedObjects.Contains(levelObject);
    }

    static bool IsInvalidDropTarget(LevelObject target, List<LevelObject> draggedObjects)
    {
        if (target == null || draggedObjects == null)
            return true;

        foreach (LevelObject draggedObject in draggedObjects)
        {
            if (draggedObject == null)
                continue;

            if (target == draggedObject || IsDescendantOf(target, draggedObject))
                return true;
        }

        return false;
    }

    static bool HasSelectedAncestor(LevelObject levelObject, HashSet<LevelObject> selectedSet)
    {
        if (levelObject == null || selectedSet == null || selectedSet.Count == 0)
            return false;

        Transform current = levelObject.transform.parent;
        while (current != null)
        {
            if (current.TryGetComponent(out LevelObject parent) && selectedSet.Contains(parent))
                return true;

            current = current.parent;
        }

        return false;
    }

    void MoveLevelObjectToContainer(LevelObject dragged, LevelObject newParent, int targetSiblingIndex)
    {
        if (dragged == null)
            return;

        if (newParent != null && (newParent == dragged || IsDescendantOf(newParent, dragged)))
            return;

        Transform newParentTransform = newParent != null
            ? newParent.transform
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

    static List<LevelObject> GetDirectLevelObjectChildren(LevelObject parent)
    {
        List<LevelObject> children = new List<LevelObject>();
        if (parent == null)
            return children;

        if (parent is LevelObjectGroup group)
        {
            foreach (LevelObject child in group.LevelObjects)
            {
                if (child == null || child.gameObject == null)
                    continue;

                children.Add(child);
            }

            children.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return children;
        }

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform childTransform = parent.transform.GetChild(i);
            if (childTransform == null || childTransform.gameObject == null)
                continue;

            if (childTransform.TryGetComponent(out LevelObject child))
                children.Add(child);
        }

        children.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        return children;
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

        if (IsShiftHeld())
        {
            SelectToolkitRange(levelObject);
            return;
        }

        toolkitRangeSelectionAnchor = levelObject;

        if (levelObject is LevelObjectGroup group)
            group.UpdateCenterWithoutMovingChildren();

        SelectionCommand command = IsCtrlHeld()
            ? SelectionCommand.ToggleSelect
            : SelectionCommand.Select;

        EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, levelObject.gameObject, command);
    }

    void SelectToolkitRange(LevelObject clickedObject)
    {
        if (clickedObject == null)
            return;

        LevelObject anchor = GetValidToolkitRangeAnchor(clickedObject);
        List<LevelObject> visibleObjects = GetToolkitRowsInVisualOrder()
            .Select(row => row.LevelObject)
            .Where(levelObject => levelObject != null)
            .ToList();

        int anchorIndex = visibleObjects.IndexOf(anchor);
        int clickedIndex = visibleObjects.IndexOf(clickedObject);
        if (anchorIndex < 0 || clickedIndex < 0)
        {
            toolkitRangeSelectionAnchor = clickedObject;
            EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, clickedObject.gameObject, SelectionCommand.Select);
            return;
        }

        int start = Mathf.Min(anchorIndex, clickedIndex);
        int end = Mathf.Max(anchorIndex, clickedIndex);
        List<GameObject> objectsToSelect = new List<GameObject>();
        for (int i = start; i <= end; i++)
        {
            LevelObject item = visibleObjects[i];
            if (item != null)
                objectsToSelect.Add(item.gameObject);
        }

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, objectsToSelect);
    }

    LevelObject GetValidToolkitRangeAnchor(LevelObject fallback)
    {
        if (toolkitRangeSelectionAnchor != null && toolkitRows.ContainsKey(toolkitRangeSelectionAnchor))
            return toolkitRangeSelectionAnchor;

        foreach (LevelObject selected in EditorBlackBoard.CurrentSelectedLevelObjects)
        {
            if (selected != null && toolkitRows.ContainsKey(selected))
            {
                toolkitRangeSelectionAnchor = selected;
                return selected;
            }
        }

        toolkitRangeSelectionAnchor = fallback;
        return fallback;
    }

    static bool IsCtrlHeld()
    {
        return Keyboard.current != null
            && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
    }

    static bool IsShiftHeld()
    {
        return Keyboard.current != null
            && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
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
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "New Game Object";

        if (!existingNames.Contains(baseName))
            return baseName;

        string rootName = GetHierarchyNameRoot(baseName);
        int index = GetNextHierarchyNameIndex(rootName);
        string candidateName;

        do
        {
            candidateName = $"{rootName} ({index})";
            index++;
        }
        while (existingNames.Contains(candidateName));

        return candidateName;
    }

    static string GetHierarchyNameRoot(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "New Game Object";

        if (name.Length < 4 || name[name.Length - 1] != ')')
            return name;

        int openParen = name.LastIndexOf(" (", StringComparison.Ordinal);
        if (openParen < 0)
            return name;

        string numberPart = name.Substring(openParen + 2, name.Length - openParen - 3);
        if (!int.TryParse(numberPart, out _))
            return name;

        return name.Substring(0, openParen);
    }

    int GetNextHierarchyNameIndex(string rootName)
    {
        int maxIndex = 0;

        foreach (string existing in existingNames)
        {
            if (string.Equals(existing, rootName, StringComparison.Ordinal))
            {
                maxIndex = Mathf.Max(maxIndex, 1);
                continue;
            }

            if (!existing.StartsWith(rootName + " (", StringComparison.Ordinal) || existing[existing.Length - 1] != ')')
                continue;

            string suffix = existing.Substring(rootName.Length + 2, existing.Length - rootName.Length - 3);
            if (int.TryParse(suffix, out int index))
                maxIndex = Mathf.Max(maxIndex, index);
        }

        return maxIndex + 1;
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
        readonly Action<LevelObject> toggleParentCallback;
        readonly Action<ToolkitHierarchyRow, PointerDownEvent> beginDragCallback;
        readonly Action<ToolkitHierarchyRow, PointerMoveEvent> moveDragCallback;
        readonly Action<ToolkitHierarchyRow, PointerUpEvent> endDragCallback;
        readonly Label nameLabel;
        readonly Label foldoutIcon;
        bool suppressNextClick;

        public VisualElement Root { get; }
        public LevelObject LevelObject => levelObject;

        public ToolkitHierarchyRow(
            LevelObject levelObject,
            int depth,
            float indentWidth,
            Action<LevelObject> selectCallback,
            Action<LevelObject> toggleParentCallback,
            Action<ToolkitHierarchyRow, PointerDownEvent> beginDragCallback,
            Action<ToolkitHierarchyRow, PointerMoveEvent> moveDragCallback,
            Action<ToolkitHierarchyRow, PointerUpEvent> endDragCallback,
            bool hasChildren,
            bool isCollapsedParent)
        {
            this.levelObject = levelObject;
            this.selectCallback = selectCallback;
            this.toggleParentCallback = toggleParentCallback;
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

            foldoutIcon = new Label(GetFoldoutText(hasChildren, isCollapsedParent));
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

        public void SuppressNextClick()
        {
            suppressNextClick = true;
        }

        static string GetFoldoutText(bool hasChildren, bool isCollapsedParent)
        {
            if (!hasChildren)
                return "";

            return isCollapsedParent ? ">" : "v";
        }

        void OnFoldoutClick(ClickEvent evt)
        {
            toggleParentCallback?.Invoke(levelObject);

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
            if (suppressNextClick)
            {
                suppressNextClick = false;
                evt.StopPropagation();
                return;
            }

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

    /// <summary>Deferred rebuild after destroy/spawn so Unity removes objects first.</summary>
    public const string ScheduleRebuildEntireHierarchy = "ScheduleRebuildEntireHierarchy";
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