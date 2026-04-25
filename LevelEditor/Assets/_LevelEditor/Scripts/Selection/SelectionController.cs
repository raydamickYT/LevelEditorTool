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
            TryClearSelectionWithUndo();
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

        //really long line but basically: get items form _selectedGameObjects which have the component LevelObject, aren't null then call the save() in that item and add that memento
        //to the list
        List<LevelObject.Memento> mementos = _selectedGameObjects.Select(item => item.BaseObject.GetComponent<LevelObject>()).Where(LevelObject => LevelObject != null)
        .Select(LevelObject => LevelObject.Save()).ToList();

        if (mementos.Count == 0) return;

        deleteAction = new DeleteAction(mementos);
        deleteAction.Execute();

        EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, deleteAction);

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
        List<int> beforeSelection = GetSelectedObjectIDs();
        List<int> afterSelection = new();

        foreach (var pair in selectableGameObjectsInSceneDict)
        {
            GameObject targetobj = pair.Key;

            if (targetobj == null) continue;

            if (targetobj.layer != LayerMask.NameToLayer("Selectable")) continue;

            if (IsObjectInsideScreenRect(targetobj, screenRect))
            {
                LevelObject lvlObj = targetobj.GetComponent<LevelObject>();

                if (lvlObj != null)
                    afterSelection.Add(lvlObj.ObjectID);
            }
        }

        if (beforeSelection.SequenceEqual(afterSelection)) return;

        var selectAction = new SelectAction(beforeSelection, afterSelection, "box Select");

        selectAction.Execute();
        EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, selectAction);
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

        LevelObject levelObject = selectedObject.GetComponent<LevelObject>();
        if (levelObject == null)
            return;

        List<int> beforeSelection = GetSelectedObjectIDs();
        List<int> afterSelection = new() { levelObject.ObjectID };

        if (beforeSelection.SequenceEqual(afterSelection))
            return;

        var selectAction = new SelectAction(beforeSelection, afterSelection, "Select Object");
        selectAction.Execute();

        EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, selectAction);
    }

    public List<int> GetSelectedObjectIDs()
    {
        List<int> ObjIDs = new();
        if (_selectedGameObjects.Count == 0)
            return ObjIDs;

        foreach (var item in _selectedGameObjects)
        {
            var lvlObj = item.BaseObject.GetComponent<LevelObject>();
            if (lvlObj != null)
                ObjIDs.Add(lvlObj.ObjectID);
        }
        return ObjIDs;
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
        ClearSelectionInternal();


        List<GameObject> objects = objectsToSelect?.ToList() ?? new List<GameObject>();


        if (objects.Count == 0)
        {
            RefreshGizmo();
            return;
        }
        foreach (GameObject obj in objects)
        {
            if (obj == null)
                continue;

            if (!selectableGameObjectsInSceneDict.TryGetValue(obj, out SelectableTargetData targetData))
                continue;

            AddToSelection(targetData);
        }

        // Debug.Log("Selected objects" + _selectedGameObjects.Count);
        RefreshGizmo();
    }

    public void ReplaceSelection(IEnumerable<SelectableTargetData> targetsToSelect)
    {
        ClearSelectionInternal();


        List<SelectableTargetData> targets = targetsToSelect?.ToList() ?? new List<SelectableTargetData>();


        if (targets.Count == 0)
        {
            RefreshGizmo();
            return;
        }

        foreach (SelectableTargetData targetData in targets)
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
    private void ClearSelectionInternal()
    {
        if (_selectedGameObjects.Count == 0) return;

        foreach (var item in _selectedGameObjects)
        {
            item.SelectableComponent?.OnDeselect();
        }

        _selectedGameObjects.Clear();
    }
    public void ClearSelection()
    {
        ClearSelectionInternal();
        RefreshGizmo();
    }

    public void TryClearSelectionWithUndo()
    {
        if (_selectedGameObjects.Count == 0)
            return;

        List<int> beforeSelection = GetSelectedObjectIDs();
        List<int> afterSelection = new();

        var selectAction = new SelectAction(beforeSelection, afterSelection, "Clear Selection");

        selectAction.Execute();

        EventManager.Instance.TriggerDelegate(
            ActionStackEvents.RegisterAction,
            selectAction
        );
    }
}

