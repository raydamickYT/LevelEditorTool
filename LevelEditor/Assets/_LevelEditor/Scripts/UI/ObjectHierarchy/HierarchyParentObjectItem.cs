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

        //get group objects
        if (!levelObjectGroup.gameObject.TryGetComponent(out levelObjectGroup))
        {
            Debug.LogWarning("object does not contain LevelObjectGroup");
            return;
        }

        // save the children buttons
        if (levelObjectGroup.LevelObjects.ToList().Count == 0)
        {
            Debug.Log("No children in object");
            return;
        }

        foreach (LevelObject child in levelObjectGroup.LevelObjects.ToList())
        {
            if (child.hierarchyObjectItem == null) continue;

            HierarchyObjectItem childItem = child.hierarchyObjectItem;

            hierarchyObjectItems.Add(childItem);
        }

        RefreshLayoutDelayed();
    }
    private void UpdatePreferredHeight()
    {
        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        int childCount = hierarchyObjectItems.Count;

        float childrenHeight = childCount * childHeight;

        if (childCount > 1)
            childrenHeight += (childCount - 1) * spacing;

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
        if (selectableObject == null) return;

        background.color = selectableObject.IsSelected ? selectedColor : normalColor;
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

        foreach (HierarchyObjectItem child in hierarchyObjectItems)
        {
            child.transform.SetParent(ChildrenContainer);
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
