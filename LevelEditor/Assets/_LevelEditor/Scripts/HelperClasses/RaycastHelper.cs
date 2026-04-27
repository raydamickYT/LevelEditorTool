using UnityEngine;
using UnityEngine.InputSystem;

public static class RaycastHelper
{
    public static bool TryGetPointerHit2D(Camera camera, LayerMask layerMask, out RaycastHit2D hitInfo, float maxDistance = Mathf.Infinity)
    {
        hitInfo = default;

        if (camera == null || Mouse.current == null)
            return false;


        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = camera.ScreenToWorldPoint(mousePosition);

        hitInfo = Physics2D.Raycast(worldPosition, Vector2.zero, maxDistance, layerMask);
        return hitInfo.collider != null;
    }

    public static bool IsClickingOnLayer(Camera camera, LayerMask layerMask, float maxDistance = Mathf.Infinity)
    {
        return TryGetPointerHit2D(camera, layerMask, out _, maxDistance);
    }

    //for gizmo handles
    public static bool TryGetHandleUnderPointer(out GizmoHandle handle, Camera cam, string gizmoHandleLayerName)
    {
        handle = null;

        if (!TryGetPointerHit2D(cam, LayerMask.GetMask(gizmoHandleLayerName), out RaycastHit2D hit))
            return false;

        handle = hit.collider.GetComponent<GizmoHandle>();
        if (handle == null)
            handle = hit.collider.GetComponentInParent<GizmoHandle>();


        return handle != null;
    }
}
