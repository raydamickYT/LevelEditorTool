using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class GizmoInteractionHandler : MonoBehaviour
{
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

    // Update is called once per frame
    void Update()
    {

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
        if (!TryGetHandleUnderPointer(out GizmoHandle handle))
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

        dragStartWorld = GetMouseWorldPosition();
        targetStartPosition = activeTarget.position;
        targetStartScale = activeTarget.localScale;
        targetStartRotationZ = activeTarget.eulerAngles.z;
        startMouseAngle = GetMouseAngleToTarget(activeTarget.position);

        isDragging = true;
    }
    private void UpdateDrag()
    {
        Vector3 currentMouseWorld = GetMouseWorldPosition();

        switch (activeHandle.Mode)
        {
            case GizmoHandleMode.Move:
                ApplyMove(currentMouseWorld);
                break;

            case GizmoHandleMode.Rotate:
                ApplyRotate();
                break;

            case GizmoHandleMode.Scale:
                ApplyScale(currentMouseWorld);
                break;
        }
    }

    private void ApplyMove(Vector3 currentMouseWorld)
    {
        Vector3 delta = currentMouseWorld - dragStartWorld;

        if (activeHandle.Axis == GizmoAxis.All)
        {
            activeTarget.position = targetStartPosition + delta;
            return;
        }

        Vector3 axis = activeHandle.GetAxisVectorWorld().normalized;
        float projectedDistance = Vector3.Dot(delta, axis);
        activeTarget.position = targetStartPosition + axis * projectedDistance;
    }

    private void ApplyRotate()
    {
        float currentAngle = GetMouseAngleToTarget(activeTarget.position);
        float deltaAngle = Mathf.DeltaAngle(startMouseAngle, currentAngle);
        float finalAngle = targetStartRotationZ + (deltaAngle * activeHandle.RotationSensitivity);

        activeTarget.rotation = Quaternion.Euler(0f, 0f, finalAngle);
    }

    private void ApplyScale(Vector3 currentMouseWorld)
    {
        Vector3 delta = currentMouseWorld - dragStartWorld;

        if (activeHandle.Axis == GizmoAxis.All)
        {
            float uniformDelta = (delta.x + delta.y) * activeHandle.ScaleSensitivity;
            Vector3 result = targetStartScale + Vector3.one * uniformDelta;
            activeTarget.localScale = ClampScale(result);
            return;
        }

        Vector3 axis = activeHandle.GetAxisVectorWorld().normalized;
        float projectedDistance = Vector3.Dot(delta, axis);
        Vector3 scaleDelta = activeHandle.Axis switch
        {
            GizmoAxis.X => new Vector3(projectedDistance, 0f, 0f),
            GizmoAxis.Y => new Vector3(0f, projectedDistance, 0f),
            GizmoAxis.Z => new Vector3(0f, 0f, projectedDistance),
            _ => Vector3.zero
        };

        Vector3 resultScale = targetStartScale + (scaleDelta * activeHandle.ScaleSensitivity);
        activeTarget.localScale = ClampScale(resultScale);
    }

    private Vector3 ClampScale(Vector3 scale)
    {
        const float minScale = 0.05f;
        return new Vector3(
            Mathf.Max(minScale, scale.x),
            Mathf.Max(minScale, scale.y),
            Mathf.Max(minScale, scale.z));
    }

    private void StopDragging()
    {
        isDragging = false;
        activeHandle = null;
        activeTarget = null;
    }

    private bool TryGetHandleUnderPointer(out GizmoHandle handle)
    {
        handle = null;

        if (!RaycastHelper.TryGetPointerHit2D(cam, LayerMask.GetMask(gizmoHandleLayerName), out RaycastHit2D hit))
            return false;

        handle = hit.collider.GetComponent<GizmoHandle>();
        if (handle == null)
            handle = hit.collider.GetComponentInParent<GizmoHandle>();


        return handle != null;
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
