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
        InputHandler.Instance.TriggerCMD += HandleInputAction;
    }

    void OnDisable()
    {
        if (InputHandler.Instance == null)
            return;
        InputHandler.Instance.TriggerCMD -= HandleInputAction;
    }

    public void HandleInputAction(EditorCommand inputActionName)
    {
        if (commandMap.TryGetValue(inputActionName, out var command))
        {
            EventManager.Instance.TriggerDelegate(ShortcutBindingEvents.OnCommandTriggered, command); // has to be of type EditorCommand
        }
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