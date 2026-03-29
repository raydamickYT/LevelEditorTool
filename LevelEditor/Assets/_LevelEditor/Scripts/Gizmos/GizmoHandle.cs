using UnityEngine;

public class GizmoHandle : MonoBehaviour
{
    [SerializeField] private GizmoObject owner;
    [SerializeField] private GizmoHandleMode mode;
    [SerializeField] private GizmoAxis axis = GizmoAxis.All;
    [SerializeField] private float rotationSensitivity = 0.25f;
    [SerializeField] private float scaleSensitivity = 0.01f;

    public GizmoObject Owner => owner;
    public GizmoHandleMode Mode => mode;
    public GizmoAxis Axis => axis;
    public float RotationSensitivity => rotationSensitivity;
    public float ScaleSensitivity => scaleSensitivity;

    private void Reset()
    {
        if (owner == null)
            owner = GetComponentInParent<GizmoObject>();
    }

    public Vector3 GetAxisVectorWorld()
    {
        if (owner == null || owner.TargetTransform == null)
            return Vector3.right;

        Transform target = owner.TargetTransform;

        return axis switch
        {
            GizmoAxis.X => target.right,
            GizmoAxis.Y => target.up,
            GizmoAxis.Z => target.forward,
            _ => Vector3.one.normalized
        };
    }
}

public enum GizmoHandleMode
{
    Move,
    Rotate,
    Scale
}

public enum GizmoAxis
{
    X,
    Y,
    Z,
    All
}