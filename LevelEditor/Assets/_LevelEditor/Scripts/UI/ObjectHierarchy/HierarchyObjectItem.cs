using TMPro;
using UnityEngine;
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
    private SelectableObject selectableObject;

    public void Initialize(LevelObject target)
    {
        levelObject = target;
        nameText.text = target.name;

        selectableObject = levelObject.gameObject.GetComponent<SelectableObject>();
        selectableObject.OnSelectionChanged += UpdateSelectionVisuals;

        button.onClick.AddListener(SetSelected);
    }

    //responsible for selecting the object whenever the button is pressed
    private void SetSelected()
    {
        if (levelObject == null)
            return;

        EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, levelObject.gameObject);
    }

    private void UpdateSelectionVisuals()
    {
        if (selectableObject == null) return;

        background.color = selectableObject.IsSelected ? selectedColor : normalColor;
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(SetSelected);
        selectableObject.OnSelectionChanged -= UpdateSelectionVisuals;
    }
}