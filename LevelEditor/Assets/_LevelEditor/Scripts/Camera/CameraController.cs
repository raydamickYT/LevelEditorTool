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
}
