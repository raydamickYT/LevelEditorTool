using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class SelectionPickUtility
{
    public static List<GameObject> GetSelectableHitsOrderedFrontToBack(
        Camera camera,
        LayerMask layerMask,
        Func<GameObject, bool> isSelectableRegistered)
    {
        List<GameObject> orderedHits = new();

        if (camera == null || Mouse.current == null || isSelectableRegistered == null)
            return orderedHits;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = camera.ScreenToWorldPoint(screenPosition);

        Collider2D[] overlaps = Physics2D.OverlapPointAll(worldPosition, layerMask);
        if (overlaps == null || overlaps.Length == 0)
            return orderedHits;

        HashSet<GameObject> seen = new();
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D collider = overlaps[i];
            if (collider == null)
                continue;

            GameObject candidate = collider.gameObject;
            if (!seen.Add(candidate) || !isSelectableRegistered(candidate))
                continue;

            orderedHits.Add(candidate);
        }

        orderedHits.Sort(CompareFrontToBack);
        return orderedHits;
    }

    public static bool ListsMatchInOrder(IReadOnlyList<GameObject> a, IReadOnlyList<GameObject> b)
    {
        if (a == null || b == null)
            return false;

        if (a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    static int CompareFrontToBack(GameObject a, GameObject b)
    {
        GetRenderSortKey(a, out int layerA, out int orderA, out int siblingA);
        GetRenderSortKey(b, out int layerB, out int orderB, out int siblingB);

        int layerCompare = layerB.CompareTo(layerA);
        if (layerCompare != 0)
            return layerCompare;

        int orderCompare = orderB.CompareTo(orderA);
        if (orderCompare != 0)
            return orderCompare;

        return siblingB.CompareTo(siblingA);
    }

    static void GetRenderSortKey(GameObject gameObject, out int sortingLayerId, out int sortingOrder, out int siblingIndex)
    {
        sortingLayerId = 0;
        sortingOrder = int.MinValue;
        siblingIndex = gameObject != null ? gameObject.transform.GetSiblingIndex() : 0;

        if (gameObject == null)
        {
            sortingOrder = 0;
            return;
        }

        SpriteRenderer[] renderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (renderer.sortingLayerID > sortingLayerId
                || (renderer.sortingLayerID == sortingLayerId && renderer.sortingOrder > sortingOrder))
            {
                sortingLayerId = renderer.sortingLayerID;
                sortingOrder = renderer.sortingOrder;
            }
        }

        if (sortingOrder == int.MinValue)
            sortingOrder = 0;
    }
}
