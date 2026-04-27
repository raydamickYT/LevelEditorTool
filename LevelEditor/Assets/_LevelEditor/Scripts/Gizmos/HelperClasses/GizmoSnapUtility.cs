using UnityEngine;

public static class GizmoSnapUtility
{
    public static float SnapFloat(float value, float snapSize, bool snappingEnabled)
    {
        if (!snappingEnabled || snapSize <= 0f)
            return value;

        return Mathf.Round(value / snapSize) * snapSize;
    }

    public static Vector3 SnapVector(Vector3 value, float snapSize, bool snappingEnabled)
    {
        if (!snappingEnabled || snapSize <= 0f)
            return value;

        return new Vector3(
            SnapFloat(value.x, snapSize, snappingEnabled),
            SnapFloat(value.y, snapSize, snappingEnabled),
            SnapFloat(value.z, snapSize, snappingEnabled)
        );
    }
}
