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
public class GizmoController : MonoBehaviour
{
    private GizmoType currentGizmoType;
    private GizmoTargetData currentTarget;
    private List<GizmoTargetData> currentTargetList = new List<GizmoTargetData>(); //for future use when multi select is implemented, for now it only holds 1 target data, but it can be easily expanded to hold multiple target data when multi select is implemented.
    private Dictionary<GameObject, GizmoTargetData> targetGizmoDictionary = new();


    void Awake()
    {
        currentGizmoType = GizmoType.move;



        EventManager.Instance.AddDelegateListener("OnGizmoTypeChanged", (Action<GizmoType>)OnChangeGizmoType);
        EventManager.Instance.AddDelegateListener("OnRegisterToGizmoController", (Action<GameObject, GizmoTargetData>)RegisterToDict);
        EventManager.Instance.AddDelegateListener("OnDeRegisterToGizmoController", (Action<GameObject>)DeRegisterFromDict);



        EventManager.Instance.AddDelegateListener("OnObjectSelected", (Action<GameObject>)OnObjectSelected);
        EventManager.Instance.AddUnityEventListener("OnObjectDeselected", OnObjectDeselected);
    }

    public void AddSelectedTarget(GizmoTargetData _target)
    {
        currentTarget = _target;
        currentTargetList.Add(_target);
    }

    //TODO kijken waar ik deze call. ik denk bij "ondeselect"
    public void RemoveSelectedTarget()
    {
        currentTarget = null;
        currentTargetList.Clear();
    }

    private void RegisterToDict(GameObject gizmoBaseObject, GizmoTargetData targetData)
    {
        if (targetGizmoDictionary.ContainsKey(gizmoBaseObject)) //if the gizmo type already exists in the dictionary, update the target data
        {
            targetGizmoDictionary[gizmoBaseObject] = targetData;
        }
        else //if the gizmo type doesn't exist in the dictionary, add it to the dictionary
        {
            targetGizmoDictionary.Add(gizmoBaseObject, targetData);
        }
    }

    private void DeRegisterFromDict(GameObject gizmoBaseObject)
    {
        if (targetGizmoDictionary.ContainsKey(gizmoBaseObject)) //if the gizmo type already exists in the dictionary, update the target data
        {
            targetGizmoDictionary.Remove(gizmoBaseObject);
        }
    }

    private void OnObjectSelected(GameObject selectedObject)
    {
        Debug.Log("object registered");
        if (!targetGizmoDictionary.ContainsKey(selectedObject)) return;


        if (selectedObject.GetComponentInChildren<GizmoObject>() is GizmoObject gizmoObject)
        {
            if (currentTarget != gizmoObject.gizmoTargetData)
                OnTargetSelected(gizmoObject.gameObject, gizmoObject.gizmoTargetData);
        }
    }

    private void OnObjectDeselected()
    {
        if (currentTarget == null) return;
        OnTargetDeselected();
    }

    //this'll be the start of showing the gizmo, if the object is updated to "selected" the show function will be called
    private void OnTargetSelected(GameObject gizmoBaseObject, GizmoTargetData targetData)
    {
        if (targetData.SelectableComponent == null)
        {
            Debug.LogWarning($"Object is missing selectable component {targetData.BaseObject.name}");
            return;
        }

        targetData.SelectableComponent.OnSelect();
        if (currentTargetList.Count > 0)
        {
            foreach (GizmoTargetData gizmoObject in currentTargetList)
            {
                gizmoObject.SelectableComponent.OnSelect();
            }
        }

        targetData.SelectableComponent.OnShow(currentGizmoType);

        AddSelectedTarget(targetData); //make sure to save the target data to the controller, so it can be used in the show and hide functions
    }

    private void OnTargetDeselected()
    {
        Debug.Log("deselecting target");
        if (currentTargetList.Count > 0)
        {
            foreach (GizmoTargetData gizmoObject in currentTargetList)
            {
                gizmoObject.SelectableComponent.OnDeselect();
            }
        }

        currentTarget.SelectableComponent.OnDeselect(); //single target
        currentTarget.SelectableComponent.OnHide();

        RemoveSelectedTarget();
    }

    //deze functie mag alleen beheren of/welke gizmo aan staat, en welke uit staat. dus eigenlijk een combinatie van show en hide.
    public void OnChangeGizmoType(GizmoType newgizmoType)
    {
        if (currentGizmoType == newgizmoType) //if the gizmo type is the same as the current gizmo type, do nothing
            return;

        currentTarget.SelectableComponent.OnHide();
        currentTarget.SelectableComponent.OnShow(newgizmoType);
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