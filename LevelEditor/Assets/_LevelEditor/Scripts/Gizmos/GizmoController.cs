using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// This class only manager the gizmo types and makes sure that the right object is displaying the right gizmo type
/// </summary>
public class GizmoController
{
    private GameObject tempParentObject;
    private GizmoType currentGizmoType = GizmoType.move;
    private GizmoObject gizmoObject;
    private HashSet<SelectableTargetData> _currentGizmoTargets = new();


    public GizmoController(GizmoObject _gizmoObject)
    {
        gizmoObject = _gizmoObject;
    }

    public void HandleSelectionChanged(HashSet<SelectableTargetData> data)
    {
        HideCurrentGizmos(); //having this at the top will make sure that all gizmo's are hidden even if there's none selected.
        if (data == null || data.Count == 0)
        {
            gizmoObject?.ClearTarget();
            return;
        }

        switch (data.Count)
        {
            case 1:
                var target = data.FirstOrDefault();
                ShowSingleGizmo(target);

                if (target != null)
                    _currentGizmoTargets.Add(target);
                break;
            default:
                foreach (var t in data)
                {
                    _currentGizmoTargets.Add(t);
                }
                ShowGroupGizmo();
                break;
        }
    }

    private void ShowSingleGizmo(SelectableTargetData target)
    {
        if (gizmoObject == null)
        {
            Debug.LogError("No groupgizmoObject assigned.");
            return;
        }

        if (target.BaseObject == null)
        {
            Debug.LogError("Target baseobject is null for: " + target.BaseObject.name);
            return;
        }

        gizmoObject.SetTarget(target);
        gizmoObject.gameObject.transform.position = target.BaseObject.transform.position;
        gizmoObject?.OnShow(currentGizmoType);
    }

    private void ShowGroupGizmo()
    {
        if (_currentGizmoTargets == null || _currentGizmoTargets.Count == 0)
            return;

        if (gizmoObject == null)
        {
            Debug.LogError("No groupgizmoObject assigned.");
            return;
        }

        Vector3 center = SelectionBoundsCalculator.GetSelectionBoundsCenter(_currentGizmoTargets);

        gizmoObject.gizmoTargetData.BaseObject = SetupTempParentObject(center);
        gizmoObject.gameObject.transform.position = center;
        gizmoObject?.OnShow(currentGizmoType);
    }

    public void HideCurrentGizmos(bool clearTarget = false)
    {
        if (_currentGizmoTargets.Count != 1)
            ClearTempParent();

        gizmoObject?.OnHide();

        if (clearTarget)
            gizmoObject?.ClearTarget();

        _currentGizmoTargets.Clear();
    }

    public void SetGizmoType(GizmoType newGizmoType)
    {
        if (currentGizmoType == newGizmoType)
            return;

        currentGizmoType = newGizmoType;

        gizmoObject?.OnHide();
        gizmoObject?.OnShow(currentGizmoType);

    }

    public void ClearTempParent()
    {
        if (tempParentObject != null)
        {
            foreach (var target in _currentGizmoTargets)
            {
                target.BaseObject.transform.SetParent(null);
            }

            tempParentObject.SetActive(false);
        }
    }

    public GameObject SetupTempParentObject(Vector3 center)
    {
        switch (tempParentObject)
        {
            case not null:
                tempParentObject.transform.position = center;
                tempParentObject.SetActive(true);

                foreach (var target in _currentGizmoTargets)
                {
                    target.BaseObject.transform.SetParent(tempParentObject.transform);
                }

                return tempParentObject;
            default:
                GameObject tempParent = new GameObject("GizmoTempParent");
                tempParent.transform.position = center;
                tempParent.transform.rotation = Quaternion.identity;
                tempParent.SetActive(true);

                foreach (var target in _currentGizmoTargets)
                {
                    target.BaseObject.transform.SetParent(tempParent.transform);
                }
                tempParentObject = tempParent;

                break;
        }

        return tempParentObject;
    }
}

public enum GizmoType
{
    none,
    move,
    rotate,
    scale
}

public static class SelectionBoundsCalculator
{
    public static Vector3 GetSelectionBoundsCenter(HashSet<SelectableTargetData> selection)
    {
        if (selection == null || selection.Count == 0)
            return Vector3.zero;

        bool hasBounds = false;
        Bounds combinedBounds = default;

        foreach (var target in selection)
        {
            Collider2D collider = target.BaseObject.GetComponent<Collider2D>();
            if (collider == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(collider.bounds);
            }
        }

        if (!hasBounds)
            return GetSelectionCenter(selection);

        return combinedBounds.center;
    }

    public static Vector3 GetSelectionCenter(HashSet<SelectableTargetData> selection)
    {
        if (selection == null || selection.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;

        foreach (var target in selection)
        {
            sum += target.BaseObject.transform.position;
        }

        return sum / selection.Count;
    }
}