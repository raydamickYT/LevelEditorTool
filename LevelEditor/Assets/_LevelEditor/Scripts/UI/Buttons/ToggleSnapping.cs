using UnityEngine;

/// <summary>
/// Snapping toggle logic for UI Toolkit (<see cref="EditorTopBarController"/>).
/// </summary>
public static class SnappingToggleService
{
    public static void ApplySnappingEnabled(bool enabled)
    {
        EventManager.Instance?.TriggerDelegate(SnappingEvent.OnToggleSnapping, enabled);
    }
}

public static class SnappingEvent
{
    public const string OnToggleSnapping = "OnToggleSnapping";
}

/// <summary>
/// Legacy scene component on the old uGUI Snapping toggle. Logic moved to TopBar UI Toolkit.
/// Kept so existing scene references do not break Unity's script loader.
/// </summary>
public class ToggleSnapping : MonoBehaviour
{
    void Awake()
    {
        enabled = false;
    }
}
