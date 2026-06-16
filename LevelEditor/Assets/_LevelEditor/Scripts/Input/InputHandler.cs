using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour, InputSystem_Actions.IUIActions, InputSystem_Actions.IUI_EditorActions

{
    public static InputHandler Instance;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        inputActions = new InputSystem_Actions();

        //use diffent bindings for the editor and the build.
#if UNITY_EDITOR
        inputActions.UI.Disable();
        inputActions.UI_Editor.Enable();
        inputActions.UI_Editor.SetCallbacks(this);
#else
        inputActions.UI_Editor.Disable();
        inputActions.UI.Enable();
        inputActions.UI.SetCallbacks(this);
#endif

        inputActions.UI.Enable();
    }
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        EventManager.Instance.RemoveAllListeners();
    }

    public event Action<EditorCommand> TriggerCMD;
    void triggerCommand(EditorCommand editorCommand)
    {
        if (EditorShortcutInputGate.AreShortcutsBlocked)
            return;

        TriggerCMD?.Invoke(editorCommand);
    }

    public event Action<SelectionCommand, InputAction.CallbackContext> TriggerSelectionCommand;
    void triggerSelection(InputAction.CallbackContext context)
    {
        if (EditorShortcutInputGate.AreShortcutsBlocked)
            return;

        SelectionCommand command =
    Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed
        ? SelectionCommand.ToggleSelect
        : SelectionCommand.Select;
        TriggerSelectionCommand?.Invoke(command, context);
    }

    public event Action<InputAction.CallbackContext> OnCancelEvent;
    public void OnCancel(InputAction.CallbackContext context)
        => OnCancelEvent?.Invoke(context);

    public void OnClick(InputAction.CallbackContext context)
    {
        triggerSelection(context);
    }

    public event Action<InputAction.CallbackContext> onMiddleMouseButtonEvent;
    public void OnMiddleClick(InputAction.CallbackContext context)
        => onMiddleMouseButtonEvent?.Invoke(context);

    public event Action<InputAction.CallbackContext> onRightMouseButtonEvent;
    public void OnNavigate(InputAction.CallbackContext context)
        => onRightMouseButtonEvent?.Invoke(context);

    public event Action<InputAction.CallbackContext> onPointEvent;
    public void OnPoint(InputAction.CallbackContext context)
        => onPointEvent?.Invoke(context);

    public event Action<InputAction.CallbackContext> onRightClickEvent;
    public void OnRightClick(InputAction.CallbackContext context)
        => onRightClickEvent?.Invoke(context);

    public event Action<InputAction.CallbackContext> onScrollWheelEvent;
    public void OnScrollWheel(InputAction.CallbackContext context)
        => onScrollWheelEvent?.Invoke(context);

    public event Action<InputAction.CallbackContext> onSubmitEvent;
    public void OnSubmit(InputAction.CallbackContext context)
        => onSubmitEvent?.Invoke(context);

    public void OnW(InputAction.CallbackContext context)
    {
        if (context.started)
            triggerCommand(EditorCommand.SwitchMoveTool);
    }

    public void OnE(InputAction.CallbackContext context)
    {
        if (context.started)
            triggerCommand(EditorCommand.SwitchRotateTool);
    }
    public void OnR(InputAction.CallbackContext context)
    {
        if (context.started)
            triggerCommand(EditorCommand.SwitchScaleTool);
    }

    public void OnDelete(InputAction.CallbackContext context)
    => triggerCommand(EditorCommand.Delete);
    public event Action<InputAction.CallbackContext> OnCtrlEvent;
    public void OnCtrl(InputAction.CallbackContext context)
    => OnCtrlEvent?.Invoke(context);
    public event Action<InputAction.CallbackContext> OnZEvent;
    public void OnZ(InputAction.CallbackContext context)
    => OnZEvent?.Invoke(context);

    public void OnUndo(InputAction.CallbackContext context)
    {
        if (context.started)
            triggerCommand(EditorCommand.Undo);
    }

    public void OnRedo(InputAction.CallbackContext context)
    {
        if (context.started)
            triggerCommand(EditorCommand.Redo);
    }

    public void OnCopy(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            triggerCommand(EditorCommand.Copy);
        }
    }

    public void OnPaste(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            triggerCommand(EditorCommand.Paste);
        }
    }

    public void OnDuplicate(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            triggerCommand(EditorCommand.Duplicate);
        }
    }

    public void OnCut(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            triggerCommand(EditorCommand.Cut);
        }
    }

    public void OnSave(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            triggerCommand(EditorCommand.SaveFile);
        }
    }

    public void OnImportAssets(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            triggerCommand(EditorCommand.ImportAssets);
        }
    }

    public void OnNewProject(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            triggerCommand(EditorCommand.NewFile);
        }
    }

    public void OnOpenProject(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            triggerCommand(EditorCommand.OpenFile);
        }
    }

    public void OnSnappingShortcut(InputAction.CallbackContext context)
    {
        if (!context.started || HasBlockingModifierHeld())
            return;

        triggerCommand(EditorCommand.ToggleSnapping);
    }

    static bool HasBlockingModifierHeld()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.shiftKey.isPressed
            || Keyboard.current.ctrlKey.isPressed
            || Keyboard.current.altKey.isPressed;
    }
}
