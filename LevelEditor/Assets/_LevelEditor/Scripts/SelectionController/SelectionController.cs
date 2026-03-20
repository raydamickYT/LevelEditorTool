using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SelectionController : MonoBehaviour
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
            if(RaycastHelper.TryGetPointerHit2D(cam, LayerMask.GetMask("Selectable"), out RaycastHit2D hit))
            {
                hit.transform.gameObject.GetComponent<ISelectable>()?.OnSelect();
            }
        }
    }
}
