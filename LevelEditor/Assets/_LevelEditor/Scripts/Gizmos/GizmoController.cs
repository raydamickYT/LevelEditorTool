/// <summary>
/// This class only manager the gizmo types and makes sure that the right object is displaying the right gizmo type
/// </summary>
public class GizmoController
{
    private GizmoType currentGizmoType = GizmoType.move;
    private SelectableTargetData currentTarget;

    public SelectableTargetData CurrentTarget => currentTarget;
    public GizmoType CurrentGizmoType => currentGizmoType;

    public void ShowGizmo(SelectableTargetData data)
    {
        if (data == null)
            return;

        currentTarget = data;
        currentTarget.SelectableComponent?.OnShow(currentGizmoType);
    }

    public void HideGizmo(SelectableTargetData data)
    {
        if (data == null)
            return;

        data.SelectableComponent?.OnHide();

        if (currentTarget == data)
            ClearTarget();
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }

    public void SetGizmoType(GizmoType newGizmoType)
    {
        if (currentGizmoType == newGizmoType)
            return;

        if (currentTarget != null)
            currentTarget.SelectableComponent?.OnHide();

        currentGizmoType = newGizmoType;

        if (currentTarget != null)
            currentTarget.SelectableComponent?.OnShow(currentGizmoType);
    }
}

public enum GizmoType
{
    none,
    move,
    rotate,
    scale
}

