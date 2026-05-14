using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditorShortcutHandler : MonoBehaviour
{
    [SerializeField] private List<ShortCutBinding> shortcutBindings = new();
    private Dictionary<EditorCommand, EditorCommand> commandMap;
    private Coroutine subscribeRoutine;


    void Awake()
    {
        commandMap = new Dictionary<EditorCommand, EditorCommand>();

        // All commands must reach listeners (Copy/Cut/Paste/Delete/…). The inspector list is optional extras, not a whitelist.
        foreach (EditorCommand cmd in Enum.GetValues(typeof(EditorCommand)))
            commandMap[cmd] = cmd;

        foreach (var binding in shortcutBindings)
        {
            if (binding == null)
                continue;

            commandMap[binding.command] = binding.command;
        }
    }

    void OnEnable()
    {
        subscribeRoutine = StartCoroutine(waitForInputHandler());
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

    private IEnumerator waitForInputHandler()
    {
        yield return new WaitUntil(() => InputHandler.Instance != null);
        InputHandler.Instance.TriggerCMD += HandleInputAction;
    }

}

public enum EditorCommand
{
    Undo,
    Redo,
    Delete,
    SwitchMoveTool,
    SwitchRotateTool,
    SwitchScaleTool,
    Copy,
    Paste,
    Duplicate,
    Cut,
    ToggleSelect
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