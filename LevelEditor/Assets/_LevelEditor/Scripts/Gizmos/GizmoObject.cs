using UnityEngine;

public class GizmoObject : MonoBehaviour, IGizmoObject
{
    public SelectableTargetData gizmoTargetData;

    public bool IsSelected { get { return gizmoTargetData != null && gizmoTargetData.IsSelected; } }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gizmoTargetData.SelectableComponent = this;
        if (gizmoTargetData == null)
        {
            Debug.LogError("GizmoTargetData is not assigned in the inspector for " + this.gameObject.name); //TODO this can be found in the children of this gameobj, so update it so that it searches first
            return;
        }
        EventManager.Instance.TriggerDelegate("OnRegisterToSelectionController", gizmoTargetData.BaseObject, gizmoTargetData);
    }

    void OnDestroy()
    {
        EventManager.Instance.TriggerDelegate("OnDeRegisterToSelectionController", gizmoTargetData.BaseObject);
    }


    //this'll be called anytime the gizmo changes or is activated
    public void OnShow(GizmoType gizmoType)
    {
        switch (gizmoType)
        {
            case GizmoType.none:
                OnHide();
                break;
            case GizmoType.move:
                gizmoTargetData.MoveGizmo.SetActive(true);
                break;
            case GizmoType.rotate:
                gizmoTargetData.RotateGizmo.SetActive(true);
                break;
            case GizmoType.scale:
                gizmoTargetData.ScaleGizmo.SetActive(true);
                break;
            default:
                break;
        }

        gizmoTargetData.type = gizmoType;
    }

    //deze functie mag alleen beheren of/welke gizmo uit staat.
    // This function should be allowed to be 
    public void OnHide()
    {
        switch (gizmoTargetData.type)
        {
            case GizmoType.none:
                break;
            case GizmoType.move:
                gizmoTargetData.MoveGizmo.SetActive(false);
                break;
            case GizmoType.rotate:
                gizmoTargetData.RotateGizmo.SetActive(false);
                break;
            case GizmoType.scale:
                gizmoTargetData.ScaleGizmo.SetActive(false);
                break;
            default:
                break;
        }
    }

    public void OnSelect()
    {
        Debug.Log("this object was selected");
        if (gizmoTargetData == null)
        {
            return;
        }
        gizmoTargetData.IsSelected = true;
    }

    public void OnDeselect()
    {
        if (gizmoTargetData == null)
        {
            return;
        }
        gizmoTargetData.IsSelected = false;
    }

}
