/// <summary>
/// This class only manager the gizmo types and makes sure that the right object is displaying the right gizmo type
/// </summary>
public class GizmoController
{
    private GizmoType currentGizmoType = GizmoType.move;
    private SelectableTargetData currentTarget;
    
    public void ShowGizmo(SelectableTargetData data)
    {
        data.SelectableComponent?.OnShow(currentGizmoType);
        currentTarget = data;
    }

    public void HideGizmo(SelectableTargetData data)
    {
        data.SelectableComponent?.OnHide();

        if (currentTarget == data)
            ClearTarget();
    }

    public void ClearTarget()
    {
        if (currentTarget != null)
            currentTarget = null;
    }

    //deze functie mag alleen beheren of/welke gizmo aan staat, en welke uit staat. dus eigenlijk een combinatie van show en hide.
    public void SetGizmoType(GizmoType newgizmoType)
    {
        if (currentGizmoType == newgizmoType) //if the gizmo type is the same as the current gizmo type, do nothing
            return;

        currentTarget.SelectableComponent?.OnHide();

        currentGizmoType = newgizmoType;
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

