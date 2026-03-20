using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class UIHelper
{
    /// <summary>
    /// This function already exists in eventsystem.current.ispointerovergameobject(). However this helper function will make debugging in the future easier.
    /// It'll also give me more controll over what layers I'm checking and where.
    /// </summary>
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Mouse.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count > 0;
    }
}
