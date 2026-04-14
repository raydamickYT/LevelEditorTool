using System;
using UnityEngine;

public interface IGizmoObject
{
 public void OnShow(GizmoType gizmoType);
 public void OnHide();
}


[Serializable]
public class GizmoTargetData
{
    public GizmoType type;
    public GameObject BaseObject; //dit werkt ook als anchor voor de gizmo, zodat die altijd op dezelfde plek zit als het object
    public GameObject MoveGizmo; //de sprite/ het 3D object van de gizmo
    public GameObject RotateGizmo;
    public GameObject ScaleGizmo;
}