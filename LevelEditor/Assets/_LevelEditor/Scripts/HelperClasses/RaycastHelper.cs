using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class RaycastHelper
{
    public static bool IsPointerOverLayer(LayerMask layerMask)
    {
        if (EventSystem.current == null || Mouse.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            int hitLayer = result.gameObject.layer;

            if (((1 << hitLayer) & layerMask.value) != 0)
                return true;
        }

        return false;
    }
}
