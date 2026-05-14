using UnityEngine;

public class GizmoMoveOperation : IGizmoTransformOperation
{
    public void Apply(GizmoDragContext context, Vector3 currentMouseWorld, GizmoObject gizmoObject)
    {
        Vector3 delta = currentMouseWorld - context.DragStartWorld;

        Vector3 rawPosition;
        if (context.ActiveHandle.Axis == GizmoAxis.All)
        {
            rawPosition = context.TargetStartPosition + delta;
        }
        else
        {
            Vector3 axis = context.ActiveHandle.GetAxisVectorWorld().normalized;
            float projectedDistance = Vector3.Dot(delta, axis);
            rawPosition = context.TargetStartPosition + axis * projectedDistance;
        }

        rawPosition.z = context.TargetStartPosition.z;

        Vector3 snapped = GizmoSnapToEdgeUtility.ResolveSnappedMoveWorldPosition(rawPosition, context, gizmoObject);

        context.ActiveTarget.position = snapped;
        gizmoObject.transform.position = snapped;
        EventManager.Instance.TriggerUnityEvent(TransformWindowEvents.OnTransformValuesUpdated);
    }
}
