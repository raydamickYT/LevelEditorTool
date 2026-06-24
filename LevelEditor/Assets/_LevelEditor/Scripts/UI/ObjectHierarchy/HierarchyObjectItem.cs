using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// this class is responsible for keeping a connection between the button in the objectHierarchy and the object in the scene.
/// it'll:
/// - select a levelObject when pressed.
/// </summary>
public class HierarchyObjectItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = Color.clear;
    [SerializeField] private Color selectedColor = new Color(0.25f, 0.45f, 1f, 0.45f);

    private LevelObject levelObject;
    private SelectableObject selectableObject; //levelobject selectable component
    [HideInInspector]
    public bool IsSelected => selectableObject.IsSelected;


    public void Initialize(LevelObject target)
    {
        levelObject = target;
        nameText.text = target.name;
        levelObject.hierarchyObjectItem = this;

        if (!levelObject.gameObject.TryGetComponent(out selectableObject))
        {
            Debug.LogWarning("Parent object does not contain SelectableObject");
            return;
        }
        selectableObject.OnSelectionChanged += UpdateSelectionVisuals;

        button.onClick.AddListener(SetSelected);
    }
    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(SetSelected);
        if (selectableObject != null)
            selectableObject.OnSelectionChanged -= UpdateSelectionVisuals;
        if (levelObject != null)
            levelObject.hierarchyObjectItem = null;
    }

    public void SetDisplayName(string displayName)
    {
        if (nameText != null)
            nameText.text = displayName ?? string.Empty;
    }

    //responsible for selecting the object whenever the button is pressed
    private void SetSelected()
    {
        if (levelObject == null)
            return;

        SelectionCommand command = IsCtrlHeld()
            ? SelectionCommand.ToggleSelect
            : SelectionCommand.Select;

        EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, levelObject.gameObject, command);
    }

    private void UpdateSelectionVisuals()
    {
        if (selectableObject == null || background == null)
            return;

        background.color = selectableObject.IsSelected ? selectedColor : normalColor;
    }

    private bool IsCtrlHeld()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed);
    }
}