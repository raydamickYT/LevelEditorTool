using UnityEngine;

public class GizmoScaleOperation : IGizmoTransformOperation
{
    const float minScale = 0f;
    public void Apply(GizmoDragContext context, Vector3 currentMouseWorld)
    {
        Vector3 delta = currentMouseWorld - context.DragStartWorld;

        if (context.ActiveHandle.Axis == GizmoAxis.All)
        {
            float uniformDelta = (delta.x + delta.y) * context.ActiveHandle.ScaleSensitivity;
            uniformDelta = GizmoSnapUtility.SnapFloat(uniformDelta, context.SnappingSettings.scaleSnapSize, context.SnappingSettings.snappingEnabled);

            Vector3 result = context.TargetStartScale + Vector3.one * uniformDelta;
            context.ActiveTarget.localScale = ClampScale(result);
            return;
        }

        Vector3 axis = context.ActiveHandle.GetAxisVectorWorld().normalized;
        float projectedDistance = Vector3.Dot(delta, axis);

        float scaledAmount = projectedDistance * context.ActiveHandle.ScaleSensitivity;
        scaledAmount = GizmoSnapUtility.SnapFloat(scaledAmount, context.SnappingSettings.scaleSnapSize, context.SnappingSettings.snappingEnabled);

        Vector3 scaleDelta = context.ActiveHandle.Axis switch
        {
            GizmoAxis.X => new Vector3(scaledAmount, 0f, 0f),
            GizmoAxis.Y => new Vector3(0f, scaledAmount, 0f),
            GizmoAxis.Z => new Vector3(0f, 0f, scaledAmount),
            _ => Vector3.zero
        };

        Vector3 resultScale = context.TargetStartScale + scaleDelta;
        context.ActiveTarget.localScale = ClampScale(resultScale);

        float visualDragAmount = context.ActiveHandle.ScaleSensitivity != 0f ? scaledAmount / context.ActiveHandle.ScaleSensitivity : projectedDistance;

        UpdateActiveScaleHandleVisual(visualDragAmount, context.ActiveHandle); //nice visual feedback for the user to see how much they're scaling along the axis.
    }

    private void UpdateActiveScaleHandleVisual(float dragAmount, GizmoHandle activeHandle)
    {
        if (activeHandle is not GizmoScaleHandle scaleHandle)
            return;

        scaleHandle.UpdateScaleVisual(dragAmount);
    }

    //helper
    private Vector3 ClampScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Max(minScale, scale.x),
            Mathf.Max(minScale, scale.y),
            Mathf.Max(minScale, scale.z));
    }
}
