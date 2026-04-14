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
    // private SelectableTargetData _currentSelection;
    private HashSet<SelectableTargetData> _selectedGameObjects = new();
    // private SelectionBoxBase _selectionBoxView;
    private Vector2 _pressStartScreenPosition;
    private bool startedOnGizmo, isPointerDown, isBoxDragging;
    private float _dragThreshold = 10f;

    public selectionController(Camera cam)
    {
        this.cam = cam;

        EventManager.Instance.AddDelegateListener(SelectionEvents.OnTrySelection, (Action<GameObject>)TrySelect);
    }

    //ik heb de selection controller een normale class gemaakt zodat de meeste logica daar uitgevoerd kan worden.
    public void OnStartLeftClick()
    {
        if (UIHelper.IsPointerOverUI()) return;

        startedOnGizmo = false;

        if (RaycastHelper.IsClickingOnLayer(cam, LayerMask.GetMask("GizmoHandle"))) //first check if we're clicking on the gizmo, if so we ignore the click and wait for the next one
        {
            startedOnGizmo = true;
            return;
        }

        _pressStartScreenPosition = Mouse.current.position.ReadValue();
        isPointerDown = true;
    }

    public void OnStopLeftClick()
    {
        if (startedOnGizmo)
        {
            startedOnGizmo = false;
            isPointerDown = false;
            isBoxDragging = false;
            return;
        }
        if (!isPointerDown) return;

        isPointerDown = false;

        if (isBoxDragging) //check if we were dragging a selection.
        {
            Rect rect = SelectionEvents.FinalizeSelectionRect?.Invoke() ?? default;
            EventManager.Instance.TriggerUnityEvent(SelectionEvents.HideSelectionBox);

            SelectInRect(rect);
            isBoxDragging = false;
            return;
        }

        if (RaycastHelper.TryGetPointerHit2D(cam, LayerMask.GetMask("Selectable"), out RaycastHit2D hit)) //then we get to the selection logic, if we hit something with the selectable layer, we try to select it
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
            EventManager.Instance.TriggerUnityEvent(SelectionEvents.ShowSelectionBox);
        }

        if (isBoxDragging)
        {
            EventManager.Instance.TriggerDelegate(SelectionEvents.UpdateSelectionBox, _pressStartScreenPosition, currentMousePosition);
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


            if (IsObjectInsideScreenRect(targetobj, screenRect))
            {
                Debug.Log(targetobj.transform.position);
                if (targetobj.layer == LayerMask.NameToLayer("Selectable"))
                {
                    AddToSelection(targetData);
                }
            }
        }

        RefreshGizmo();
        //later kijken hoe de gizmo zich gedraagt bij het selecteren van meerdere objecten
    }
    private bool IsObjectInsideScreenRect(GameObject targetObject, Rect screenRect)
    {
        Collider2D collider = targetObject.GetComponent<Collider2D>();
        if (collider == null)
            return false;

        Bounds bounds = collider.bounds;

        Vector3 bottomLeft = cam.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.center.z));
        Vector3 topRight = cam.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.center.z));
        Vector3 topLeft = cam.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.center.z));
        Vector3 bottomRight = cam.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.center.z));

        if (bottomLeft.z < 0f || topRight.z < 0f || topLeft.z < 0f || bottomRight.z < 0f)
            return false;

        float minX = Mathf.Min(bottomLeft.x, topRight.x, topLeft.x, bottomRight.x);
        float maxX = Mathf.Max(bottomLeft.x, topRight.x, topLeft.x, bottomRight.x);
        float minY = Mathf.Min(bottomLeft.y, topRight.y, topLeft.y, bottomRight.y);
        float maxY = Mathf.Max(bottomLeft.y, topRight.y, topLeft.y, bottomRight.y);

        Rect objectScreenRect = Rect.MinMaxRect(minX, minY, maxX, maxY);

        return screenRect.Overlaps(objectScreenRect, true);
    }
    
    public void TrySelect(GameObject selectedObject)
    {
        if (!selectableGameObjectsInSceneDict.TryGetValue(selectedObject, out SelectableTargetData obj))
            return;

        Debug.Log(_selectedGameObjects.Contains(obj));
        if (_selectedGameObjects.Count >= 1 && _selectedGameObjects.Contains(obj))
            return;

        ClearSelection();
        AddToSelection(obj);
        RefreshGizmo();
    }

    public void AddToSelection(SelectableTargetData targetData)
    {
        if (targetData?.SelectableComponent == null)
        {
            Debug.LogWarning($"Object is missing selectable component {targetData.BaseObject.name}");
            return;
        }

        if (_selectedGameObjects.Add(targetData))
        {
            targetData.SelectableComponent.OnSelect();
        }
    }

    private void RefreshGizmo()
    {
        Debug.Log("selected objects count: " + _selectedGameObjects.Count);
        EventManager.Instance.TriggerDelegate(SelectionEvents.OnSelectionChanged, _selectedGameObjects);
    }

    public void ClearSelection()
    {
        if (_selectedGameObjects.Count == 0) return;

        foreach (var item in _selectedGameObjects)
        {
            item.SelectableComponent?.OnDeselect();
        }

        _selectedGameObjects.Clear();
        RefreshGizmo();
    }
}

