using System;
using System.Collections.Generic;
using UnityEngine;

public class GizmoHandler : MonoBehaviour
{
    [SerializeField] private GizmoController gizmoController;
    public GizmoObject GizmoObject;

    private void Awake()
    {
        EventManager.Instance.AddDelegateListener(SelectionEvents.OnSelectionChanged, (Action<HashSet<SelectableTargetData>>)HandleGizmoChange);

        EventManager.Instance.AddDelegateListener(ShortcutBindingEvents.OnCommandTriggered, (Action<EditorCommand>)HandleGizmoCommand);

        if (GizmoObject == null || !GizmoObject.GetComponent<GizmoObject>())
        {
            Debug.LogWarning("Gizmo object is not assigned properly in: " + name);
            return;
        }

        gizmoController = new GizmoController(GizmoObject);
    }
    private void HandleGizmoCommand(EditorCommand editorCommand)
    {
        switch (editorCommand)
        {
            case EditorCommand.SwitchMoveTool:
                HandleGizmoTypeChanged(GizmoType.move);
                break;
            case EditorCommand.SwitchRotateTool:
                HandleGizmoTypeChanged(GizmoType.rotate);
                break;
            case EditorCommand.SwitchScaleTool:
                HandleGizmoTypeChanged(GizmoType.scale);
                break;
        }
    }

    private void HandleGizmoChange(HashSet<SelectableTargetData> data)
     => gizmoController.HandleSelectionChanged(data);
    private void HandleGizmoTypeChanged(GizmoType type)
        => gizmoController.SetGizmoType(type);
}

public static class GimzmoEvents
{
    public const string OnGizmoTypeChanged = "OnGizmoTypeChanged";
}
