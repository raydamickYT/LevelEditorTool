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
    private GizmoObject groupGizmoObject;
    private HashSet<SelectableTargetData> _currentGizmoTargets = new();

    private bool groupGizmoActive = false;

    public GizmoController(GizmoObject _groupGizmoObject)
    {
        groupGizmoObject = _groupGizmoObject;
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
                    // t.SelectableComponent?.OnShow(currentGizmoType);
                    _currentGizmoTargets.Add(t);
                }
                ShowGroupGizmo();
                break;
        }
    }

    private void ShowGroupGizmo()
    {
        if (_currentGizmoTargets == null || _currentGizmoTargets.Count == 0)
            return;

        if (groupGizmoObject == null)
        {
            Debug.LogError("No groupgizmoObject assigned.");
            return;
        }

        Vector3 center = GetSelectionBoundsCenter(_currentGizmoTargets);

        groupGizmoObject.gizmoTargetData.BaseObject = SetupTempParentObject(center);
        groupGizmoObject.gameObject.transform.position = center;
        groupGizmoObject?.OnSelect();
        groupGizmoObject?.OnShow(currentGizmoType);
        groupGizmoActive = true;
    }

    public Vector3 GetSelectionBoundsCenter(HashSet<SelectableTargetData> selection)
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

    public Vector3 GetSelectionCenter(HashSet<SelectableTargetData> selection)
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

    public void HideCurrentGizmos()
    {
        switch (_currentGizmoTargets.Count)
        {
            case 1:
                var target = _currentGizmoTargets.FirstOrDefault();
                target?.SelectableComponent?.OnHide();
                break;
            default:
                ClearTempParent();
                groupGizmoActive = false;
                groupGizmoObject?.OnHide();
                groupGizmoObject?.OnDeselect();
                break;
        }

        _currentGizmoTargets.Clear();
    }

    public void SetGizmoType(GizmoType newGizmoType)
    {
        if (currentGizmoType == newGizmoType)
            return;

        currentGizmoType = newGizmoType;

        switch (groupGizmoActive)
        {
            case false:
                foreach (var target in _currentGizmoTargets)
                {
                    target.SelectableComponent?.OnHide();
                    target.SelectableComponent?.OnShow(currentGizmoType);
                }
                break;
            default:
                groupGizmoObject?.OnHide();
                groupGizmoObject?.OnShow(currentGizmoType);
                break;
        }

    }

    public void ClearTempParent()
    {
        if (tempParentObject != null)
        {
            foreach (var target in _currentGizmoTargets)
            {
                target.BaseObject.transform.SetParent(null);
            }
            // Object.Destroy(tempParentObject); //I could do this but I think it'd be nicer to just reuse the same temp parent object.
            // tempParentObject = null;
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

