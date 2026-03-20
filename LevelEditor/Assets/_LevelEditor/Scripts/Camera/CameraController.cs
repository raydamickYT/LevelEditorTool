using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Camera cam;
    public bool isPanning;
    private Vector3 lastMouseWorldPos;
    [SerializeField] private LayerMask targetLayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = GetComponent<Camera>();
        InputHandler.Instance.onMiddleMouseButtonEvent += OnMoveCamera;
        InputHandler.Instance.onScrollWheelEvent += OnCameraZoom;

    }
    void Update()
    {
        if (Mouse.current == null) return;

        if (!isPanning) return;

        Vector3 currentMouseWorldPos = GetMouseWorldPosition();
        Vector3 delta = lastMouseWorldPos - currentMouseWorldPos;

        transform.position += delta;

        lastMouseWorldPos = GetMouseWorldPosition();
    }
    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, cam.nearClipPlane));
        worldPos.z = 0f;
        return worldPos;
    }
    private void OnMoveCamera(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isPanning = true;
            lastMouseWorldPos = GetMouseWorldPosition();
        }
        if (context.canceled) isPanning = false;
    }

    private void OnCameraZoom(InputAction.CallbackContext context)
    {
        if(!context.performed) return; //to prevent zooming when scroll wheel is not used
        if (isPanning) return; //to prevent zooming while panning
        if(UIHelper.IsPointerOverUI()) return; //to prevent zooming when pointer is over UI

        float scrollValue = context.ReadValue<Vector2>().y;
        cam.orthographicSize -= scrollValue * 0.5f;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, 2f, 20f);

        EventManager.Instance.TriggerUnityEvent("OnCameraZoom");
    }
}
