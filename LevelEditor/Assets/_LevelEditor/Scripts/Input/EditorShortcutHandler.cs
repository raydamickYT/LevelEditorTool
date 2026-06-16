using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class EditorShortcutHandler : MonoBehaviour
{
    [SerializeField] private List<ShortCutBinding> shortcutBindings = new();
    private Dictionary<EditorCommand, EditorCommand> commandMap;
    private Coroutine subscribeRoutine;
    readonly List<UIDocument> registeredUiDocuments = new();

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
        RegisterUiTextInputSuppression();
        subscribeRoutine = StartCoroutine(waitForInputHandler());
    }

    void OnDisable()
    {
        UnregisterUiTextInputSuppression();

        if (InputHandler.Instance != null)
            InputHandler.Instance.TriggerCMD -= HandleInputAction;

        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }
    }

    void RegisterUiTextInputSuppression()
    {
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (UIDocument document in documents)
        {
            if (document == null || document.rootVisualElement == null)
                continue;

            document.rootVisualElement.RegisterCallback<KeyDownEvent>(OnUiKeyDown, TrickleDown.TrickleDown);
            registeredUiDocuments.Add(document);
        }
    }

    void UnregisterUiTextInputSuppression()
    {
        foreach (UIDocument document in registeredUiDocuments)
        {
            if (document == null || document.rootVisualElement == null)
                continue;

            document.rootVisualElement.UnregisterCallback<KeyDownEvent>(OnUiKeyDown, TrickleDown.TrickleDown);
        }

        registeredUiDocuments.Clear();
    }

    static void OnUiKeyDown(KeyDownEvent evt)
    {
        if (!EditorShortcutInputGate.ShouldSuppressTextInput(evt))
            return;

        evt.StopPropagation();
        evt.PreventDefault();
        EditorShortcutInputGate.ClearTextInputSuppression();
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
    ToggleSelect,
    SaveFile,
    SaveFileAs,
    OpenFile,
    NewFile,
    ImportAssets,
    ToggleSnapping
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