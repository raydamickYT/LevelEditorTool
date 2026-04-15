using UnityEngine;
using UnityEngine.InputSystem;

public class EditorShortcutHandler : MonoBehaviour
{
    void OnEnable()
    {
        InputHandler.Instance.OnWEvent += EnableMoveGizmo;
        InputHandler.Instance.OnEEvent += EnableRotateGizmo;
        InputHandler.Instance.OnREvent += EnableScaleGizmo;
        InputHandler.Instance.OnDeleteEvent += DeleteSelectedObjects;
    }

    void OnDisable()
    {
        InputHandler.Instance.OnWEvent -= EnableMoveGizmo;
        InputHandler.Instance.OnEEvent -= EnableRotateGizmo;
        InputHandler.Instance.OnREvent -= EnableScaleGizmo;
        InputHandler.Instance.OnDeleteEvent -= DeleteSelectedObjects;

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

    public void DeleteSelectedObjects(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            EventManager.Instance.TriggerUnityEvent(SelectionEvents.OnDeleteSelected);
        }
    }
}
