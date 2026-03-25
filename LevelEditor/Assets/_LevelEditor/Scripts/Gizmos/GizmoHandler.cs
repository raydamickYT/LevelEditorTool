using System;
using System.Collections.Generic;
using UnityEngine;

public class GizmoHandler : MonoBehaviour
{
    [SerializeField] private GizmoController gizmoController = new();

    private void Awake()
    {
        EventManager.Instance.AddDelegateListener("OnShowGizmo", (Action<SelectableTargetData>)HandleShowGizmo);
        EventManager.Instance.AddDelegateListener("OnHideGizmo", (Action<SelectableTargetData>)HandleHideGizmo);
        EventManager.Instance.AddDelegateListener("OnGizmoTypeChanged", (Action<GizmoType>)HandleGizmoTypeChanged);

        EventManager.Instance.AddUnityEventListener("OnSelectionCleared", HandleClearTarget);
    }
    private void HandleShowGizmo(SelectableTargetData data)
     => gizmoController.ShowGizmo(data);
    private void HandleHideGizmo(SelectableTargetData data)
     => gizmoController.HideGizmo(data);

     private void HandleClearTarget()
     => gizmoController.ClearTarget();
    private void HandleGizmoTypeChanged(GizmoType type)
        => gizmoController.SetGizmoType(type);
}
