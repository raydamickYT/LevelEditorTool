using System;
using UnityEngine;

public interface ISelectable
{

    bool IsSelected { get; }
    void OnSelect();
    void OnDeselect();
}

// ik ga er van uit dat gizmo's als prefabs opgeslagen gaan worden, waarbij ze altijd alle gizmo's bij zich hebben. maar dat die allemaal disabled zijn.
// deze tool is altijd 2D dus view reference is altijd hetzelfde
[Serializable]
public class SelectableTargetData
{
    public bool IsSelected;
    public GameObject BaseObject; //dit werkt ook als anchor voor de gizmo, zodat die altijd op dezelfde plek zit als het object
    public ISelectable SelectableComponent; //de component die geselecteerd kan worden, deze heeft de select en deselect functies, zodat de gizmo controller niet hoeft te weten wat voor object het is, zolang het maar een ISelectable component heeft.
}
