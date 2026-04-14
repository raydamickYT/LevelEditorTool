using System;
using UnityEngine;
using UnityEngine.Rendering;

public class GizmoObject : MonoBehaviour, IGizmoObject
{
    public GizmoTargetData gizmoTargetData;
    public ISelectable selectableObject;

    public Transform TargetTransform => gizmoTargetData != null && gizmoTargetData.BaseObject != null
        ? gizmoTargetData.BaseObject.transform
        : null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (gizmoTargetData == null)
        {
            Debug.LogError("GizmoTargetData is not assigned in the inspector for " + this.gameObject.name); //TODO this can be found in the children of this gameobj, so update it so that it searches first
            return;
        }

        EventManager.Instance.AddDelegateListener(SelectionEvents.OnSelectionMade, (Action<SelectableTargetData>)SetTarget);

        // gizmoTargetData.SelectableComponent = this;
        // EventManager.Instance.TriggerDelegate(SelectionEvents.RegisterToSelectionController, gizmoTargetData.BaseObject, gizmoTargetData);
    }

    void LateUpdate()
    {
        // if (!IsSelected || TargetTransform == null) //TODO there should be another flag to check if the gizmo is active.
        //     return;
        if(selectableObject == null)
            return;

        Transform activeRoot = GetActiveGizmoRoot();
        if (activeRoot == null)
            return;

        activeRoot.position = TargetTransform.position;
    }

    void OnDestroy()
    {
        // EventManager.Instance.TriggerDelegate(SelectionEvents.DeRegisterToSelectionController, gizmoTargetData.BaseObject);
    }


    //this'll be called anytime the gizmo changes or is activated
    public void OnShow(GizmoType gizmoType)
    {
        OnHide();

        switch (gizmoType)
        {
            case GizmoType.none:
                OnHide();
                break;
            case GizmoType.move:
                if (gizmoTargetData.MoveGizmo != null)
                    gizmoTargetData.MoveGizmo.SetActive(true);
                break;
            case GizmoType.rotate:
                if (gizmoTargetData.RotateGizmo != null)
                    gizmoTargetData.RotateGizmo.SetActive(true);
                break;
            case GizmoType.scale:
                if (gizmoTargetData.ScaleGizmo != null)
                    gizmoTargetData.ScaleGizmo.SetActive(true);
                break;
        }

        Debug.Log("shwoing handles for gimzo: ");
        gizmoTargetData.type = gizmoType;
    }

    //deze functie mag alleen beheren of/welke gizmo uit staat.
    // This function should be allowed to be 
    public void OnHide()
    {
        if (gizmoTargetData == null)
            return;
        Debug.Log("hiding handles for gimzo: ");

        if (gizmoTargetData.MoveGizmo != null)
            gizmoTargetData.MoveGizmo.SetActive(false);

        if (gizmoTargetData.RotateGizmo != null)
            gizmoTargetData.RotateGizmo.SetActive(false);

        if (gizmoTargetData.ScaleGizmo != null)
            gizmoTargetData.ScaleGizmo.SetActive(false);

        // selectableObject = null;
    }

    public void SetTarget(SelectableTargetData selectable)
    {
        gizmoTargetData.BaseObject = selectable?.BaseObject;
        selectableObject = selectable.SelectableComponent;
    }

    public void ClearTarget()
    {
        gizmoTargetData.BaseObject = null;
        selectableObject = null;
    }


    public Transform GetActiveGizmoRoot()
    {
        if (gizmoTargetData == null)
            return null;

        return gizmoTargetData.type switch
        {
            GizmoType.move => gizmoTargetData.MoveGizmo != null ? gizmoTargetData.MoveGizmo.transform : null,
            GizmoType.rotate => gizmoTargetData.RotateGizmo != null ? gizmoTargetData.RotateGizmo.transform : null,
            GizmoType.scale => gizmoTargetData.ScaleGizmo != null ? gizmoTargetData.ScaleGizmo.transform : null,
            _ => null
        };
    }

}
