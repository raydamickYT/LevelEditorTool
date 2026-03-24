using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// deze class zit op de gizmo prefab, en is verantwoordelijk voor het beheren van de gizmo's.
/// dat betekent dat een aantal dingen lokaal beheert kunnen worden, zoals: welke gizmo zichtbaar is, de positie van de gizmo, etc.
/// ook het opslaan van de gizmo types kan hier gebeuren.
/// 
/// 
/// deze class moet als eerste kunnen: draggen, moven, roteren, schalen van geselecteerde objecten kunnen doen.
/// het moet hierom weten: welk object geselecteerd is, welke gizmo aan dat object is gekoppeld, de gizmo zichbaar en onzichtbaar maken.
/// 
/// voor nu: deze controller houd bij
/// welke target er beheert wordt nu, en welke gizmo daar aan gekoppeld is.
/// Hij toon en verbergt ook de gizmo, en update de positie van de gizmo.
/// 
/// dus nog geen: drag logica, selectie bepalen of input lezen.
/// 
/// verdere updates: multi mode single mode, group pivot, shared gizmo behaviour, ect.
/// </summary>
public class GizmoController
{
    private GizmoType currentGizmoType = GizmoType.move;
    private GizmoTargetData currentTarget;
    private List<GizmoTargetData> currentTargetList = new List<GizmoTargetData>(); //for future use when multi select is implemented, for now it only holds 1 target data, but it can be easily expanded to hold multiple target data when multi select is implemented.
    private Dictionary<GameObject, GizmoTargetData> targetGizmoDictionary = new();

    public void Register(GameObject rootObject, GizmoTargetData data)
    {
        targetGizmoDictionary[rootObject] = data;
    }

    public void Deregister(GameObject rootObject)
    {
        targetGizmoDictionary.Remove(rootObject);
    }

    public void TrySelect(GameObject selectedObject)
    {
        if (!targetGizmoDictionary.ContainsKey(selectedObject)) return;


        if (targetGizmoDictionary.TryGetValue(selectedObject, out GizmoTargetData gizmoObject)) //note to self: if you notice you use this more often than once, try making a helper
        {
            if (currentTarget != gizmoObject)
            {
                if (currentTarget != null)
                    ClearSelection();
                OnTargetSelected(gizmoObject);
            }
        }
    }

    //this'll be the start of showing the gizmo, if the object is updated to "selected" the show function will be called
    private void OnTargetSelected(GizmoTargetData targetData)
    {
        if (targetData.SelectableComponent == null)
        {
            Debug.LogWarning($"Object is missing selectable component {targetData.BaseObject.name}");
            return;
        }
        currentTarget = targetData;
        currentTargetList.Add(targetData);

        //future: add multi selection logic

        currentTarget.SelectableComponent.OnSelect();
        currentTarget.SelectableComponent.OnShow(currentGizmoType);

    }

    public void ClearSelection()
    {
        if (currentTarget == null) return;

        //future: add multi selection logic

        currentTarget.SelectableComponent.OnDeselect(); //single target
        currentTarget.SelectableComponent.OnHide();

        currentTarget = null;
        currentTargetList.Clear();
    }

    //deze functie mag alleen beheren of/welke gizmo aan staat, en welke uit staat. dus eigenlijk een combinatie van show en hide.
    public void SetGizmoType(GizmoType newgizmoType)
    {
        if (currentGizmoType == newgizmoType) //if the gizmo type is the same as the current gizmo type, do nothing
            return;

        currentTarget.SelectableComponent.OnHide();

        currentGizmoType = newgizmoType;
        currentTarget.SelectableComponent.OnShow(currentGizmoType);
    }

    public void UpdatePosition()
    {

    }
}

public enum GizmoType
{
    none,
    move,
    rotate,
    scale
}


// ik ga er van uit dat gizmo's als prefabs opgeslagen gaan worden, waarbij ze altijd alle gizmo's bij zich hebben. maar dat die allemaal disabled zijn.
// deze tool is altijd 2D dus view reference is altijd hetzelfde
[Serializable]
public class GizmoTargetData
{
    public GizmoType type;
    public GameObject BaseObject; //dit werkt ook als anchor voor de gizmo, zodat die altijd op dezelfde plek zit als het object
    public GameObject MoveGizmo; //de sprite/ het 3D object van de gizmo
    public GameObject RotateGizmo;
    public GameObject ScaleGizmo;
    public bool IsSelected;
    public IGizmoObject SelectableComponent; //de component die geselecteerd kan worden, deze heeft de select en deselect functies, zodat de gizmo controller niet hoeft te weten wat voor object het is, zolang het maar een ISelectable component heeft.
}