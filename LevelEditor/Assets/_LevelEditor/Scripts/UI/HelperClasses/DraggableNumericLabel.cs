using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Turns a label into a Unity-style scrub handle for a <see cref="FloatField"/>:
/// dragging the label horizontally changes the field value (and fires its value-changed callbacks).
/// </summary>
public static class DraggableNumericLabel
{
    const string DragLabelClass = "draggable-numeric-label";

    public static void Enable(VisualElement dragHandle, FloatField field, float unitsPerPixel = 1f, bool roundToInt = true)
    {
        Enable(dragHandle, field, unitsPerPixel, roundToInt ? 0 : 2);
    }

    public static void Enable(VisualElement dragHandle, FloatField field, float unitsPerPixel, int decimalPlaces)
    {
        if (dragHandle == null || field == null)
            return;

        bool dragging = false;
        float startValue = 0f;
        Vector2 startPointer = Vector2.zero;

        dragHandle.AddToClassList(DragLabelClass);

        dragHandle.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0)
                return;

            dragging = true;
            startValue = field.value;
            startPointer = evt.position;
            dragHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        });

        dragHandle.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!dragging || !dragHandle.HasPointerCapture(evt.pointerId))
                return;

            float delta = ((Vector2)evt.position - startPointer).x * unitsPerPixel;
            float newValue = startValue + delta;
            if (decimalPlaces <= 0)
                newValue = Mathf.Round(newValue);
            else
                newValue = (float)System.Math.Round(newValue, decimalPlaces);

            field.value = newValue;
            evt.StopPropagation();
        });

        dragHandle.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!dragHandle.HasPointerCapture(evt.pointerId))
                return;

            dragging = false;
            dragHandle.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        });

        dragHandle.RegisterCallback<PointerCancelEvent>(evt =>
        {
            if (!dragHandle.HasPointerCapture(evt.pointerId))
                return;

            dragging = false;
            dragHandle.ReleasePointer(evt.pointerId);
        });
    }

    public static void Enable(VisualElement dragHandle, IntegerField field, float unitsPerPixel = 1f)
    {
        if (dragHandle == null || field == null)
            return;

        bool dragging = false;
        int startValue = 0;
        Vector2 startPointer = Vector2.zero;

        dragHandle.AddToClassList(DragLabelClass);

        dragHandle.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0)
                return;

            dragging = true;
            startValue = field.value;
            startPointer = evt.position;
            dragHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        });

        dragHandle.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!dragging || !dragHandle.HasPointerCapture(evt.pointerId))
                return;

            float delta = ((Vector2)evt.position - startPointer).x * unitsPerPixel;
            field.value = Mathf.RoundToInt(startValue + delta);
            evt.StopPropagation();
        });

        dragHandle.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!dragHandle.HasPointerCapture(evt.pointerId))
                return;

            dragging = false;
            dragHandle.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        });

        dragHandle.RegisterCallback<PointerCancelEvent>(evt =>
        {
            if (!dragHandle.HasPointerCapture(evt.pointerId))
                return;

            dragging = false;
            dragHandle.ReleasePointer(evt.pointerId);
        });
    }
}
