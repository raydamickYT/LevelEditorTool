using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This class only manager the gizmo types and makes sure that the right object is displaying the right gizmo type
/// </summary>
public class GizmoController
{
    private GizmoType currentGizmoType = GizmoType.move;
    private SelectableTargetData currentTarget;
    private HashSet<SelectableTargetData> _currentGizmoTargets = new();

    public GizmoType CurrentGizmoType => currentGizmoType;

    public void ShowGizmo(SelectableTargetData data)
    {
        if (data == null)
            return;

        currentTarget = data;
        currentTarget.SelectableComponent?.OnShow(currentGizmoType);
    }

    public void HandleSelectionChanged(HashSet<SelectableTargetData> data)
    {
        HideCurrentGizmos(); //having this at the top will make sure that all gizmo's are hidden even if there's none selected.
        if (data == null || data.Count == 0)
            return;

        switch (data.Count)
        {
            case 1:
                var target = data.FirstOrDefault();
                target?.SelectableComponent?.OnShow(currentGizmoType);

                if (target != null)
                    _currentGizmoTargets.Add(target);
                break;
            default:
                foreach (var t in data)
                {
                    t.SelectableComponent?.OnShow(currentGizmoType);
                    _currentGizmoTargets.Add(t);
                }
                break;
        }
    }

    public void HideCurrentGizmos()
    {
        foreach (var target in _currentGizmoTargets)
        {
            target.SelectableComponent?.OnHide();
        }

        _currentGizmoTargets.Clear();
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

