using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ExternalJsonMappingWizardView : MonoBehaviour
{
    public static ExternalJsonMappingWizardView Instance { get; private set; }

    const string UxmlResourcePath = "ExternalJson/ExternalJsonMappingWizard";

    const string TooltipProfileName =
        "Friendly name for this JSON format mapping. Saved as a .leveleditor-profile.json file beside your level JSON for re-import.";

    const string TooltipPixelScale =
        "Converts pixel coordinates from the JSON into editor world units. 0.01 means 100 JSON pixels = 1 world unit.";

    const string TooltipViewportSection =
        "Optional. Reads width/height numbers from the JSON to size the editor viewport frame. This is editor-only and is not written back to the game JSON on export.";

    const string TooltipViewportWidthPath =
        "JSON path to the level width in pixels (e.g. width or groundWidth). Only used to size the editor viewport frame.";

    const string TooltipViewportHeightPath =
        "JSON path to the level height in pixels (e.g. height). Only used to size the editor viewport frame.";

    const string TooltipObjectSourcesSection =
        "Each detected JSON array or object that contains positions. Enable the sources you want imported and map their field names below.";

    const string TooltipSourceEnabled =
        "When enabled, objects from this JSON path are imported into the level.";

    const string TooltipShape =
        "How each entry is stored in the JSON: point (x,y), rectangle (x,y,width,height), or a numeric array such as [x,y] or [x,y,w,h].";

    const string TooltipFieldX =
        "JSON property name for horizontal position in pixels (usually the top-left corner).";

    const string TooltipFieldY =
        "JSON property name for vertical position in pixels (usually top-left; Y increases downward in the JSON).";

    const string TooltipFieldW =
        "JSON property name for object width in pixels. Used for rectangles; ignored for point-only shapes.";

    const string TooltipFieldH =
        "JSON property name for object height in pixels. Used for rectangles; ignored for point-only shapes.";

    [SerializeField] UIDocument uiDocument;
    [SerializeField] int sortingOrder = 33200;

    VisualElement documentRoot;
    VisualElement panelRoot;
    VisualElement dragHeader;
    VisualElement sourcesContainer;
    FloatField pixelScaleField;
    TextField displayNameField;
    TextField viewportWidthField;
    TextField viewportHeightField;
    Button importButton;
    Button cancelButton;
    Button closeButton;
    Label hoverTooltip;
    IVisualElementScheduledItem tooltipDelay;

    const int TooltipDelayMs = 400;

    string pendingJson;
    string pendingSourcePath;
    ExternalJsonImportProfile workingProfile;
    readonly List<SourceRowBinding> sourceRows = new();
    bool isDraggingWindow;
    Vector2 dragStartPointer;
    Vector2 dragStartPanelPosition;

    sealed class SourceRowBinding
    {
        public ExternalJsonObjectSourceProfile Source;
        public Toggle EnabledToggle;
        public EnumField ShapeField;
        public TextField XField;
        public TextField YField;
        public TextField WidthField;
        public TextField HeightField;
    }

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
        HidePanel();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Open(string json, string sourcePath, ExternalJsonImportProfile suggestedProfile)
    {
        if (Instance == null)
        {
            Debug.LogWarning("JSON mapping wizard is not available.");
            return;
        }

        Instance.Show(json, sourcePath, suggestedProfile);
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
        documentRoot = root.Q<VisualElement>("json-mapping-root");
        panelRoot = root.Q<VisualElement>("json-mapping-panel");
        dragHeader = root.Q<VisualElement>("json-mapping-drag-header");
        sourcesContainer = root.Q<VisualElement>("json-mapping-sources");
        pixelScaleField = root.Q<FloatField>("json-mapping-pixel-scale");
        displayNameField = root.Q<TextField>("json-mapping-display-name");
        viewportWidthField = root.Q<TextField>("json-mapping-viewport-width");
        viewportHeightField = root.Q<TextField>("json-mapping-viewport-height");
        importButton = root.Q<Button>("json-mapping-import-button");
        cancelButton = root.Q<Button>("json-mapping-cancel-button");
        closeButton = root.Q<Button>("json-mapping-close-button");
        hoverTooltip = root.Q<Label>("json-mapping-tooltip");

        if (importButton != null)
            importButton.clicked += OnImportClicked;

        if (cancelButton != null)
            cancelButton.clicked += HidePanel;

        if (closeButton != null)
            closeButton.clicked += HidePanel;

        ApplyStaticTooltips(root);
        RegisterWindowDrag();
    }

    void ApplyStaticTooltips(VisualElement root)
    {
        RegisterTooltip(displayNameField, TooltipProfileName);
        RegisterTooltip(pixelScaleField, TooltipPixelScale);
        RegisterTooltip(root.Q<Label>("json-mapping-viewport-section"), TooltipViewportSection);
        RegisterTooltip(viewportWidthField, TooltipViewportWidthPath);
        RegisterTooltip(viewportHeightField, TooltipViewportHeightPath);
        RegisterTooltip(root.Q<Label>("json-mapping-sources-title"), TooltipObjectSourcesSection);
    }

    void RegisterTooltip(VisualElement element, string tooltip)
    {
        if (element == null || string.IsNullOrEmpty(tooltip))
            return;

        element.RegisterCallback<PointerEnterEvent>(_ => BeginTooltipDelay(element, tooltip));
        element.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            CancelTooltipDelay();
            HideTooltip();
        });
    }

    void BeginTooltipDelay(VisualElement anchor, string tooltip)
    {
        CancelTooltipDelay();
        tooltipDelay = anchor.schedule.Execute(() => ShowTooltip(anchor, tooltip)).StartingIn(TooltipDelayMs);
    }

    void CancelTooltipDelay()
    {
        tooltipDelay?.Pause();
        tooltipDelay = null;
    }

    void ShowTooltip(VisualElement anchor, string tooltip)
    {
        if (hoverTooltip == null || documentRoot == null || anchor == null)
            return;

        hoverTooltip.text = tooltip;
        hoverTooltip.style.display = DisplayStyle.Flex;
        hoverTooltip.BringToFront();

        Rect anchorBounds = anchor.worldBound;
        Vector2 belowAnchor = new(anchorBounds.xMin, anchorBounds.yMax + 4f);
        Vector2 localPosition = documentRoot.WorldToLocal(belowAnchor);

        hoverTooltip.style.left = localPosition.x;
        hoverTooltip.style.top = localPosition.y;
    }

    void HideTooltip()
    {
        if (hoverTooltip != null)
            hoverTooltip.style.display = DisplayStyle.None;
    }

    void Show(string json, string sourcePath, ExternalJsonImportProfile suggestedProfile)
    {
        pendingJson = json;
        pendingSourcePath = sourcePath;
        workingProfile = CloneProfile(suggestedProfile);

        if (displayNameField != null)
            displayNameField.value = workingProfile.displayName ?? string.Empty;

        if (pixelScaleField != null)
            pixelScaleField.value = workingProfile.pixelScale;

        if (viewportWidthField != null)
            viewportWidthField.value = workingProfile.viewportWidthPath ?? string.Empty;

        if (viewportHeightField != null)
            viewportHeightField.value = workingProfile.viewportHeightPath ?? string.Empty;

        RebuildSourceRows();
        documentRoot.style.display = DisplayStyle.Flex;
        panelRoot?.BringToFront();
    }

    void HidePanel()
    {
        CancelTooltipDelay();
        HideTooltip();

        if (documentRoot != null)
            documentRoot.style.display = DisplayStyle.None;
    }

    void RebuildSourceRows()
    {
        sourceRows.Clear();
        if (sourcesContainer == null)
            return;

        sourcesContainer.Clear();

        foreach (ExternalJsonObjectSourceProfile source in workingProfile.GetObjectSourcesList())
        {
            var row = new VisualElement();
            row.AddToClassList("json-mapping-source-row");

            var header = new VisualElement();
            header.AddToClassList("json-mapping-source-header");

            var enabledToggle = new Toggle { label = source.jsonPath, value = source.enabled };
            enabledToggle.AddToClassList("json-mapping-source-toggle");
            RegisterTooltip(enabledToggle, TooltipSourceEnabled);
            header.Add(enabledToggle);

            var shapeField = new EnumField("Shape", source.shape);
            shapeField.AddToClassList("json-mapping-shape-field");
            RegisterTooltip(shapeField, TooltipShape);
            header.Add(shapeField);

            row.Add(header);

            var fieldsRow = new VisualElement();
            fieldsRow.AddToClassList("row");

            var xField = new TextField("X") { value = source.xField };
            var yField = new TextField("Y") { value = source.yField };
            var widthField = new TextField("W") { value = source.widthField };
            var heightField = new TextField("H") { value = source.heightField };

            RegisterTooltip(xField, TooltipFieldX);
            RegisterTooltip(yField, TooltipFieldY);
            RegisterTooltip(widthField, TooltipFieldW);
            RegisterTooltip(heightField, TooltipFieldH);


            fieldsRow.Add(xField);
            fieldsRow.Add(yField);
            fieldsRow.Add(widthField);
            fieldsRow.Add(heightField);
            row.Add(fieldsRow);

            sourcesContainer.Add(row);

            sourceRows.Add(new SourceRowBinding
            {
                Source = source,
                EnabledToggle = enabledToggle,
                ShapeField = shapeField,
                XField = xField,
                YField = yField,
                WidthField = widthField,
                HeightField = heightField,
            });
        }
    }

    void OnImportClicked()
    {
        ApplyUiToProfile();
        ExternalLevelImportResult result = ExternalLevelJsonImportService.ImportWithProfile(
            workingProfile,
            pendingJson,
            pendingSourcePath);

        HidePanel();

        if (!result.Success)
        {
            EditorPopupService.ShowWarning(
                "Import failed",
                string.IsNullOrEmpty(result.ErrorMessage)
                    ? "The mapped JSON could not be imported."
                    : result.ErrorMessage,
                pendingSourcePath);
            return;
        }

        string message = $"Imported {result.SpawnedObjectCount} objects using {result.FormatDisplayName}.";
        if (result.Warnings.Count > 0)
            message += "\n" + string.Join("\n", result.Warnings);

        EditorPopupService.ShowToast(message);
    }

    void ApplyUiToProfile()
    {
        if (workingProfile == null)
            return;

        if (displayNameField != null)
        {
            workingProfile.displayName = displayNameField.value;
            if (!string.IsNullOrWhiteSpace(pendingSourcePath))
            {
                string baseName = Path.GetFileNameWithoutExtension(pendingSourcePath);
                workingProfile.formatId = "profile." + baseName.ToLowerInvariant();
            }
        }

        if (pixelScaleField != null)
            workingProfile.pixelScale = Mathf.Max(0.0001f, pixelScaleField.value);

        if (viewportWidthField != null)
            workingProfile.viewportWidthPath = viewportWidthField.value?.Trim() ?? string.Empty;

        if (viewportHeightField != null)
            workingProfile.viewportHeightPath = viewportHeightField.value?.Trim() ?? string.Empty;

        var sources = new List<ExternalJsonObjectSourceProfile>();
        foreach (SourceRowBinding row in sourceRows)
        {
            ExternalJsonObjectSourceProfile source = row.Source;
            source.enabled = row.EnabledToggle.value;
            source.shape = (ExternalJsonShapeKind)row.ShapeField.value;
            source.xField = row.XField.value;
            source.yField = row.YField.value;
            source.widthField = row.WidthField.value;
            source.heightField = row.HeightField.value;
            sources.Add(source);
        }

        workingProfile.SetObjectSources(sources);
    }

    static ExternalJsonImportProfile CloneProfile(ExternalJsonImportProfile profile)
    {
        if (profile == null)
            return new ExternalJsonImportProfile();

        string json = JsonUtility.ToJson(profile);
        return JsonUtility.FromJson<ExternalJsonImportProfile>(json);
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
        if (evt.button != 0 || evt.target is Button)
            return;

        dragHeader.CapturePointer(evt.pointerId);
        isDraggingWindow = true;
        dragStartPointer = evt.position;
        dragStartPanelPosition = new Vector2(panelRoot.resolvedStyle.left, panelRoot.resolvedStyle.top);
        evt.StopPropagation();
    }

    void OnWindowPointerMove(PointerMoveEvent evt)
    {
        if (!isDraggingWindow || !dragHeader.HasPointerCapture(evt.pointerId))
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
}

public static class ExternalJsonMappingWizardBootstrap
{
    const string UxmlResourcePath = "ExternalJson/ExternalJsonMappingWizard";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureInitialized()
    {
        if (UnityEngine.Object.FindAnyObjectByType<ExternalJsonMappingWizardView>() != null)
            return;

        VisualTreeAsset wizardAsset = Resources.Load<VisualTreeAsset>(UxmlResourcePath);
        UIDocument referenceDocument = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
        if (wizardAsset == null || referenceDocument == null)
            return;

        GameObject host = new GameObject("ExternalJsonMappingWizard");
        UIDocument document = host.AddComponent<UIDocument>();
        document.panelSettings = referenceDocument.panelSettings;
        document.visualTreeAsset = wizardAsset;
        host.AddComponent<ExternalJsonMappingWizardView>();
    }
}
