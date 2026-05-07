using System;
using UnityEngine;

public class SelectableObject : MonoBehaviour, ISelectable
{
    public bool IsSelected => targetData.IsSelected;
    [SerializeField] private SelectableTargetData targetData = new();
    public SelectableTargetData TargetData => targetData;
    public Action OnSelectionChanged;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        targetData.BaseObject = gameObject;
        targetData.SelectableComponent = this;

        EventManager.Instance.TriggerDelegate(SelectionEvents.RegisterToSelectionController, gameObject, targetData);
    }
    void OnDestroy()
    {
        EventManager.Instance.TriggerDelegate(SelectionEvents.DeRegisterToSelectionController, gameObject);
    }

    public void OnDeselect()
    {
        targetData.IsSelected = false;
        OnSelectionChanged?.Invoke();
    }

    public void OnSelect()
    {
        targetData.IsSelected = true;
        OnSelectionChanged?.Invoke();
    }

}
