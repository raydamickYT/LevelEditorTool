using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;


public class GizmoInteractionHandler : MonoBehaviour
{
    //snapping
    [Header("Snapping")]
    [SerializeField] private SnappingSettings snappingSettings;
    public bool SnappingEnabled => snappingSettings.snappingEnabled;

    [Header("Camer & Gizmo LayerMaskName")]
    [SerializeField] private Camera cam;
    [SerializeField] private string gizmoHandleLayerName = "GizmoHandle";

    private Coroutine subscribeRoutine;

    private GizmoHandle activeHandle;
    private Transform activeTarget;
    private Vector3 dragStartWorld;
    private Vector3 targetStartPosition;
    private Vector3 targetStartScale;
    private float targetStartRotationZ;
    private float startMouseAngle;
    private bool isDragging;

    //undo stack
    private TransformAction currentAction;
    private List<TransformAction> transformActions = new();

    //operations
    GizmoMoveOperation gizmoMoveOperation = new();
    GizmoRotationOperation gizmoRotationOperation = new();
    GizmoScaleOperation gizmoScaleOperation = new();

    void Awake()
    {
        EventManager.Instance.AddDelegateListener(SnappingEvent.OnToggleSnapping, (Action<bool>)(isEnabled => snappingSettings.snappingEnabled = isEnabled));
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning($"No cam found on {gameObject.name} ");
            }
        }
    }

    private void OnEnable()
    {
        subscribeRoutine = StartCoroutine(WaitForInputHandler());
    }

    private void OnDisable()
    {
        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        if (InputHandler.Instance != null)
        {
            InputHandler.Instance.OnLeftMouseButtonEvent -= OnLeftMouseButton;
            InputHandler.Instance.onPointEvent -= OnPoint;
        }

        StopDragging();
    }

    private IEnumerator WaitForInputHandler()
    {
        yield return new WaitUntil(() => InputHandler.Instance != null);

        InputHandler.Instance.OnLeftMouseButtonEvent += OnLeftMouseButton;
        InputHandler.Instance.onPointEvent += OnPoint;
    }

    private void OnLeftMouseButton(InputAction.CallbackContext context)
    {
        if (cam == null)
            return;

        if (context.started)
        {
            TryBeginDrag();
        }
        else if (context.canceled)
        {
            StopDragging();
        }
    }

    private void OnPoint(InputAction.CallbackContext context)
    {
        if (!isDragging || activeHandle == null || activeTarget == null)
            return;

        UpdateDrag();
    }

    private void TryBeginDrag()
    {
        if (!RaycastHelper.TryGetHandleUnderPointer(out GizmoHandle handle, cam, gizmoHandleLayerName))
            return;

        if (handle.Owner.selectableObject == null)
        {
            Debug.Log("Handle's owner has no selectableObject: " + handle.name);
            return;
        }

        if (handle.Owner == null || handle.Owner.selectableObject == null || !handle.Owner.selectableObject.IsSelected || handle.Owner.TargetTransform == null)
        {
            Debug.Log("Handle under pointer is not valid for dragging.");
            return;
        }

        activeHandle = handle;
        activeTarget = handle.Owner.TargetTransform;
        var gizmoObject = handle.Owner;

        dragStartWorld = GetMouseWorldPosition();
        targetStartPosition = activeTarget.position;
        targetStartScale = activeTarget.localScale;
        targetStartRotationZ = activeTarget.eulerAngles.z;
        startMouseAngle = GetMouseAngleToTarget(activeTarget.position);

        // Debug.Log("logging action" + gizmoObject.dragLevelObjects.Count);
        if (gizmoObject.selectedDragLevelObjects.Count == 1)
        {
            currentAction = new TransformAction(gizmoObject.selectedDragLevelObjects[0]);
        }
        else
        {
            foreach (var levelObj in gizmoObject.selectedDragLevelObjects)
            {
                var t = new TransformAction(levelObj);
                transformActions.Add(t);
            }
        }

        isDragging = true;
    }

    private void StopDragging()
    {
        if (!isDragging) return;

        if (activeHandle is GizmoScaleHandle scaleHandle)
            scaleHandle.ResetScaleVisual();

        if (currentAction != null)
        {
            currentAction?.CaptureAfterState();
            if (currentAction.HasChanged())
                EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, currentAction);
        }
        else if (transformActions.Count > 1)
        {
            foreach (TransformAction t in transformActions)
            {
                t.CaptureAfterState();
            }

            if (transformActions[0].HasChanged())
            {
                var compositeAction = new CompositeAction(transformActions, "TransformActions");
                EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, compositeAction);
            }

        }

        currentAction = null;
        transformActions.Clear();

        isDragging = false;
        activeHandle = null;
        activeTarget = null;

    }
    private void UpdateDrag()
    {
        Vector3 currentMouseWorld = GetMouseWorldPosition();
        GizmoDragContext context = CreateDragContext();

        switch (activeHandle.Mode)
        {
            case GizmoHandleMode.Move:
                // ApplyMove(currentMouseWorld);
                gizmoMoveOperation.Apply(context, currentMouseWorld);
                break;

            case GizmoHandleMode.Rotate:
                gizmoRotationOperation.Apply(context, currentMouseWorld);
                break;

            case GizmoHandleMode.Scale:
                // ApplyScale(currentMouseWorld);
                gizmoScaleOperation.Apply(context, currentMouseWorld);
                break;
        }
    }

    GizmoDragContext CreateDragContext()
    {
        GizmoDragContext dragCtx = new GizmoDragContext(activeHandle, activeTarget, dragStartWorld,
        targetStartPosition, targetStartScale, targetStartRotationZ, startMouseAngle, snappingSettings);
        return dragCtx;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(mouseScreen);
        world.z = 0f;
        return world;
    }

    private float GetMouseAngleToTarget(Vector3 targetPosition)
    {
        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector2 dir = mouseWorld - targetPosition;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }
}
