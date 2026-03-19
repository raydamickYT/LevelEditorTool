using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Camera cam;
    public bool isPanning;
    private Vector3 lastMouseWorldPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = GetComponent<Camera>();

        EventManager.Instance.AddDelegateListener("OnMiddleMouseButton", (Action<InputAction.CallbackContext>)OnMoveCamera);
        EventManager.Instance.AddDelegateListener("OnScrollWheel", (Action<InputAction.CallbackContext>)OnCameraZoom);
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
        if (isPanning) return; //to prevent zooming while panning

        float scrollValue = context.ReadValue<Vector2>().y;
        cam.orthographicSize -= scrollValue * 0.5f;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, 2f, 20f);

        EventManager.Instance.TriggerUnityEvent("OnCameraZoom");
    }
}
