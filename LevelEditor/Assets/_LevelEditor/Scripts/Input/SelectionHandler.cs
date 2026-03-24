using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionHandler : MonoBehaviour
{
    public Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning($"No cam found on {gameObject.name} ");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InputHandler.Instance.OnLeftMouseButtonEvent += OnLeftMouseButtonEvent;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnLeftMouseButtonEvent(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleLeftClick();
        }
    }


    //TODO: dit moet verder afgemaakt worden als de andere classes zo ver zijn.
    //ik heb de selectrion controller een normale class gemaakt zodat de meeste logica daar uitgevoerd kan worden.
    void HandleLeftClick()
    {
        RaycastHit2D hit;
        if (RaycastHelper.TryGetPointerHit2D(cam, LayerMask.GetMask("Selectable"), out hit))
        {
            EventManager.Instance.TriggerDelegate("OnObjectSelected", hit.collider.gameObject);
            return;
        }
        else
        {
            EventManager.Instance.TriggerUnityEvent("OnObjectDeselected");
        }
    }

}
