using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class GizmoInteractionHandler : MonoBehaviour
{
    public static GizmoInteractionHandler Instance { get; private set; }

    //snapping
    [Header("Snapping")]
    [SerializeField] private SnappingSettings snappingSettings;
    public bool SnappingEnabled => snappingSettings.snappingEnabled;

    [Header("Camera & Gizmo LayerMaskName")]
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
    private GizmoObject gizmoObject;

    private Bounds selectionBoundsWorldAtDragStart;
    private bool hasSelectionBoundsWorldAtDragStart;

    //undo stack
    private TransformAction currentAction;
    private List<TransformAction> transformActions = new();

    //operations
    GizmoMoveOperation gizmoMoveOperation = new();
    GizmoRotationOperation gizmoRotationOperation = new();
    GizmoScaleOperation gizmoScaleOperation = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EventManager.Instance.AddDelegateListener(SnappingEvent.OnToggleSnapping, (Action<bool>)(isEnabled => snappingSettings.snappingEnabled = isEnabled));
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Vector3 SnapSpawnWorldPosition(
        Vector3 rawWorldPosition,
        GameObject spawnedObject = null,
        Vector3 spawnDragStartPosition = default,
        Bounds spawnDragStartBounds = default,
        bool hasSpawnDragStartBounds = false)
    {
        if (snappingSettings == null || !snappingSettings.snappingEnabled)
            return rawWorldPosition;

        if (spawnedObject != null && hasSpawnDragStartBounds)
        {
            return GizmoSnapToEdgeUtility.ResolveSnappedPlanePosition(
                rawWorldPosition,
                spawnDragStartPosition,
                spawnDragStartBounds,
                true,
                snappingSettings,
                GizmoAxis.All,
                Vector3.zero,
                new[] { spawnedObject });
        }

        return GizmoSnapToGridUtility.SnapVector(rawWorldPosition, snappingSettings.moveSnapSize, true);
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
            InputHandler.Instance.TriggerSelectionCommand -= OnLeftMouseButton;
            InputHandler.Instance.onPointEvent -= OnPoint;
        }

        StopDragging();
    }

    private IEnumerator WaitForInputHandler()
    {
        yield return new WaitUntil(() => InputHandler.Instance != null);

        InputHandler.Instance.TriggerSelectionCommand += OnLeftMouseButton;
        InputHandler.Instance.onPointEvent += OnPoint;
    }

    private void OnLeftMouseButton(SelectionCommand selectionCommand, InputAction.CallbackContext context)
    {
        if (cam == null)
            return;

        // Always end drag on mouse up, even when Ctrl changes the selection command to ToggleSelect.
        if (context.canceled)
        {
            StopDragging();
            return;
        }

        if (selectionCommand != SelectionCommand.Select)
            return;

        if (context.started)
            TryBeginDrag();
    }

    void Update()
    {
        if (isDragging && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
            StopDragging();
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
        if (gizmoObject == null)
            gizmoObject = handle.Owner;

        dragStartWorld = GetMouseWorldPosition();
        targetStartPosition = activeTarget.position;
        targetStartScale = activeTarget.localScale;
        targetStartRotationZ = activeTarget.eulerAngles.z;
        startMouseAngle = GetMouseAngleToTarget(activeTarget.position);

        // Debug.Log("logging action" + gizmoObject.dragLevelObjects.Count);
        if (gizmoObject.selectedLevelObjects.Count == 1)
        {
            currentAction = new TransformAction(gizmoObject.selectedLevelObjects[0]);
        }
        else
        {
            foreach (var levelObj in gizmoObject.selectedLevelObjects)
            {
                var t = new TransformAction(levelObj);
                transformActions.Add(t);
            }
        }

        hasSelectionBoundsWorldAtDragStart = TryGetCombinedColliderBoundsWorld(
            gizmoObject.selectedLevelObjects, out selectionBoundsWorldAtDragStart);

        isDragging = true;
    }

    static bool TryGetCombinedColliderBoundsWorld(List<LevelObject> levelObjects, out Bounds combined)
    {
        combined = default;
        if (levelObjects == null || levelObjects.Count == 0)
            return false;

        bool hasAny = false;
        for (int i = 0; i < levelObjects.Count; i++)
        {
            LevelObject lo = levelObjects[i];
            if (lo == null)
                continue;

            Collider2D[] cols = lo.GetComponentsInChildren<Collider2D>(true);
            for (int c = 0; c < cols.Length; c++)
            {
                Collider2D col = cols[c];
                if (col == null)
                    continue;

                if (!hasAny)
                {
                    combined = col.bounds;
                    hasAny = true;
                }
                else
                    combined.Encapsulate(col.bounds);
            }
        }

        return hasAny;
    }

    public static bool TryGetColliderBoundsWorld(GameObject root, out Bounds combined)
    {
        combined = default;
        if (root == null)
            return false;

        Collider2D[] cols = root.GetComponentsInChildren<Collider2D>(true);
        bool hasAny = false;
        for (int c = 0; c < cols.Length; c++)
        {
            Collider2D col = cols[c];
            if (col == null)
                continue;

            if (!hasAny)
            {
                combined = col.bounds;
                hasAny = true;
            }
            else
                combined.Encapsulate(col.bounds);
        }

        return hasAny;
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

        hasSelectionBoundsWorldAtDragStart = false;

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
                gizmoMoveOperation.Apply(context, currentMouseWorld, gizmoObject);
                break;

            case GizmoHandleMode.Rotate:
                gizmoRotationOperation.Apply(context, currentMouseWorld, gizmoObject);
                break;

            case GizmoHandleMode.Scale:
                // ApplyScale(currentMouseWorld);
                gizmoScaleOperation.Apply(context, currentMouseWorld, gizmoObject);
                break;
        }
    }

    GizmoDragContext CreateDragContext()
    {
        return new GizmoDragContext(
            activeHandle,
            activeTarget,
            dragStartWorld,
            targetStartPosition,
            targetStartScale,
            targetStartRotationZ,
            startMouseAngle,
            snappingSettings,
            selectionBoundsWorldAtDragStart,
            hasSelectionBoundsWorldAtDragStart);
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
