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

    public event Action<InputAction.CallbackContext> OnWEvent;
    public void OnW(InputAction.CallbackContext context)
        => OnWEvent?.Invoke(context);

    public event Action<InputAction.CallbackContext> OnEEvent;
    public void OnE(InputAction.CallbackContext context)
        => OnEEvent?.Invoke(context);
    public event Action<InputAction.CallbackContext> OnREvent;
    public void OnR(InputAction.CallbackContext context)
        => OnREvent?.Invoke(context);
    
    public event Action<InputAction.CallbackContext> OnDeleteEvent;
    public void OnDelete(InputAction.CallbackContext context)
    => OnDeleteEvent?.Invoke(context);  
}
