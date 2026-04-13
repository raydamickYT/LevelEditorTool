using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// so if the canvas is set to screen space overlay, the selection box will be drawn properly on top of the screen.
/// however if you set it to screen space camera, the selection will be done properly. So I'll have to kind of make this into 2 scripts who do the same thing.
/// which is why the current version of the script has split up the code in to 2 classes with a base class so that the selection object works.
/// </summary>
public class SelectionBoxBase : MonoBehaviour
{
    public RectTransform rectTransform, parentRectTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void Start()
    {
        SetupEvents();

        if (rectTransform == null || parentRectTransform == null)
        {
            Debug.LogError($"No rectTransform found in the children or parent of {gameObject.name}. Please add one for the selection box to work.");
        }
    }
    void OnDestroy()
    {
        DeregisterEvents();
    }

    public virtual void SetupEvents()
    {
        //Delegates
        EventManager.Instance.AddDelegateListener(SelectionEvents.UpdateSelectionBox, (Action<Vector2, Vector2>)UpdateBox);
    }

    public virtual void DeregisterEvents() { }

    public virtual void Show()
    {
        rectTransform.gameObject.SetActive(true);
    }
    public virtual void Hide()
    {
        rectTransform.gameObject.SetActive(false);
    }

    public virtual void UpdateBox(Vector2 startScreenPosition, Vector2 currentScreenPosition)
    {
        throw new NotImplementedException();
    }

}
