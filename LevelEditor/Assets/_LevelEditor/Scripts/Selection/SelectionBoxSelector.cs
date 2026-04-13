using System;
using UnityEngine;

public class SelectionBoxSelector : SelectionBoxBase
{
    private Vector2 _lastStartScreenPosition;
    private Vector2 _lastCurrentScreenPosition;
    public override void SetupEvents()
    {
        base.SetupEvents();
        //func
        SelectionEvents.FinalizeSelectionRect += FinalizeSelectionBox;
    }

    public override void DeregisterEvents()
    {
        Debug.Log("deregistering selection box events");
        SelectionEvents.FinalizeSelectionRect -= FinalizeSelectionBox;
    }

    public override void UpdateBox(Vector2 startScreenPosition, Vector2 currentScreenPosition)
    {
        _lastStartScreenPosition = startScreenPosition;
        _lastCurrentScreenPosition = currentScreenPosition;
    }

    public Rect FinalizeSelectionBox()
    {
        Vector2 min = Vector2.Min(_lastStartScreenPosition, _lastCurrentScreenPosition);
        Vector2 max = Vector2.Max(_lastStartScreenPosition, _lastCurrentScreenPosition);

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
