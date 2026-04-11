using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this class manages whatever is selected at that point in time, it will communicate this by sending out events for any class to read.
/// 
/// 25/03: it is currently only able to select single targets
/// </summary>
public class selectionController
{
    public Camera cam;
    private Dictionary<GameObject, SelectableTargetData> selectedGameObjectsDictionary = new();
    private SelectableTargetData _currentSelection;
    private bool startedOnGizmo;

    public selectionController(Camera cam)
    {
        this.cam = cam;

        EventManager.Instance.AddDelegateListener("OnTrySelection", (Action<GameObject>)TrySelect);
    }

    //ik heb de selection controller een normale class gemaakt zodat de meeste logica daar uitgevoerd kan worden.
    public void OnStartLeftClick()
    {
        if (_currentSelection != null) //first check if we're clicking on the gizmo, if so we ignore the click and wait for the next one
        {
            if (RaycastHelper.IsClickingOnLayer(cam, LayerMask.GetMask("GizmoHandle")))
            {
                startedOnGizmo = true;
                return;
            }
        }
    }
    public void OnStopLeftClick()
    {
        RaycastHit2D hit;
        if(startedOnGizmo) //if we clicked on the gizmo, we ignore this click and reset the bool for the next click
        {
            startedOnGizmo = false;
            return;
        }

        if (RaycastHelper.TryGetPointerHit2D(cam, LayerMask.GetMask("Selectable"), out hit)) //then we get to the selection logic, if we hit something with the selectable layer, we try to select it
        {
            TrySelect(hit.collider.gameObject);
            return;
        }
        else if (!UIHelper.IsPointerOverUI())
        {
            ClearSelection();
        }
    }


    public void Register(GameObject rootObject, SelectableTargetData data)
    {
        selectedGameObjectsDictionary[rootObject] = data;
    }

    public void Deregister(GameObject rootObject)
    {
        selectedGameObjectsDictionary.Remove(rootObject);
    }
    public void ClearDict()
    {
        selectedGameObjectsDictionary.Clear();
    }

    public void StartSelectionBox()
    {
        
    }

    public void StopSelectionBox()
    {

    }

    public void TrySelect(GameObject selectedObject)
    {
        if (!selectedGameObjectsDictionary.ContainsKey(selectedObject)) return;


        if (selectedGameObjectsDictionary.TryGetValue(selectedObject, out SelectableTargetData obj)) //note to self: if you notice you use this more often than once, try making a helper
        {
            if (_currentSelection != obj)
            {
                if (_currentSelection != null)
                    ClearSelection();
                OnTargetSelected(obj);
            }
        }
    }

    //this'll be the start of showing the gizmo, if the object is updated to "selected" the show function will be called
    private void OnTargetSelected(SelectableTargetData targetData)
    {
        if (targetData.SelectableComponent == null)
        {
            Debug.LogWarning($"Object is missing selectable component {targetData.BaseObject.name}");
            return;
        }
        _currentSelection = targetData;
        // currentTargetList.Add(targetData);

        _currentSelection.SelectableComponent.OnSelect();
        //future: add multi selection logic

        EventManager.Instance.TriggerDelegate("OnShowGizmo", targetData);

    }

    public void ClearSelection()
    {
        if (_currentSelection == null) return;

        //future: add multi selection logic

        _currentSelection.SelectableComponent.OnDeselect(); //single target
        EventManager.Instance.TriggerDelegate("OnHideGizmo", _currentSelection);

        _currentSelection = null;
        EventManager.Instance.TriggerUnityEvent("OnSelectionCleared");
        // currentTargetList.Clear();
    }
}

