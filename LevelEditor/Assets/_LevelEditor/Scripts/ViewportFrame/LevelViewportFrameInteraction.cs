using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Move-only interaction for the viewport frame border. Dimensions are edited via the panel, not by scaling.
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelViewportFrameInteraction : MonoBehaviour
{
    public static LevelViewportFrameInteraction Instance { get; private set; }

    const string LayerName = "ViewportFrame";
    const float BorderPickThicknessWorld = 0.18f;

    [SerializeField] Camera cam;

    readonly List<BoxCollider2D> borderColliders = new();
    Transform pickRoot;
    bool isDragging;
    Vector3 dragStartWorld;
    float dragStartPixelX;
    float dragStartPixelY;
    Coroutine subscribeRoutine;
    bool inputSubscribed;

    public static int PickLayerMask => UnityEngine.LayerMask.GetMask(LayerName);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsurePickRoot();
        LevelViewportFrameState.Instance.Changed += RebuildPickColliders;
    }

    void OnDestroy()
    {
        LevelViewportFrameState.Instance.Changed -= RebuildPickColliders;
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void OnEnable()
    {
        subscribeRoutine = StartCoroutine(WaitForInputHandler());
        RebuildPickColliders();
    }

    void OnDisable()
    {
        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        UnsubscribeInput();
        CancelDrag();
    }

    IEnumerator WaitForInputHandler()
    {
        yield return new WaitUntil(() => InputHandler.Instance != null);
        SubscribeInput();
    }

    void SubscribeInput()
    {
        if (inputSubscribed || InputHandler.Instance == null)
            return;

        InputHandler.Instance.onPointEvent += OnPoint;
        inputSubscribed = true;
    }

    void UnsubscribeInput()
    {
        if (!inputSubscribed || InputHandler.Instance == null)
            return;

        InputHandler.Instance.onPointEvent -= OnPoint;
        inputSubscribed = false;
    }

    public bool IsPointerOverFrameBorder()
    {
        if (!LevelViewportFrameState.Instance.Enabled || cam == null)
            return false;

        return RaycastHelper.TryGetPointerHit2D(cam, PickLayerMask, out _);
    }

    public bool TryBeginInteractionOnPointerDown()
    {
        if (!IsPointerOverFrameBorder())
            return false;

        LevelViewportFrameState state = LevelViewportFrameState.Instance;
        state.Select();

        EventManager.Instance.TriggerDelegate(
            SelectionEvents.ReplaceSelectionWithObject,
            Enumerable.Empty<GameObject>());

        BeginDrag();
        return true;
    }

    public void EndPointerInteraction() => EndDrag();

    void OnPoint(InputAction.CallbackContext context)
    {
        if (!isDragging || !context.performed || cam == null || Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 currentWorld = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0f));
        currentWorld.z = 0f;

        Vector2 worldDelta = currentWorld - dragStartWorld;
        Vector2 pixelDelta = LevelViewportFrameUtil.WorldDeltaToPixelDelta(worldDelta, LevelViewportFrameState.Instance.PixelScale);

        LevelViewportFrameState state = LevelViewportFrameState.Instance;
        state.PixelX = dragStartPixelX + pixelDelta.x;
        state.PixelY = dragStartPixelY + pixelDelta.y;
        LevelProjectDirtyState.MarkDirty();
    }

    void BeginDrag()
    {
        if (cam == null || Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        dragStartWorld = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0f));
        dragStartWorld.z = 0f;

        LevelViewportFrameState state = LevelViewportFrameState.Instance;
        dragStartPixelX = state.PixelX;
        dragStartPixelY = state.PixelY;
        isDragging = true;
    }

    void EndDrag()
    {
        isDragging = false;
    }

    void CancelDrag() => isDragging = false;

    void EnsurePickRoot()
    {
        if (pickRoot != null)
            return;

        GameObject root = new GameObject("ViewportFramePickColliders");
        root.transform.SetParent(transform, false);
        pickRoot = root.transform;
    }

    void RebuildPickColliders()
    {
        EnsurePickRoot();
        ClearBorderColliders();

        LevelViewportFrameState state = LevelViewportFrameState.Instance;
        if (!state.Enabled)
            return;

        int layer = UnityEngine.LayerMask.NameToLayer(LayerName);
        if (layer < 0)
            layer = 0;

        Bounds bounds = state.WorldBounds;
        float thickness = BorderPickThicknessWorld;
        float z = bounds.center.z;

        CreateBorderCollider("Top", layer, bounds.center.x, bounds.max.y - thickness * 0.5f, z, bounds.size.x, thickness);
        CreateBorderCollider("Bottom", layer, bounds.center.x, bounds.min.y + thickness * 0.5f, z, bounds.size.x, thickness);
        CreateBorderCollider("Left", layer, bounds.min.x + thickness * 0.5f, bounds.center.y, z, thickness, bounds.size.y);
        CreateBorderCollider("Right", layer, bounds.max.x - thickness * 0.5f, bounds.center.y, z, thickness, bounds.size.y);
    }

    void CreateBorderCollider(string label, int layer, float centerX, float centerY, float z, float width, float height)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(pickRoot, false);
        go.transform.position = new Vector3(centerX, centerY, z);
        go.layer = layer;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(Mathf.Max(0.01f, width), Mathf.Max(0.01f, height));
        borderColliders.Add(collider);
    }

    void ClearBorderColliders()
    {
        for (int i = borderColliders.Count - 1; i >= 0; i--)
        {
            if (borderColliders[i] != null)
                Destroy(borderColliders[i].gameObject);
        }

        borderColliders.Clear();
    }
}
