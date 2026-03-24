using UnityEngine;

public class GizmoUIController : MonoBehaviour
{

    public void EnableMoveGizmo()
    {
        EventManager.Instance.TriggerDelegate("OnGizmoTypeChanged", GizmoType.move);
    }

    public void EnableRotateGizmo()
    {
        EventManager.Instance.TriggerDelegate("OnGizmoTypeChanged", GizmoType.rotate);
    }

    public void EnableScaleGizmo()
    {
        EventManager.Instance.TriggerDelegate("OnGizmoTypeChanged", GizmoType.scale);
    }
}
