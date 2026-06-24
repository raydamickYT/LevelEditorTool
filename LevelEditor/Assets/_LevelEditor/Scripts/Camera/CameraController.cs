
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Camera cam;
    public bool isPanning;
    private Vector3 lastMouseWorldPos;
    [SerializeField] private LayerMask targetLayer;

    [Header("Zoom (orthographic)")]
    [Tooltip("Kleinste orthographicSize = sterkst ingezoomd. Grootste = verst uitgezoomd.")]
    [SerializeField] private float minOrthographicSize = 2f;
    [SerializeField] private float maxOrthographicSize = 20f;

    [Header("Focus selection")]
    [SerializeField] float focusFramePadding = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = GetComponent<Camera>();
        InputHandler.Instance.onMiddleMouseButtonEvent += OnMoveCamera;
        InputHandler.Instance.onScrollWheelEvent += OnCameraZoom;
        EventManager.Instance.AddDelegateListener(ShortcutBindingEvents.OnCommandTriggered, (Action<EditorCommand>)OnFocusObject);

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

    private void OnFocusObject(EditorCommand command)
    {
        if (command != EditorCommand.FocusObject)
            return;

        FocusOnObject();
    }

    private void FocusOnObject()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (!EditorBlackBoard.HasSelection)
            return;

        if (!GizmoInteractionHandler.TryGetCombinedColliderBoundsWorld(
                EditorBlackBoard.GetSelectedLevelObjectsList(),
                out Bounds selectionBounds))
            return;

        Vector3 center = selectionBounds.center;
        center.z = transform.position.z;
        transform.position = center;

        float halfHeight = Mathf.Max(selectionBounds.extents.y, 0.01f) + focusFramePadding;
        float halfWidth = Mathf.Max(selectionBounds.extents.x, 0.01f) + focusFramePadding;
        cam.orthographicSize = Mathf.Clamp(
            Mathf.Max(halfHeight, halfWidth / cam.aspect),
            minOrthographicSize,
            maxOrthographicSize);

        EventManager.Instance.TriggerUnityEvent(CameraEvents.OnCameraZoom);
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

        Vector3 worldBeforeZoom = GetMouseWorldPosition();

        float newSize = cam.orthographicSize - scrollValue * 0.5f;
        newSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);

        if (Mathf.Approximately(newSize, cam.orthographicSize)) return;

        cam.orthographicSize = newSize;

        Vector3 worldAfterZoom = GetMouseWorldPosition();

        transform.position += worldBeforeZoom - worldAfterZoom; // apply mouse position offset to zoom in on the mouse position

        EventManager.Instance.TriggerUnityEvent(CameraEvents.OnCameraZoom);
    }
}

public static class CameraEvents
{
    public const string OnCameraZoom = "OnCameraZoom";
}
