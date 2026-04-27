using UnityEngine;

public class GizmoMoveOperation : IGizmoTransformOperation
{
    public void Apply(GizmoDragContext context, Vector3 currentMouseWorld)
    {
        Vector3 delta = currentMouseWorld - context.DragStartWorld;

        if (context.ActiveHandle.Axis == GizmoAxis.All)
        {
            delta = GizmoSnapUtility.SnapVector(delta, context.SnappingSettings.moveSnapSize, context.SnappingSettings.snappingEnabled);
            context.ActiveTarget.position = context.TargetStartPosition + delta;
            return;
        }

        Vector3 axis = context.ActiveHandle.GetAxisVectorWorld().normalized;
        float projectedDistance = Vector3.Dot(delta, axis);

        projectedDistance = GizmoSnapUtility.SnapFloat(projectedDistance, context.SnappingSettings.moveSnapSize, context.SnappingSettings.snappingEnabled);

        context.ActiveTarget.position = context.TargetStartPosition + axis * projectedDistance;
    }

}
