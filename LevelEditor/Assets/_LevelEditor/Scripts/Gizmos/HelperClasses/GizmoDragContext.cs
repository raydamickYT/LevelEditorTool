using UnityEngine;


/// <summary>
/// this class is used to store all the relevant information about a drag operation on a gizmo, so that it can be passed around to various utility functions without having to pass a long list of parameters.
/// </summary>
public class GizmoDragContext
{
    // which of the three handles is being dragged (or All for free movement)
    public GizmoHandle ActiveHandle { get; }
    public Transform ActiveTarget { get; }

    public Vector3 DragStartWorld { get; }
    public Vector3 TargetStartPosition { get; }
    public Vector3 TargetStartScale { get; }
    public float TargetStartRotationZ { get; }
    public float StartMouseAngle { get; }

    public SnappingSettings SnappingSettings { get; }

    //if this is false, then edge snapping will not work (grid snapping will still work)
    public bool HasSelectionBoundsWorld { get; }
    //Combined world bounds of the selection at drag start (for edge snapping).
    public Bounds SelectionBoundsWorldAtDragStart { get; }

    public GizmoDragContext(
        GizmoHandle activeHandle,
        Transform activeTarget,
        Vector3 dragStartWorld,
        Vector3 targetStartPosition,
        Vector3 targetStartScale,
        float targetStartRotationZ,
        float startMouseAngle,
        SnappingSettings snappingSettings,
        Bounds selectionBoundsWorldAtDragStart,
        bool hasSelectionBoundsWorld)
    {
        ActiveHandle = activeHandle;
        ActiveTarget = activeTarget;
        DragStartWorld = dragStartWorld;
        TargetStartPosition = targetStartPosition;
        TargetStartScale = targetStartScale;
        TargetStartRotationZ = targetStartRotationZ;
        StartMouseAngle = startMouseAngle;
        SnappingSettings = snappingSettings;
        SelectionBoundsWorldAtDragStart = selectionBoundsWorldAtDragStart;
        HasSelectionBoundsWorld = hasSelectionBoundsWorld;
    }
}