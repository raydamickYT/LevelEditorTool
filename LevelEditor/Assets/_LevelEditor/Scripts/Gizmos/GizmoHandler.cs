using System;
using System.Collections.Generic;
using UnityEngine;

public class GizmoHandler : MonoBehaviour
{
    [SerializeField] private GizmoController gizmoController = new();

    private void Awake()
    {
        // EventManager.Instance.AddDelegateListener("OnShowGizmo", (Action<SelectableTargetData>)HandleShowGizmo);
        EventManager.Instance.AddDelegateListener("OnSelectionChanged", (Action<HashSet<SelectableTargetData>>)HandleHideGizmo);
        EventManager.Instance.AddDelegateListener("OnGizmoTypeChanged", (Action<GizmoType>)HandleGizmoTypeChanged);

    }
    private void HandleShowGizmo(SelectableTargetData data)
     => gizmoController.ShowGizmo(data);
    private void HandleHideGizmo(HashSet<SelectableTargetData> data)
     => gizmoController.HandleSelectionChanged(data);
    private void HandleGizmoTypeChanged(GizmoType type)
        => gizmoController.SetGizmoType(type);
}
