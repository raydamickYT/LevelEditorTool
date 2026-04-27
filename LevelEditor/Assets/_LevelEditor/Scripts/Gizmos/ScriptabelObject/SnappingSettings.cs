using UnityEngine;

[CreateAssetMenu(fileName = "SnappingSettings", menuName = "Scriptable Objects/SnappingSettings")]
public class SnappingSettings : ScriptableObject
{
    [Header("Snapping")]
    public bool snappingEnabled = false;
    public float moveSnapSize = 1f;
    public float rotateSnapAngle = 15f;
    public float scaleSnapSize = 0.25f;
}
