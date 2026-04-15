using System;
using UnityEngine;
using UnityEngine.Rendering;

public class GizmoObject : MonoBehaviour, IGizmoObject
{
    public GizmoTargetData gizmoTargetData;
    public ISelectable selectableObject;
    [SerializeField] private float gizmoBaseSize = 1f;

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
    }
    void LateUpdate()
    {
        if (TargetTransform == null)
            return;

        Transform activeRoot = GetActiveGizmoRoot();
        if (activeRoot == null)
            return;

        UpdateActiveGizmoScale();
    }

    private Vector3 GetInverseTargetScale(Transform transform)
    {
        if (transform == null)
            return Vector3.one;

        Vector3 lossy = transform.lossyScale;

        return new Vector3(
            Mathf.Abs(lossy.x) > 0.0001f ? 1f / Mathf.Abs(lossy.x) : 1f,
            Mathf.Abs(lossy.y) > 0.0001f ? 1f / Mathf.Abs(lossy.y) : 1f,
            Mathf.Abs(lossy.z) > 0.0001f ? 1f / Mathf.Abs(lossy.z) : 1f
        );
    }
    private float GetZoomScale()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return 1f;

        if (cam.orthographic)
            return cam.orthographicSize * 0.1f;

        return 1f;
    }
    private void UpdateActiveGizmoScale()
    {
        Transform activeRoot = GetActiveGizmoRoot();
        if (activeRoot == null)
            return;

        Vector3 inverseTargetScale = GetInverseTargetScale(TargetTransform);
        float zoomScale = GetZoomScale();

        Vector3 finalScale = inverseTargetScale * (gizmoBaseSize * zoomScale);

        activeRoot.localScale = finalScale;
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

        gizmoTargetData.type = gizmoType;
    }

    //deze functie mag alleen beheren of/welke gizmo uit staat.
    // This function should be allowed to be 
    public void OnHide()
    {
        if (gizmoTargetData == null)
            return;

        if (gizmoTargetData.MoveGizmo != null)
            gizmoTargetData.MoveGizmo.SetActive(false);

        if (gizmoTargetData.RotateGizmo != null)
            gizmoTargetData.RotateGizmo.SetActive(false);

        if (gizmoTargetData.ScaleGizmo != null)
            gizmoTargetData.ScaleGizmo.SetActive(false);

        gizmoTargetData.type = GizmoType.none;
    }

    //setup the target to transform
    public void SetTarget(SelectableTargetData selectable)
    {
        gizmoTargetData.BaseObject = selectable?.BaseObject;
        selectableObject = selectable.SelectableComponent;

        Transform parent = selectable.BaseObject.transform;
        transform.SetParent(parent, false);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public void ClearTarget()
    {
        gizmoTargetData.BaseObject = null;
        selectableObject = null;
        transform.SetParent(null);
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
