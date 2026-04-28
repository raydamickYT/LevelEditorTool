using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HierarchyObjectItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private LevelObject levelObject;

    public void Initialize(LevelObject target)
    {
        levelObject = target;
        nameText.text = target.name;

        button.onClick.AddListener(SelectObject);
    }

    private void SelectObject()
    {
        if (levelObject == null)
            return;

        // Hier jouw bestaande selection event/function gebruiken.
        // Bijvoorbeeld:
        // EventManager.InvokeSelectionRequest(levelObject);
        // of:
        // SelectionController.Select(levelObject);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(SelectObject);
    }
}