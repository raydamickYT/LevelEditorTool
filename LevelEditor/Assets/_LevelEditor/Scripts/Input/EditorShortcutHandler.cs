using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditorShortcutHandler : MonoBehaviour
{
    [SerializeField] private List<ShortCutBinding> shortcutBindings = new();
    private Dictionary<EditorCommand, EditorCommand> commandMap;
    public static event Action<EditorCommand> OnCommandTriggered;

    void Awake()
    {
        commandMap = new Dictionary<EditorCommand, EditorCommand>();

        foreach (var binding in shortcutBindings)
        {
            if (!commandMap.ContainsKey(binding.command))
                commandMap.Add(binding.command, binding.command);
        }
    }
    void Start()
    {
        if (InputHandler.Instance == null)
        {
            Debug.LogWarning("Input handler not found");
            return;
        }
        InputHandler.Instance.OnWEvent += EnableMoveGizmo;
        InputHandler.Instance.OnEEvent += EnableRotateGizmo;
        InputHandler.Instance.OnREvent += EnableScaleGizmo;
        InputHandler.Instance.OnDeleteEvent += DeleteSelectedObjects;
        InputHandler.Instance.OnZEvent += ZAction;
        InputHandler.Instance.OnCtrlEvent += CTRLAction;

        InputHandler.Instance.TriggerCMD += HandleInputAction;
    }

    void OnDisable()
    {
        if (InputHandler.Instance == null)
            return;

        InputHandler.Instance.OnWEvent -= EnableMoveGizmo;
        InputHandler.Instance.OnEEvent -= EnableRotateGizmo;
        InputHandler.Instance.OnREvent -= EnableScaleGizmo;
        InputHandler.Instance.OnDeleteEvent -= DeleteSelectedObjects;
        InputHandler.Instance.OnZEvent -= ZAction;
        InputHandler.Instance.OnCtrlEvent -= CTRLAction;

        InputHandler.Instance.TriggerCMD -= HandleInputAction;
    }

    public void HandleInputAction(EditorCommand inputActionName)
    {
        if (commandMap.TryGetValue(inputActionName, out var command))
        {
            EventManager.Instance.TriggerDelegate(ShortcutBindingEvents.OnCommandTriggered, command); // has to be of type EditorCommand
        }
    }

    public void EnableMoveGizmo(InputAction.CallbackContext context)
    {
        if (context.started)
            EventManager.Instance.TriggerDelegate(GimzmoEvents.OnGizmoTypeChanged, GizmoType.move);
    }

    public void EnableRotateGizmo(InputAction.CallbackContext context)
    {
        if (context.started)
            EventManager.Instance.TriggerDelegate(GimzmoEvents.OnGizmoTypeChanged, GizmoType.rotate);
    }

    public void EnableScaleGizmo(InputAction.CallbackContext context)
    {
        if (context.started)
            EventManager.Instance.TriggerDelegate(GimzmoEvents.OnGizmoTypeChanged, GizmoType.scale);
    }

    public void DeleteSelectedObjects(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            EventManager.Instance.TriggerUnityEvent(SelectionEvents.OnDeleteSelected);
        }
    }

    public void ZAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
            {
                EventManager.Instance.TriggerDelegate(ActionStackEvents.Undo);
            }
        }
    }
    public void CTRLAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
            {
                EventManager.Instance.TriggerDelegate(ActionStackEvents.Undo);
            }
        }
    }

    void OnUndoShortcut()
    {

    }

    void OnRedoShortcut()
    {

    }
}

public enum EditorCommand
{
    Undo,
    Redo,
    Delete,
    SwitchMoveTool,
    SwitchRotateTool,
    SwitchScaleTool
}

[Serializable]
public class ShortCutBinding
{
    public EditorCommand command;
}

public static class ShortcutBindingEvents
{
    public const string OnCommandTriggered = "EditorCommandTriggered";
}