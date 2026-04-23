using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour, InputSystem_Actions.IUIActions
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
        inputActions.UI.SetCallbacks(this);
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
        TriggerCMD?.Invoke(editorCommand);
    }

    public event Action<InputAction.CallbackContext> OnCancelEvent;
    public void OnCancel(InputAction.CallbackContext context)
        => OnCancelEvent?.Invoke(context);

    public event Action<InputAction.CallbackContext> OnLeftMouseButtonEvent;
    public void OnClick(InputAction.CallbackContext context)
    {
        OnLeftMouseButtonEvent?.Invoke(context);
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
}
