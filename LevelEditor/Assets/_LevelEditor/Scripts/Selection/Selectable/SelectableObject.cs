using UnityEngine;

public class SelectableObject : MonoBehaviour, ISelectable
{
    public bool IsSelected => TargetData.IsSelected;
    [SerializeField] private SelectableTargetData TargetData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        TargetData.BaseObject = gameObject;
        TargetData.SelectableComponent = this;

        EventManager.Instance.TriggerDelegate(SelectionEvents.RegisterToSelectionController, gameObject, TargetData);
    }
    void OnDestroy()
    {
        EventManager.Instance.TriggerDelegate(SelectionEvents.DeRegisterToSelectionController, gameObject, TargetData);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnDeselect()
    {
        TargetData.IsSelected = false;
    }

    public void OnSelect()
    {
        TargetData.IsSelected = true;
        EventManager.Instance.TriggerDelegate(SelectionEvents.OnSelectionMade, TargetData);
    }

}
