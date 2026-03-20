using UnityEngine;

public class PointerHitContext
{
    public Collider2D Collider;
    public ISelectable Selectable;
    public IGizmoHandle GizmoHandle;
    public bool HitSomething;
}

public interface IGizmoHandle
{
}