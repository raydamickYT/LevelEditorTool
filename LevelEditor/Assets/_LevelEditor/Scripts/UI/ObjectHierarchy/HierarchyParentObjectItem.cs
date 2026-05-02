using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// this class controlls the button of the parent of a group of objects
/// todo: this should also be an action, since it should be undoable
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
    private LevelObject levelObject; //used for linking to the levelObject
    [SerializeField]private List<HierarchyObjectItem> hierarchyObjectItems = new(); //childbuttons under this parent
    private LevelObjectGroup levelObjectGroup; //levelobject group, contains the children in the level.


    public void Initialize(LevelObject target)
    {
        levelObject = target;
        nameText.text = target.name;

        if (!levelObject.gameObject.TryGetComponent(out selectableObject))
        {
            Debug.LogWarning("Parent object does not contain SelectableObject");
            return;
        }
        selectableObject.OnSelectionChanged += UpdateSelectionVisuals;

        button.onClick.AddListener(SetSelected);

        //get group objects
        if (!levelObject.gameObject.TryGetComponent(out levelObjectGroup))
        {
            Debug.LogWarning("object does not contain LevelObjectGroup");
            return;
        }

        // save the children buttons
        if (levelObjectGroup.LevelObjects.ToList().Count == 0) return;

        foreach (LevelObject child in levelObjectGroup.LevelObjects.ToList())
        {
            if(child.hierarchyObjectItem == null) continue;

            HierarchyObjectItem childItem = child.hierarchyObjectItem;

            hierarchyObjectItems.Add(childItem);
            childItem.transform.SetParent(ChildrenContainer, false);
        }
    }

    private void UpdateSelectionVisuals()
    {
        if (selectableObject == null) return;

        background.color = selectableObject.IsSelected ? selectedColor : normalColor;
    }

    private void SetSelected()
    {
        if (levelObject == null)
            return;


        SelectionCommand command = IsCtrlHeld()
            ? SelectionCommand.ToggleSelect
            : SelectionCommand.Select;

        EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, levelObject.gameObject, command); //TODO: finish this class first
    }

    private bool IsCtrlHeld()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed);
    }
}
