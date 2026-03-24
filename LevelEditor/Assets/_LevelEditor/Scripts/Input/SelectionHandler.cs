using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionHandler : MonoBehaviour
{
    public Camera cam;
    private selectionController selectionController;

    void Awake()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning($"No cam found on {gameObject.name} ");
        }

        selectionController = new selectionController(cam);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InputHandler.Instance.OnLeftMouseButtonEvent += OnLeftMouseButtonEvent;
    }

    void OnLeftMouseButtonEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            selectionController.HandleLeftClick();
        }
    }
}

public class selectionController
{
    public Camera cam;

    public selectionController(Camera cam)
    {
        this.cam = cam;
    }

    //ik heb de selection controller een normale class gemaakt zodat de meeste logica daar uitgevoerd kan worden.
    public void HandleLeftClick()
    {
        RaycastHit2D hit;
        if (RaycastHelper.TryGetPointerHit2D(cam, LayerMask.GetMask("Selectable"), out hit))
        {
            EventManager.Instance.TriggerDelegate("OnObjectSelected", hit.collider.gameObject);
            return;
        }
        else if (!UIHelper.IsPointerOverUI())
        {
            EventManager.Instance.TriggerUnityEvent("OnObjectDeselected");
        }
    }
}
