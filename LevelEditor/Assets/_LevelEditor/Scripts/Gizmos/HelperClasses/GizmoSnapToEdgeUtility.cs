using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GizmoSnapToEdgeUtility
{
    static readonly List<Bounds> s_OtherBoundsScratch = new(128);
    static readonly HashSet<GameObject> s_ExcludeScratch = new();

    // Grid snap is always used when snapping is enabled. Hold Shift during a move drag for edge snap
    // (within threshold) on nearby objects; edge snap wins per axis when applied.
    public static Vector3 ResolveSnappedMoveWorldPosition(Vector3 rawWorldPosition, GizmoDragContext context, GizmoObject gizmoObject)
    {
        SnappingSettings s = context.SnappingSettings;
        if (s == null || !s.snappingEnabled)
            return rawWorldPosition;

        // If the user is dragging along a specific axis, we need to project the delta onto that axis
        GizmoAxis axisMode = context.ActiveHandle.Axis;
        Vector3 axisWorld = axisMode != GizmoAxis.All
            ? context.ActiveHandle.GetAxisVectorWorld().normalized
            : Vector3.zero;

        var exclude = new List<GameObject>();
        if (gizmoObject?.selectedLevelObjects != null)
            for (int i = 0; i < gizmoObject.selectedLevelObjects.Count; i++)
                if (gizmoObject.selectedLevelObjects[i] != null)
                    exclude.Add(gizmoObject.selectedLevelObjects[i].gameObject);

        return ResolveSnappedPlanePosition(
            rawWorldPosition,
            context.TargetStartPosition,
            context.SelectionBoundsWorldAtDragStart,
            context.HasSelectionBoundsWorld,
            s,
            axisMode,
            axisWorld,
            exclude);
    }

    public static Vector3 ResolveSnappedPlanePosition(
        Vector3 rawWorldPosition,
        Vector3 startPosition,
        Bounds selectionBoundsAtStart,
        bool hasSelectionBounds,
        SnappingSettings settings,
        GizmoAxis axisMode,
        Vector3 axisWorld,
        IReadOnlyList<GameObject> excludeObjects)
    {
        SnappingSettings s = settings;
        if (s == null || !s.snappingEnabled)
            return rawWorldPosition;

        //save the start position of the drag, so we can calculate the delta from that point
        Vector2 planeDelta = new Vector2(rawWorldPosition.x - startPosition.x, rawWorldPosition.y - startPosition.y);

        if (axisMode != GizmoAxis.All) // project the delta onto the axis
        {
            float alongRaw = Vector3.Dot(new Vector3(planeDelta.x, planeDelta.y, 0f), axisWorld);
            planeDelta = new Vector2(axisWorld.x * alongRaw, axisWorld.y * alongRaw);
        }

        bool usedEdgeX = false;
        bool usedEdgeY = false;
        Vector2 edgeCorrection = Vector2.zero;

        if (IsEdgeSnapModifierHeld()
            && hasSelectionBounds
            && s.edgeSnapThreshold > 0f
            && LevelObjectsRoot.Instance != null)
        {
            s_OtherBoundsScratch.Clear();
            s_ExcludeScratch.Clear();
            if (excludeObjects != null)
            {
                for (int i = 0; i < excludeObjects.Count; i++)
                {
                    GameObject go = excludeObjects[i];
                    if (go != null)
                        s_ExcludeScratch.Add(go);
                }
            }

            LevelObjectsRoot.Instance.AppendLevelColliderBounds(s_OtherBoundsScratch, s_ExcludeScratch);

            if (s_OtherBoundsScratch.Count > 0)
            {
                edgeCorrection = ComputeAxisAlignedEdgeCorrection(
                    planeDelta,
                    selectionBoundsAtStart,
                    s_OtherBoundsScratch,
                    s.edgeSnapThreshold,
                    s.moveSnapSize);

                if (Mathf.Abs(edgeCorrection.x) > 1e-5f)
                    usedEdgeX = true;
                if (Mathf.Abs(edgeCorrection.y) > 1e-5f)
                    usedEdgeY = true;
            }
        }

        Vector2 afterEdge = planeDelta + edgeCorrection;

        float grid = s.moveSnapSize;
        Vector3 result;

        if (axisMode == GizmoAxis.All)
        {
            Vector3 worldBeforeGrid = startPosition + new Vector3(afterEdge.x, afterEdge.y, 0f);
            worldBeforeGrid.z = startPosition.z;
            result = worldBeforeGrid;
            result.x = GizmoSnapToGridUtility.SnapFloat(result.x, grid, !usedEdgeX);
            result.y = GizmoSnapToGridUtility.SnapFloat(result.y, grid, !usedEdgeY);
        }
        else
        {
            float along = Vector3.Dot(new Vector3(afterEdge.x, afterEdge.y, 0f), axisWorld);
            bool skipGridAlong = Mathf.Abs(Vector3.Dot(new Vector3(edgeCorrection.x, edgeCorrection.y, 0f), axisWorld)) > 1e-5f;
            along = GizmoSnapToGridUtility.SnapFloat(along, grid, !skipGridAlong);
            result = startPosition + axisWorld * along;
            result.z = startPosition.z;
        }

        return result;
    }

    static Vector2 ComputeAxisAlignedEdgeCorrection(
        Vector2 translation,
        Bounds selectionAtDragStart,
        List<Bounds> others,
        float threshold,
        float proximityRange)
    {
        float minX = selectionAtDragStart.min.x + translation.x;
        float maxX = selectionAtDragStart.max.x + translation.x;
        float minY = selectionAtDragStart.min.y + translation.y;
        float maxY = selectionAtDragStart.max.y + translation.y;

        Bounds selectionBounds = new Bounds();
        selectionBounds.SetMinMax(
            new Vector3(minX, minY, selectionAtDragStart.min.z),
            new Vector3(maxX, maxY, selectionAtDragStart.max.z));

        float bestDx = 0f;
        float bestAbsDx = threshold + 1f;
        float bestDy = 0f;
        float bestAbsDy = threshold + 1f;

        for (int i = 0; i < others.Count; i++)
        {
            Bounds o = others[i];
            if (!IsWithinEdgeSnapProximity(selectionBounds, o, proximityRange))
                continue;

            float ominX = o.min.x;
            float omaxX = o.max.x;
            float ominY = o.min.y;
            float omaxY = o.max.y;

            ConsiderDx(omaxX - minX, ref bestDx, ref bestAbsDx, threshold);
            ConsiderDx(ominX - maxX, ref bestDx, ref bestAbsDx, threshold);
            ConsiderDx(ominX - minX, ref bestDx, ref bestAbsDx, threshold);
            ConsiderDx(omaxX - maxX, ref bestDx, ref bestAbsDx, threshold);

            ConsiderDy(omaxY - minY, ref bestDy, ref bestAbsDy, threshold);
            ConsiderDy(ominY - maxY, ref bestDy, ref bestAbsDy, threshold);
            ConsiderDy(ominY - minY, ref bestDy, ref bestAbsDy, threshold);
            ConsiderDy(omaxY - maxY, ref bestDy, ref bestAbsDy, threshold);
        }

        Vector2 total = Vector2.zero;
        if (bestAbsDx <= threshold)
            total.x = bestDx;
        if (bestAbsDy <= threshold)
            total.y = bestDy;

        return total;
    }

    /// <summary>
    /// True when <paramref name="other"/> is within one tile on both axes
    /// (touching, overlapping, or separated by at most <paramref name="range"/>).
    /// </summary>
    static bool IsWithinEdgeSnapProximity(Bounds selection, Bounds other, float range)
    {
        if (range <= 0f)
            return false;

        float gapX = GetAxisSeparation(selection.min.x, selection.max.x, other.min.x, other.max.x);
        float gapY = GetAxisSeparation(selection.min.y, selection.max.y, other.min.y, other.max.y);
        return gapX <= range && gapY <= range;
    }

    static float GetAxisSeparation(float minA, float maxA, float minB, float maxB)
    {
        if (maxA < minB)
            return minB - maxA;

        if (maxB < minA)
            return minA - maxB;

        return 0f;
    }

    static void ConsiderDx(float dx, ref float bestDx, ref float bestAbsDx, float threshold)
    {
        float ax = Mathf.Abs(dx);
        if (ax <= threshold && ax < bestAbsDx - 1e-6f)
        {
            bestAbsDx = ax;
            bestDx = dx;
        }
    }

    static void ConsiderDy(float dy, ref float bestDy, ref float bestAbsDy, float threshold)
    {
        float ay = Mathf.Abs(dy);
        if (ay <= threshold && ay < bestAbsDy - 1e-6f)
        {
            bestAbsDy = ay;
            bestDy = dy;
        }
    }

    static bool IsEdgeSnapModifierHeld()
    {
        return Keyboard.current != null
            && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
    }
}
