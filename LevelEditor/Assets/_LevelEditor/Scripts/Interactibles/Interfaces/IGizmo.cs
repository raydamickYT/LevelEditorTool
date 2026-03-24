using UnityEngine;

public interface IGizmoObject : ISelectable
{
 public void OnShow(GizmoType gizmoType);
 public void OnHide();
}
