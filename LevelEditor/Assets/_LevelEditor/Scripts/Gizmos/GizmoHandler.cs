using System;
using System.Collections.Generic;
using UnityEngine;

public class GizmoHandler : MonoBehaviour
{
    [SerializeField] private GizmoController gizmoController = new ();

    private void Awake()
    {
        EventManager.Instance.AddDelegateListener("OnRegisterToGizmoController", (Action<GameObject, GizmoTargetData>)HandleRegister);
        EventManager.Instance.AddDelegateListener("OnDeRegisterToGizmoController", (Action<GameObject>)HandleDeregister);
        EventManager.Instance.AddDelegateListener("OnObjectSelected", (Action<GameObject>)HandleObjectSelected);
        EventManager.Instance.AddUnityEventListener("OnObjectDeselected", HandleObjectDeselected);
        EventManager.Instance.AddDelegateListener("OnGizmoTypeChanged", (Action<GizmoType>)HandleGizmoTypeChanged);
    }

    private void HandleRegister(GameObject obj, GizmoTargetData data)
        => gizmoController.Register(obj, data);

    private void HandleDeregister(GameObject obj)
        => gizmoController.Deregister(obj);

    private void HandleObjectSelected(GameObject obj)
        => gizmoController.TrySelect(obj);

    private void HandleObjectDeselected()
        => gizmoController.ClearSelection();

    private void HandleGizmoTypeChanged(GizmoType type)
        => gizmoController.SetGizmoType(type);
}
