using UnityEngine;

public class GizmoObject : MonoBehaviour, ISelectable
{
    public GizmoTargetData gizmoTargetData;

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
        EventManager.Instance.TriggerDelegate("OnGizmoTargetUpdated", this.gameObject, gizmoTargetData);
    }

    public void OnSelected()
    {
        if (gizmoTargetData == null)
        {
            return;
        }
        gizmoTargetData.IsSelected = true;
        EventManager.Instance.TriggerDelegate("OnGizmoTargetUpdated", this.gameObject, gizmoTargetData);
    }

    public void OnDeselected()
    {
        if (gizmoTargetData == null)
        {
            return;
        }
        gizmoTargetData.IsSelected = false;
        EventManager.Instance.TriggerDelegate("OnGizmoTargetUpdated", this.gameObject, gizmoTargetData);
    }

    public void OnSelect()
    {
        throw new System.NotImplementedException();
    }

    public void OnDeselect()
    {
        throw new System.NotImplementedException();
    }
}
