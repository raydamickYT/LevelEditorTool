using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// this class manages whatever is selected at that point in time, it will communicate this by sending out events for any class to read.
/// 
/// 25/03: it is currently only able to select single targets
/// </summary>
public class selectionController
{
    public Camera cam;
    private Dictionary<GameObject, SelectableTargetData> selectableGameObjectsInSceneDict = new();
    private SelectableTargetData _currentSelection;
    private HashSet<SelectableTargetData> _selectedGameObjects = new();
    private SelectionBoxView _selectionBoxView;
    private Vector2 _pressStartScreenPosition;
    private bool startedOnGizmo, isPointerDown, isBoxDragging;
    private float _dragThreshold = 10f;

    public selectionController(Camera cam, SelectionBoxView selectionBoxView)
    {
        this.cam = cam;
        _selectionBoxView = selectionBoxView;

        EventManager.Instance.AddDelegateListener("OnTrySelection", (Action<GameObject>)TrySelect);
    }

    //ik heb de selection controller een normale class gemaakt zodat de meeste logica daar uitgevoerd kan worden.
    public void OnStartLeftClick()
    {
        if (UIHelper.IsPointerOverUI()) return; 
        if (_currentSelection != null) //first check if we're clicking on the gizmo, if so we ignore the click and wait for the next one
        {
            if (RaycastHelper.IsClickingOnLayer(cam, LayerMask.GetMask("GizmoHandle")))
            {
                startedOnGizmo = true;
                return;
            }
        }

        _pressStartScreenPosition = Mouse.current.position.ReadValue();
        isPointerDown = true;

    }
    public void OnStopLeftClick()
    {
        if (!isPointerDown) return;

        isPointerDown = false;

        if (isBoxDragging) //check if we were dragging a selection.
        {
            SelectInRect(_selectionBoxView.Hide());
            isBoxDragging = false;
            return;
        }

        RaycastHit2D hit;
        if (startedOnGizmo) //if we clicked on the gizmo, we ignore this click and reset the bool for the next click
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

    public void TickSelectionInput()
    {
        if (!isPointerDown) return;

        Vector2 currentMousePosition = Mouse.current.position.ReadValue();
        if (!isBoxDragging &&
            Vector2.Distance(_pressStartScreenPosition, currentMousePosition) >= _dragThreshold)
        {
            isBoxDragging = true;
            // _selectionBoxView.Show();
            _selectionBoxView.Show();
        }

        if (isBoxDragging)
        {
            _selectionBoxView.UpdateBox(_pressStartScreenPosition, currentMousePosition);
            // _selectionBoxView.UpdateBox(_pressStartScreenPosition, currentMousePosition);
        }
    }
    public void Register(GameObject rootObject, SelectableTargetData data)
    {
        selectableGameObjectsInSceneDict[rootObject] = data;
    }

    public void Deregister(GameObject rootObject)
    {
        selectableGameObjectsInSceneDict.Remove(rootObject);
    }
    public void ClearDict()
    {
        selectableGameObjectsInSceneDict.Clear();
    }
    public void SelectInRect(Rect screenRect)
    {
        ClearSelection();

        foreach (var pair in selectableGameObjectsInSceneDict)
        {
            GameObject targetobj = pair.Key;
            SelectableTargetData targetData = pair.Value;

            Vector3 screenpoint3D = cam.WorldToScreenPoint(targetobj.transform.position);
            if (screenpoint3D.z < 0f) continue; //if the object is behind the camera, we ignore it

            Vector2 screenpoint = new Vector2(screenpoint3D.x, screenpoint3D.y);

            if (screenRect.Contains(screenpoint))
            {
                OnTargetSelected(targetData);
            }
        }

        //later kijken hoe de gizmo zich gedraagt bij het selecteren van meerdere objecten
    }

    public void TrySelect(GameObject selectedObject)
    {
        if (!selectableGameObjectsInSceneDict.ContainsKey(selectedObject)) return;


        if (selectableGameObjectsInSceneDict.TryGetValue(selectedObject, out SelectableTargetData obj)) //note to self: if you notice you use this more often than once, try making a helper
        {
            if (_currentSelection != obj)
            {
                if (_currentSelection != null)
                    ClearSelection();
                OnTargetSelected(obj);
            }
        }
    }
    public void AddToSelection(SelectableTargetData targetData)
    {
        if (targetData?.SelectableComponent == null)
            return;

        if (_selectedGameObjects.Add(targetData))
        {
            targetData.SelectableComponent.OnSelect();
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
        AddToSelection(targetData);
        // currentTargetList.Add(targetData);

        _currentSelection.SelectableComponent.OnSelect();
        //future: add multi selection logic

        EventManager.Instance.TriggerDelegate("OnShowGizmo", targetData);

    }

    public void ClearSelection()
    {
        if (_currentSelection == null) return;

        foreach (var item in _selectedGameObjects)
        {
            item.SelectableComponent.OnDeselect();
            item.SelectableComponent.OnHide();
        }
        _selectedGameObjects.Clear();
        //future: add multi selection logic

        _currentSelection.SelectableComponent.OnDeselect(); //single target
        EventManager.Instance.TriggerDelegate("OnHideGizmo", _currentSelection);

        _currentSelection = null;
        EventManager.Instance.TriggerUnityEvent("OnSelectionCleared");
        // currentTargetList.Clear();
    }
}

