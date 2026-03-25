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
    private SelectableTargetData currentSelection;

    public selectionController(Camera cam)
    {
        this.cam = cam;
    }

    //ik heb de selection controller een normale class gemaakt zodat de meeste logica daar uitgevoerd kan worden.
    public void HandleLeftClick()
    {
        RaycastHit2D hit;
        if (RaycastHelper.TryGetPointerHit2D(cam, LayerMask.GetMask("Selectable"), out hit))
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

    public void TrySelect(GameObject selectedObject)
    {
        if (!selectedGameObjectsDictionary.ContainsKey(selectedObject)) return;


        if (selectedGameObjectsDictionary.TryGetValue(selectedObject, out SelectableTargetData obj)) //note to self: if you notice you use this more often than once, try making a helper
        {
            if (currentSelection != obj)
            {
                if (currentSelection != null)
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
        currentSelection = targetData;
        // currentTargetList.Add(targetData);

        currentSelection.SelectableComponent.OnSelect();
        //future: add multi selection logic

        EventManager.Instance.TriggerDelegate("OnShowGizmo", targetData);

    }

    public void ClearSelection()
    {
        if (currentSelection == null) return;

        //future: add multi selection logic

        currentSelection.SelectableComponent.OnDeselect(); //single target
        EventManager.Instance.TriggerDelegate("OnHideGizmo", currentSelection);

        currentSelection = null;
        EventManager.Instance.TriggerUnityEvent("OnSelectionCleared");
        // currentTargetList.Clear();
    }
}

