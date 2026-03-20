using UnityEngine;


/// <summary>
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
    private GizmoType currentGizmoType;
    private GameObject target;

    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void SetTarget(GameObject _target)
    {
        target = _target;
    }
    public void Show()
    {
        if (target == null)
        {
            
        }
    }

    public void Hide()
    {
        
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

public enum GizmoVisibility
{
    visible,
    hidden
}

public class GizmoTargetData
{
    public GameObject BaseObject;
    public GameObject MoveGizmo;
    public GameObject RotateGizmo;
    public GameObject ScaleGizmo;
    public bool IsSelected;
}