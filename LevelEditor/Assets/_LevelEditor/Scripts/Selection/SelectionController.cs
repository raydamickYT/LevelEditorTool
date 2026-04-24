using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

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

    //undo
    private DeleteAction deleteAction;

    public selectionController(Camera cam)
    {
        this.cam = cam;
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

    public void TryDeleteSelected()
    {
        if (_selectedGameObjects.Count == 0) return;

        EventManager.Instance.TriggerDelegate(SelectionEvents.OnSelectionChanged, new HashSet<SelectableTargetData>()); //clear the data in gizmo controller to prevent the gizmo object from beeing deleted

        switch (_selectedGameObjects.Count)
        {
            case 1:
                var obj = _selectedGameObjects.FirstOrDefault();
                var lvlObj = obj.BaseObject.GetComponent<LevelObject>();
                if (lvlObj != null)
                {
                    deleteAction = new DeleteAction(lvlObj);
                }
                if (deleteAction != null)
                {
                    EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, deleteAction);
                }
                GameObject.Destroy(obj.BaseObject);
                break;
            default:
                var actionList = new List<IUndoableAction>();

                foreach (var item in _selectedGameObjects)
                {
                    var lvlObj2 = item.BaseObject.GetComponent<LevelObject>();
                    if (lvlObj2 != null)
                    {
                        var action = new DeleteAction(lvlObj2);
                        actionList.Add(action);
                    }
                    GameObject.Destroy(item.BaseObject);
                }
                if (actionList.Count > 0)
                {
                    var composite = new CompositeAction(actionList, "delete Selection");
                    EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, composite);
                }

                break;
        }


        _selectedGameObjects.Clear();
        RefreshGizmo();
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
        List<SelectableTargetData> targetsInRect = new();

        foreach (var pair in selectableGameObjectsInSceneDict)
        {
            GameObject targetobj = pair.Key;
            SelectableTargetData targetData = pair.Value;

            if (targetobj == null) continue;

            if (targetobj.layer != LayerMask.NameToLayer("Selectable")) continue;

            if (IsObjectInsideScreenRect(targetobj, screenRect))
            {
                targetsInRect.Add(targetData);
            }
        }

        ReplaceSelection(targetsInRect);
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
    public void ReplaceSelection(IEnumerable<GameObject> objectsToSelect)
    {
        ClearSelection();

        foreach (GameObject obj in objectsToSelect)
        {
            if (obj == null)
                continue;

            if (!selectableGameObjectsInSceneDict.TryGetValue(obj, out SelectableTargetData targetData))
                continue;

            AddToSelection(targetData);
        }

        Debug.Log("Selected objects" + _selectedGameObjects.Count);
        RefreshGizmo();
    }

    public void ReplaceSelection(IEnumerable<SelectableTargetData> targetsToSelect)
    {
        ClearSelection();

        foreach (SelectableTargetData targetData in targetsToSelect)
        {
            if (targetData == null)
                continue;

            AddToSelection(targetData);
        }

        RefreshGizmo();
    }

    private void RefreshGizmo()
    {
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

