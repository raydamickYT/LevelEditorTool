using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// this class controlls the button of the parent of a group of objects
/// </summary>
public class HierarchyParentObjectItem : MonoBehaviour
{
    [Header("Children Container")]
    public Transform ChildrenContainer;

    [Header("Button Settings")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = Color.clear;
    [SerializeField] private Color selectedColor = new Color(0.25f, 0.45f, 1f, 0.45f);

    [SerializeField] private Color hoverColor = Color.grey;
    private SelectableObject selectableObject;
    [SerializeField] private List<HierarchyObjectItem> hierarchyObjectItems = new(); //childbuttons under this parent
    private readonly List<HierarchyParentObjectItem> stagedNestedGroupRows = new();
    private LevelObjectGroup levelObjectGroup; //levelobject group, contains the children in the level.


    //layout element
    [Header("Layout")]
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private float headerHeight = 60f;
    [SerializeField] private float childHeight = 60f;
    [SerializeField] private float spacing = 2f;


    [Header("Debugging")]
    public bool rebuildHierarchy;


    void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
    }

    void Update()
    {
        if (rebuildHierarchy)
        {
            UpdatePreferredHeight();
            RebuildHierarchyLayout();
            rebuildHierarchy = false;
        }
    }
    public void SetStagedNestedGroupRows(IEnumerable<HierarchyParentObjectItem> nestedRows)
    {
        stagedNestedGroupRows.Clear();
        if (nestedRows == null)
            return;

        foreach (HierarchyParentObjectItem row in nestedRows)
        {
            if (row != null)
                stagedNestedGroupRows.Add(row);
        }
    }

    public void Initialize(LevelObjectGroup target)
    {
        hierarchyObjectItems.Clear();

        levelObjectGroup = target;
        nameText.text = target.name;

        if (!levelObjectGroup.gameObject.TryGetComponent(out selectableObject))
        {
            Debug.LogWarning("Parent object does not contain SelectableObject");
            return;
        }
        selectableObject.OnSelectionChanged += UpdateSelectionVisuals;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(SetSelected);

        foreach (LevelObject child in levelObjectGroup.LevelObjects.ToList())
        {
            if (child == null || child is LevelObjectGroup)
                continue;

            if (child.hierarchyObjectItem != null)
                hierarchyObjectItems.Add(child.hierarchyObjectItem);
        }

        RefreshLayoutDelayed();
    }
    private void UpdatePreferredHeight()
    {
        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        int count = 0;
        float childrenHeight = 0f;

        foreach (Transform t in ChildrenContainer)
        {
            if (t == null)
                continue;

            count++;

            if (t.TryGetComponent(out LayoutElement childLe) && childLe != null && childLe.preferredHeight > 0.01f)
                childrenHeight += childLe.preferredHeight;
            else
                childrenHeight += childHeight;
        }

        if (count > 1)
            childrenHeight += (count - 1) * spacing;

        layoutElement.preferredHeight = headerHeight + childrenHeight;
    }

    private void RebuildHierarchyLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ChildrenContainer);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform.parent);
    }

    public void ReleaseChildren(Transform newParent)
    {
        if (newParent == null)
            return;

        foreach (HierarchyParentObjectItem nested in stagedNestedGroupRows.ToList())
        {
            if (nested == null)
                continue;

            nested.transform.SetParent(newParent, false);
        }

        stagedNestedGroupRows.Clear();

        foreach (HierarchyObjectItem childItem in hierarchyObjectItems.ToList())
        {
            if (childItem == null)
                continue;

            childItem.transform.SetParent(newParent, false);
        }

        hierarchyObjectItems.Clear();
    }

    private void UpdateSelectionVisuals()
    {
        if (selectableObject == null || background == null)
            return;

        background.color = selectableObject.IsSelected ? selectedColor : normalColor;
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(SetSelected);
        if (selectableObject != null)
            selectableObject.OnSelectionChanged -= UpdateSelectionVisuals;
    }
    // * for some reason unity didn't like the pasting, it wouldn't display the children properly. That's why we wait till the end of the frame before we display the children
    // * properly
    private void RefreshLayoutDelayed()
    {
        StartCoroutine(RebuildAtEndOfFrame());
    }
    private IEnumerator RebuildAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        foreach (HierarchyParentObjectItem nested in stagedNestedGroupRows)
        {
            if (nested == null)
                continue;

            nested.transform.SetParent(ChildrenContainer, false);
        }

        foreach (HierarchyObjectItem child in hierarchyObjectItems)
        {
            if (child == null)
                continue;

            child.transform.SetParent(ChildrenContainer, false);
        }

        UpdatePreferredHeight();
        RebuildHierarchyLayout();
    }

    private void SetSelected()
    {
        if (levelObjectGroup == null)
            return;


        SelectionCommand command = IsCtrlHeld()
            ? SelectionCommand.ToggleSelect
            : SelectionCommand.Select;


        //reset the centre of the object according to the new position
        levelObjectGroup.UpdateCenterWithoutMovingChildren();

        EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, levelObjectGroup.gameObject, command);
    }

    private bool IsCtrlHeld()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed);
    }
}
