using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// this class controlls the button of the parent of a group of objects
/// </summary>
public class HierarchyParentObjectItem : MonoBehaviour
{
    [Header("PrefabToSpawn")]
    public GameObject parent;
    private GameObject parentObject;

    [Header("Button Settings")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = Color.clear;
    [SerializeField] private Color selectedColor = new Color(0.25f, 0.45f, 1f, 0.45f);

    private LevelObject[] levelObject;
    private SelectableObject selectableObject;
    [HideInInspector]
    public bool isSelected => selectableObject.IsSelected;


    public void Initialize(LevelObject[] target)
    {
        levelObject = target;
        nameText.text = target[0].name;

        // selectableObject = levelObject.gameObject.GetComponent<SelectableObject>();
        selectableObject.OnSelectionChanged += UpdateSelectionVisuals;

        button.onClick.AddListener(SetSelected);
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

        // EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, parent, command); //TODO: finish this class first
    }

    private bool IsCtrlHeld()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed);
    }

    void InitializeParent()
    {
        if(parent != null)
        parentObject = Instantiate(parent, new Vector3(0, 0, 0), quaternion.identity);
        
    }
}
