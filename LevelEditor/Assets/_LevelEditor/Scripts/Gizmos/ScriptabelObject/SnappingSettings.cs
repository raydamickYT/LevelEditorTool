using UnityEngine;

[CreateAssetMenu(fileName = "SnappingSettings", menuName = "Scriptable Objects/SnappingSettings")]
public class SnappingSettings : ScriptableObject
{
    [Header("Snapping")]
    public bool snappingEnabled = false;
    public float moveSnapSize = 1f;
    public float rotateSnapAngle = 15f;
    public float scaleSnapSize = 0.25f;

    [Header("Move — edge snap")]
    [Tooltip("Max world distance (per axis) to pull selection bounds to align with another object's collider edges.")]
    public float edgeSnapThreshold = 0.35f;
}
