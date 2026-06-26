using UnityEngine;
using UnityEngine.UIElements;

public sealed class ViewportFramePanelView : MonoBehaviour
{
    public static ViewportFramePanelView Instance { get; private set; }

    [SerializeField] UIDocument uiDocument;
    [SerializeField] int sortingOrder = 33100;

    VisualElement documentRoot;
    VisualElement panelRoot;
    VisualElement dragHeader;
    Toggle enabledToggle;
    FloatField pixelXField;
    FloatField pixelYField;
    FloatField pixelWidthField;
    FloatField pixelHeightField;
    Toggle lockAspectToggle;
    TextField outlineColorHexField;
    VisualElement outlineColorPreview;
    Button closeButton;
    bool isUpdatingUi;
    bool isDraggingWindow;
    Vector2 dragStartPointer;
    Vector2 dragStartPanelPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        TrySetSortingOrder();
        BindUi();
        LevelViewportFrameState.Instance.Changed += RefreshUi;
        HidePanel();
    }

    void OnDestroy()
    {
        LevelViewportFrameState.Instance.Changed -= RefreshUi;
        if (Instance == this)
            Instance = null;
    }

    public static void OpenPanel()
    {
        if (Instance == null)
        {
            Debug.LogWarning("Viewport frame panel is not available.");
            return;
        }

        Instance.ShowPanel();
    }

    public void ShowPanel()
    {
        if (documentRoot == null)
            return;

        documentRoot.style.display = DisplayStyle.Flex;
        panelRoot?.BringToFront();
        RefreshUi();
    }

    public void HidePanel()
    {
        if (documentRoot != null)
            documentRoot.style.display = DisplayStyle.None;
    }

    void TrySetSortingOrder()
    {
        if (uiDocument == null)
            return;

        System.Reflection.PropertyInfo property = typeof(UIDocument).GetProperty("sortingOrder");
        if (property == null || !property.CanWrite)
            return;

        property.SetValue(uiDocument, sortingOrder);
    }

    void BindUi()
    {
        if (uiDocument == null)
            return;

        VisualElement root = uiDocument.rootVisualElement;
        documentRoot = root.Q<VisualElement>("viewport-frame-root");
        panelRoot = root.Q<VisualElement>("viewport-frame-panel");
        dragHeader = root.Q<VisualElement>("viewport-frame-drag-header");
        enabledToggle = root.Q<Toggle>("viewport-enabled-toggle");
        pixelXField = root.Q<FloatField>("viewport-x");
        pixelYField = root.Q<FloatField>("viewport-y");
        pixelWidthField = root.Q<FloatField>("viewport-width");
        pixelHeightField = root.Q<FloatField>("viewport-height");
        lockAspectToggle = root.Q<Toggle>("viewport-lock-aspect");
        outlineColorHexField = root.Q<TextField>("viewport-outline-color-hex");
        outlineColorPreview = root.Q<VisualElement>("viewport-outline-color-preview");
        closeButton = root.Q<Button>("viewport-close-button");

        if (enabledToggle != null)
            enabledToggle.RegisterValueChangedCallback(_ => ApplyEnabled());

        if (pixelXField != null)
            pixelXField.RegisterValueChangedCallback(_ => ApplyPosition());

        if (pixelYField != null)
            pixelYField.RegisterValueChangedCallback(_ => ApplyPosition());

        if (pixelWidthField != null)
            pixelWidthField.RegisterValueChangedCallback(_ => ApplyWidth());

        if (pixelHeightField != null)
            pixelHeightField.RegisterValueChangedCallback(_ => ApplyHeight());

        if (lockAspectToggle != null)
            lockAspectToggle.RegisterValueChangedCallback(_ => ApplyLockAspect());

        if (outlineColorHexField != null)
        {
            outlineColorHexField.RegisterValueChangedCallback(_ => ApplyOutlineColorFromHex());
            outlineColorHexField.RegisterCallback<FocusOutEvent>(_ => NormalizeOutlineHexField());
        }

        if (closeButton != null)
            closeButton.clicked += HidePanel;

        EnableNumericDragHandles(root);
        RegisterWindowDrag();
    }

    void EnableNumericDragHandles(VisualElement root)
    {
        DraggableNumericLabel.Enable(root.Q<Label>("viewport-x-label"), pixelXField);
        DraggableNumericLabel.Enable(root.Q<Label>("viewport-y-label"), pixelYField);
        DraggableNumericLabel.Enable(root.Q<Label>("viewport-width-label"), pixelWidthField);
        DraggableNumericLabel.Enable(root.Q<Label>("viewport-height-label"), pixelHeightField);
    }

    void RegisterWindowDrag()
    {
        if (dragHeader == null || panelRoot == null)
            return;

        dragHeader.RegisterCallback<PointerDownEvent>(OnWindowPointerDown);
        dragHeader.RegisterCallback<PointerMoveEvent>(OnWindowPointerMove);
        dragHeader.RegisterCallback<PointerUpEvent>(OnWindowPointerUp);
        dragHeader.RegisterCallback<PointerCancelEvent>(OnWindowPointerCancel);
    }

    void OnWindowPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0 || panelRoot == null)
            return;

        if (evt.target is Button)
            return;

        dragHeader.CapturePointer(evt.pointerId);
        isDraggingWindow = true;
        dragStartPointer = evt.position;
        dragStartPanelPosition = new Vector2(panelRoot.resolvedStyle.left, panelRoot.resolvedStyle.top);
        evt.StopPropagation();
    }

    void OnWindowPointerMove(PointerMoveEvent evt)
    {
        if (!isDraggingWindow || panelRoot == null || !dragHeader.HasPointerCapture(evt.pointerId))
            return;

        Vector2 delta = (Vector2)evt.position - dragStartPointer;
        panelRoot.style.left = dragStartPanelPosition.x + delta.x;
        panelRoot.style.top = dragStartPanelPosition.y + delta.y;
        evt.StopPropagation();
    }

    void OnWindowPointerUp(PointerUpEvent evt)
    {
        if (!dragHeader.HasPointerCapture(evt.pointerId))
            return;

        dragHeader.ReleasePointer(evt.pointerId);
        isDraggingWindow = false;
        evt.StopPropagation();
    }

    void OnWindowPointerCancel(PointerCancelEvent evt)
    {
        if (!dragHeader.HasPointerCapture(evt.pointerId))
            return;

        dragHeader.ReleasePointer(evt.pointerId);
        isDraggingWindow = false;
    }

    void RefreshUi()
    {
        if (panelRoot == null)
            return;

        isUpdatingUi = true;
        LevelViewportFrameState state = LevelViewportFrameState.Instance;

        if (enabledToggle != null)
            enabledToggle.SetValueWithoutNotify(state.Enabled);

        if (pixelXField != null)
            pixelXField.SetValueWithoutNotify(state.PixelX);

        if (pixelYField != null)
            pixelYField.SetValueWithoutNotify(state.PixelY);

        if (pixelWidthField != null)
            pixelWidthField.SetValueWithoutNotify(state.PixelWidth);

        if (pixelHeightField != null)
            pixelHeightField.SetValueWithoutNotify(state.PixelHeight);

        if (lockAspectToggle != null)
            lockAspectToggle.SetValueWithoutNotify(state.LockAspectRatio);

        if (outlineColorHexField != null)
            outlineColorHexField.SetValueWithoutNotify(ViewportOutlineColorUtil.ColorToHexRgb(state.OutlineColor));

        UpdateOutlinePreview(state.OutlineColor);

        isUpdatingUi = false;
    }

    void ApplyEnabled()
    {
        if (isUpdatingUi || enabledToggle == null)
            return;

        LevelViewportFrameState.Instance.Enabled = enabledToggle.value;
        LevelProjectDirtyState.MarkDirty();
    }

    void ApplyPosition()
    {
        if (isUpdatingUi || pixelXField == null || pixelYField == null)
            return;

        LevelViewportFrameState state = LevelViewportFrameState.Instance;
        state.PixelX = pixelXField.value;
        state.PixelY = pixelYField.value;
        LevelProjectDirtyState.MarkDirty();
    }

    void ApplyWidth()
    {
        if (isUpdatingUi || pixelWidthField == null)
            return;

        LevelViewportFrameState.Instance.PixelWidth = pixelWidthField.value;
        LevelProjectDirtyState.MarkDirty();
    }

    void ApplyHeight()
    {
        if (isUpdatingUi || pixelHeightField == null)
            return;

        LevelViewportFrameState.Instance.PixelHeight = pixelHeightField.value;
        LevelProjectDirtyState.MarkDirty();
    }

    void ApplyLockAspect()
    {
        if (isUpdatingUi || lockAspectToggle == null)
            return;

        LevelViewportFrameState.Instance.LockAspectRatio = lockAspectToggle.value;
        LevelProjectDirtyState.MarkDirty();
    }

    void ApplyOutlineColorFromHex()
    {
        if (isUpdatingUi || outlineColorHexField == null)
            return;

        LevelViewportFrameState state = LevelViewportFrameState.Instance;
        if (!ViewportOutlineColorUtil.TryParseHex(outlineColorHexField.value, state.OutlineColor, out Color parsed))
            return;

        state.OutlineColor = parsed;
        UpdateOutlinePreview(parsed);
        LevelProjectDirtyState.MarkDirty();
    }

    void NormalizeOutlineHexField()
    {
        if (isUpdatingUi || outlineColorHexField == null)
            return;

        Color current = LevelViewportFrameState.Instance.OutlineColor;
        if (!ViewportOutlineColorUtil.TryParseHex(outlineColorHexField.value, current, out Color parsed))
        {
            isUpdatingUi = true;
            outlineColorHexField.SetValueWithoutNotify(ViewportOutlineColorUtil.ColorToHexRgb(current));
            isUpdatingUi = false;
            return;
        }

        isUpdatingUi = true;
        outlineColorHexField.SetValueWithoutNotify(ViewportOutlineColorUtil.ColorToHexRgb(parsed));
        isUpdatingUi = false;
        LevelViewportFrameState.Instance.OutlineColor = parsed;
        UpdateOutlinePreview(parsed);
        LevelProjectDirtyState.MarkDirty();
    }

    void UpdateOutlinePreview(Color color)
    {
        if (outlineColorPreview == null)
            return;

        outlineColorPreview.style.backgroundColor = new StyleColor(color);
    }
}
