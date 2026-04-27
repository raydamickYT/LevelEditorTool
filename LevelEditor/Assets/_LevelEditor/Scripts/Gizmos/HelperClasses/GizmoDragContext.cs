using UnityEngine;

public class GizmoDragContext
{
    public GizmoHandle ActiveHandle { get; }
    public Transform ActiveTarget { get; }

    public Vector3 DragStartWorld { get; }
    public Vector3 TargetStartPosition { get; }
    public Vector3 TargetStartScale { get; }
    public float TargetStartRotationZ { get; }
    public float StartMouseAngle { get; }

    public SnappingSettings SnappingSettings { get; }

    public GizmoDragContext(
        GizmoHandle activeHandle,
        Transform activeTarget,
        Vector3 dragStartWorld,
        Vector3 targetStartPosition,
        Vector3 targetStartScale,
        float targetStartRotationZ,
        float startMouseAngle,
        SnappingSettings snappingSettings)
    {
        ActiveHandle = activeHandle;
        ActiveTarget = activeTarget;
        DragStartWorld = dragStartWorld;
        TargetStartPosition = targetStartPosition;
        TargetStartScale = targetStartScale;
        TargetStartRotationZ = targetStartRotationZ;
        StartMouseAngle = startMouseAngle;
        SnappingSettings = snappingSettings;
    }
}