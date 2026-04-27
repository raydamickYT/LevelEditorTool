using System.Net.Mime;
using UnityEngine;

public class GizmoRotationOperation : IGizmoTransformOperation
{
    public void Apply(GizmoDragContext context, Vector3 currentMouseWorld)
    {
        float currentAngle = GetMouseAngleToTarget(context.ActiveTarget.position, currentMouseWorld);
        float deltaAngle = Mathf.DeltaAngle(context.StartMouseAngle, currentAngle);

        deltaAngle *= context.ActiveHandle.RotationSensitivity;
        deltaAngle = GizmoSnapUtility.SnapFloat(deltaAngle, context.SnappingSettings.rotateSnapAngle, context.SnappingSettings.snappingEnabled);

        float finalAngle = context.TargetStartRotationZ + deltaAngle;

        context.ActiveTarget.rotation = Quaternion.Euler(0f, 0f, finalAngle);
    }

    private float GetMouseAngleToTarget(Vector3 targetPosition, Vector3 currentMouseWorld)
    {
        Vector3 mouseWorld = currentMouseWorld;
        Vector2 dir = mouseWorld - targetPosition;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }
}
