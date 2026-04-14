using UnityEngine;
using UnityEngine.InputSystem;

public class GizmoUIController : MonoBehaviour
{
    void OnEnable()
    {
        InputHandler.Instance.OnWEvent += EnableMoveGizmo;
        InputHandler.Instance.OnEEvent += EnableRotateGizmo;
        InputHandler.Instance.OnREvent += EnableScaleGizmo;
    }

    void OnDisable()
    {
        InputHandler.Instance.OnWEvent -= EnableMoveGizmo;
        InputHandler.Instance.OnEEvent -= EnableRotateGizmo;
        InputHandler.Instance.OnREvent -= EnableScaleGizmo;
    }

    public void EnableMoveGizmo(InputAction.CallbackContext context)
    {
        if (context.started)
            EventManager.Instance.TriggerDelegate(GimzmoEvents.OnGizmoTypeChanged, GizmoType.move);
    }

    public void EnableRotateGizmo(InputAction.CallbackContext context)
    {
        if (context.started)
            EventManager.Instance.TriggerDelegate(GimzmoEvents.OnGizmoTypeChanged, GizmoType.rotate);
    }

    public void EnableScaleGizmo(InputAction.CallbackContext context)
    {
        if (context.started)
            EventManager.Instance.TriggerDelegate(GimzmoEvents.OnGizmoTypeChanged, GizmoType.scale);
    }
}
