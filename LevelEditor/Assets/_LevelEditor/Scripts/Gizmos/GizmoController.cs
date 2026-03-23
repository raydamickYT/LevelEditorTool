using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
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
/// </summary>
public class GizmoController : MonoBehaviour
{
    public static GizmoController Instance { get; private set; }
    private GizmoType currentGizmoType, newGizmoType;
    private GizmoTargetData target;
    private Dictionary<GameObject, GizmoTargetData> targetGizmoDictionary = new();


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        EventManager.Instance.AddDelegateListener("OnGizmoSelectionChanged", (Action<GameObject, GizmoType>)OnChangeGizmoType);
        EventManager.Instance.AddDelegateListener("OnGizmoCreated", (Action<GameObject, GizmoTargetData>)UpdateDictionary);

        EventManager.Instance.AddUnityEventListener("OnHideGizmo", hide);
    }

    public void SetTarget(GizmoTargetData _target)
    {
        target = _target;
    }
    public void ClearTarget()
    {
        target = null;
    }

    //deze functie mag alleen beheren of/welke gizmo aan staat.
    private void show()
    {
        if (target == null)
            return;

        switch (newGizmoType)
        {
            case GizmoType.none:
                hide();
                break;
            case GizmoType.move:
                target.MoveGizmo.SetActive(true);
                break;
            case GizmoType.rotate:
                target.RotateGizmo.SetActive(true);
                break;
            case GizmoType.scale:
                target.ScaleGizmo.SetActive(true);
                break;
            default:
                break;
        }

        currentGizmoType = newGizmoType;
        newGizmoType = GizmoType.none;
        ClearTarget(); //make sure to clear the target after changing the gizmo. This controller only needs to know what to switch to nothing else.
    }

    //deze functie mag alleen beheren of/welke gizmo uit staat.
    // This function should be allowed to be 
    private void hide()
    {
        if (target == null)
            return;

        switch (currentGizmoType)
        {
            case GizmoType.none:
                break;
            case GizmoType.move:
                target.MoveGizmo.SetActive(false);
                break;
            case GizmoType.rotate:
                target.RotateGizmo.SetActive(false);
                break;
            case GizmoType.scale:
                target.ScaleGizmo.SetActive(false);
                break;
            default:
                break;
        }
    }

    private void UpdateDictionary(GameObject gizmoBaseObject, GizmoTargetData targetData)
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

    //deze functie mag alleen beheren of/welke gizmo aan staat, en welke uit staat. dus eigenlijk een combinatie van show en hide.
    public void OnChangeGizmoType(GameObject targetKey, GizmoType newgizmoType)
    {
        if (currentGizmoType == newgizmoType) //if the gizmo type is the same as the current gizmo type, do nothing
            return;

        newGizmoType = newgizmoType;

        if (!targetGizmoDictionary.TryGetValue(targetKey, out GizmoTargetData targetData))
        {
            Debug.LogWarning("following object could not be found: " + targetKey);
            return;
        }

        if (targetData != null && targetData.IsSelected) //if target data exists and is selected, switch the gizmo
        {
            SetTarget(targetData); //make sure to save the target data to the controller, so it can be used in the show and hide functions
            hide();
            show();
            return;
        }

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
    public GameObject BaseObject; //dit werkt ook als anchor voor de gizmo, zodat die altijd op dezelfde plek zit als het object
    public GameObject MoveGizmo; //de sprite/ het 3D object van de gizmo
    public GameObject RotateGizmo;
    public GameObject ScaleGizmo;
    public bool IsSelected;
}