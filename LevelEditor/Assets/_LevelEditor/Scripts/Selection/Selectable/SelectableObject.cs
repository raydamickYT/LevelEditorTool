using System;
using UnityEngine;

public class SelectableObject : MonoBehaviour, ISelectable
{
    public bool IsSelected => TargetData.IsSelected;
    [SerializeField] private SelectableTargetData TargetData;
    public Action OnSelectionChanged;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        TargetData.BaseObject = gameObject;
        TargetData.SelectableComponent = this;

        EventManager.Instance.TriggerDelegate(SelectionEvents.RegisterToSelectionController, gameObject, TargetData);
    }
    void OnDestroy()
    {
        EventManager.Instance.TriggerDelegate(SelectionEvents.DeRegisterToSelectionController, gameObject);
    }

    public void OnDeselect()
    {
        TargetData.IsSelected = false;
        OnSelectionChanged?.Invoke();
    }

    public void OnSelect()
    {
        TargetData.IsSelected = true;
        OnSelectionChanged?.Invoke();
    }

}
