using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class RaycastHelper
{
    public static bool TryGetPointerHit2D(Camera camera, LayerMask layerMask, out RaycastHit2D hitInfo, float maxDistance = Mathf.Infinity)
    {
        hitInfo = default;

        if (camera == null || Mouse.current == null)
            return false;


        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = camera.ScreenToWorldPoint(mousePosition);

        hitInfo = Physics2D.Raycast(worldPosition, Vector2.zero, maxDistance, layerMask);
        return hitInfo.collider != null;
    }
}
