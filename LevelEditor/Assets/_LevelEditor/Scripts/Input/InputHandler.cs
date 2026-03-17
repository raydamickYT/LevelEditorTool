using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour, InputSystem_Actions.IPlayerActions, InputSystem_Actions.IUIActions
{
    public static InputHandler Instance;

    private InputCommands inputCommands;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        inputCommands = new InputCommands();
    }
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    public void OnCancel(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnNavigate(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }
}
