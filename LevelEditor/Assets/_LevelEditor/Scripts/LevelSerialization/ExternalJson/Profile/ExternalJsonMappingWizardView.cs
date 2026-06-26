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
        "Friendly name for this level format mapping. Saved with the Level Editor project so it can be edited and reused later.";

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
        "How each JSON entry is read: point (x,y), rectangle (x,y,width,height), or a numeric array such as [x,y]. "
        + "This also sets the placeholder sprite shape when Sprite is set to Placeholder.";

    const string TooltipFieldX =
        "JSON property name for horizontal position in pixels (usually the top-left corner).";

    const string TooltipFieldY =
        "JSON property name for vertical position in pixels (usually top-left; Y increases downward in the JSON).";

    const string TooltipFieldW =
        "JSON property name for object width in pixels. Used for rectangles; ignored for point-only shapes.";

    const string TooltipFieldH =
        "JSON property name for object height in pixels. Used for rectangles; ignored for point-only shapes.";

    const string TooltipDiscriminatorField =
        "Optional. JSON property used to filter items inside a shared array (e.g. type). Leave empty to import every item at this path.";

    const string TooltipDiscriminatorValue =
        "Only import items where the discriminator field equals this value (e.g. block or enemy).";

    const string TooltipSpriteMode =
        "Placeholder uses the built-in shape sprite (matches Shape above, colored per category). Pick a library sprite or 'Custom file...' to import your own.";

    [SerializeField] UIDocument uiDocument;
    [SerializeField] int sortingOrder = 33200;

    VisualElement documentRoot;
    VisualElement panelRoot;
    VisualElement dragHeader;
    VisualElement sourcesContainer;
    Label titleLabel;
    FloatField pixelScaleField;
    TextField displayNameField;
    TextField viewportWidthField;
    TextField viewportHeightField;
    Button importButton;
    Button cancelButton;
    Button closeButton;
    Label hoverTooltip;
    VisualElement resizeGrip;
    IVisualElementScheduledItem tooltipDelay;
    bool isResizingWindow;
    Vector2 resizeStartPointer;
    Vector2 resizeStartSize;

    const int TooltipDelayMs = 400;

    string pendingJson;
    string pendingSourcePath;
    ExternalJsonImportProfile workingProfile;
    bool editProfileOnly;
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
        public TextField DiscriminatorField;
        public TextField DiscriminatorValueField;
        public PopupField<string> SpriteField;
    }

    const string CustomSpriteKey = "__custom_file__";
    const string PlaceholderKey = "placeholder";
    const string AssetKeyPrefix = "asset:";

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

        Instance.Show(json, sourcePath, suggestedProfile, editOnly: false);
    }

    public static void OpenForEdit(ExternalJsonImportProfile profile)
    {
        if (Instance == null)
        {
            Debug.LogWarning("JSON mapping wizard is not available.");
            return;
        }

        Instance.Show(string.Empty, string.Empty, profile, editOnly: true);
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
        titleLabel = root.Q<Label>("json-mapping-title");
        sourcesContainer = root.Q<VisualElement>("json-mapping-sources");
        pixelScaleField = root.Q<FloatField>("json-mapping-pixel-scale");
        displayNameField = root.Q<TextField>("json-mapping-display-name");
        viewportWidthField = root.Q<TextField>("json-mapping-viewport-width");
        viewportHeightField = root.Q<TextField>("json-mapping-viewport-height");
        importButton = root.Q<Button>("json-mapping-import-button");
        cancelButton = root.Q<Button>("json-mapping-cancel-button");
        closeButton = root.Q<Button>("json-mapping-close-button");
        hoverTooltip = root.Q<Label>("json-mapping-tooltip");
        resizeGrip = root.Q<VisualElement>("json-mapping-resize-grip");

        if (importButton != null)
            importButton.clicked += OnImportClicked;

        if (cancelButton != null)
            cancelButton.clicked += HidePanel;

        if (closeButton != null)
            closeButton.clicked += HidePanel;

        ApplyStaticTooltips(root);
        RegisterWindowDrag();
        RegisterWindowResize();
    }

    void RegisterWindowResize()
    {
        if (resizeGrip == null || panelRoot == null)
            return;

        resizeGrip.RegisterCallback<PointerDownEvent>(OnResizePointerDown);
        resizeGrip.RegisterCallback<PointerMoveEvent>(OnResizePointerMove);
        resizeGrip.RegisterCallback<PointerUpEvent>(OnResizePointerUp);
        resizeGrip.RegisterCallback<PointerCancelEvent>(OnResizePointerCancel);
    }

    void OnResizePointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0)
            return;

        resizeGrip.CapturePointer(evt.pointerId);
        isResizingWindow = true;
        resizeStartPointer = evt.position;
        resizeStartSize = new Vector2(panelRoot.resolvedStyle.width, panelRoot.resolvedStyle.height);
        evt.StopPropagation();
    }

    void OnResizePointerMove(PointerMoveEvent evt)
    {
        if (!isResizingWindow || !resizeGrip.HasPointerCapture(evt.pointerId))
            return;

        Vector2 delta = (Vector2)evt.position - resizeStartPointer;
        panelRoot.style.width = Mathf.Max(360f, resizeStartSize.x + delta.x);
        panelRoot.style.height = Mathf.Max(320f, resizeStartSize.y + delta.y);
        evt.StopPropagation();
    }

    void OnResizePointerUp(PointerUpEvent evt)
    {
        if (!resizeGrip.HasPointerCapture(evt.pointerId))
            return;

        resizeGrip.ReleasePointer(evt.pointerId);
        isResizingWindow = false;
        evt.StopPropagation();
    }

    void OnResizePointerCancel(PointerCancelEvent evt)
    {
        if (!resizeGrip.HasPointerCapture(evt.pointerId))
            return;

        resizeGrip.ReleasePointer(evt.pointerId);
        isResizingWindow = false;
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

    void RegisterFieldTooltip(VisualElement field, string tooltip)
    {
        RegisterTooltip(field, tooltip);
        if (field == null)
            return;

        Label fieldLabel = field.Q<Label>();
        if (fieldLabel != null)
            RegisterTooltip(fieldLabel, tooltip);
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

    void Show(string json, string sourcePath, ExternalJsonImportProfile suggestedProfile, bool editOnly)
    {
        pendingJson = json;
        pendingSourcePath = sourcePath;
        workingProfile = CloneProfile(suggestedProfile);
        editProfileOnly = editOnly;

        if (titleLabel != null)
            titleLabel.text = editProfileOnly ? "Edit Level Format Profile" : "Map JSON Structure";

        if (importButton != null)
            importButton.text = editProfileOnly ? "Save Profile" : "Import";

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

            string pathLabel = FormatSourceLabel(source);
            var enabledToggle = new Toggle { label = pathLabel, value = source.enabled };
            enabledToggle.AddToClassList("json-mapping-source-toggle");
            RegisterTooltip(enabledToggle, TooltipSourceEnabled);
            header.Add(enabledToggle);

            var shapeField = new EnumField("Shape", source.shape);
            shapeField.AddToClassList("json-mapping-shape-field");
            RegisterFieldTooltip(shapeField, TooltipShape);
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

            var discriminatorRow = new VisualElement();
            discriminatorRow.AddToClassList("row");

            var discriminatorField = new TextField("Type field") { value = source.discriminatorField ?? string.Empty };
            var discriminatorValueField = new TextField("Type value") { value = source.discriminatorValue ?? string.Empty };
            RegisterTooltip(discriminatorField, TooltipDiscriminatorField);
            RegisterTooltip(discriminatorValueField, TooltipDiscriminatorValue);

            discriminatorRow.Add(discriminatorField);
            discriminatorRow.Add(discriminatorValueField);
            row.Add(discriminatorRow);

            var spriteRow = new VisualElement();
            spriteRow.AddToClassList("row");

            var spriteChoices = BuildSpriteChoices();
            string currentKey = GetSpriteKeyForSource(source);
            if (!spriteChoices.Contains(currentKey))
                currentKey = PlaceholderKey;

            var spriteField = new PopupField<string>(
                "Sprite",
                spriteChoices,
                spriteChoices.IndexOf(currentKey),
                FormatSpriteChoice,
                FormatSpriteChoice);
            spriteField.AddToClassList("json-mapping-sprite-asset-field");
            RegisterTooltip(spriteField, TooltipSpriteMode);

            spriteField.RegisterValueChangedCallback(evt => OnSpriteChoiceChanged(spriteField, evt.previousValue, evt.newValue));

            spriteRow.Add(spriteField);
            row.Add(spriteRow);

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
                DiscriminatorField = discriminatorField,
                DiscriminatorValueField = discriminatorValueField,
                SpriteField = spriteField,
            });
        }
    }

    static string GetSpriteKeyForSource(ExternalJsonObjectSourceProfile source)
    {
        if (source == null)
            return PlaceholderKey;

        if (source.spriteMode == ExternalJsonSpriteMode.Custom && !string.IsNullOrWhiteSpace(source.spriteAssetId))
            return AssetKeyPrefix + source.spriteAssetId.Trim();

        return PlaceholderKey;
    }

    static List<string> BuildSpriteChoices()
    {
        var choices = new List<string> { PlaceholderKey };

        foreach (ImportedAssetMetaData asset in AssetStorageService.GetAllCachedImportedAssets())
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.AssetID))
                continue;

            if (!string.Equals(asset.AssetType, ImportedAssetTypes.Sprite, StringComparison.OrdinalIgnoreCase))
                continue;

            choices.Add(AssetKeyPrefix + asset.AssetID);
        }

        choices.Add(CustomSpriteKey);
        return choices;
    }

    static string FormatSpriteChoice(string key)
    {
        if (string.Equals(key, PlaceholderKey, StringComparison.Ordinal))
            return "Placeholder";

        if (string.Equals(key, CustomSpriteKey, StringComparison.Ordinal))
            return "Custom file...";

        if (key != null && key.StartsWith(AssetKeyPrefix, StringComparison.Ordinal))
        {
            string assetId = key.Substring(AssetKeyPrefix.Length);
            ImportedAssetMetaData meta = AssetStorageService.GetAssetByID(assetId);
            if (meta != null && !string.IsNullOrWhiteSpace(meta.FileName))
                return meta.FileName;

            return assetId;
        }

        return key;
    }

    void OnSpriteChoiceChanged(PopupField<string> spriteField, string previousValue, string newValue)
    {
        if (spriteField == null || !string.Equals(newValue, CustomSpriteKey, StringComparison.Ordinal))
            return;

        if (ExternalJsonWizardSpriteImport.TryPickAndImport(out string assetId)
            && !string.IsNullOrWhiteSpace(assetId))
        {
            string newKey = AssetKeyPrefix + assetId;
            List<string> refreshed = BuildSpriteChoices();
            if (!refreshed.Contains(newKey))
                refreshed.Insert(refreshed.Count - 1, newKey);

            spriteField.choices = refreshed;
            spriteField.SetValueWithoutNotify(newKey);
            return;
        }

        // Cancelled or failed: revert to the previous selection.
        spriteField.SetValueWithoutNotify(
            string.Equals(previousValue, CustomSpriteKey, StringComparison.Ordinal) ? PlaceholderKey : previousValue);
    }

    static string FormatSourceLabel(ExternalJsonObjectSourceProfile source)
    {
        string path = string.IsNullOrWhiteSpace(source.jsonPath) ? JsonPathResolver.RootPath : source.jsonPath;
        if (string.IsNullOrWhiteSpace(source.discriminatorField) || string.IsNullOrWhiteSpace(source.discriminatorValue))
            return path;

        return $"{path} [{source.discriminatorField}={source.discriminatorValue}]";
    }

    void OnImportClicked()
    {
        ApplyUiToProfile();

        if (editProfileOnly)
        {
            SaveEditedProfile();
            return;
        }

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

    void SaveEditedProfile()
    {
        HidePanel();

        if (workingProfile == null)
        {
            EditorPopupService.ShowWarning("No profile", "There is no level format profile to save.");
            return;
        }

        ExternalLevelJsonImportSession.Set(
            sourceFilePath: ExternalLevelJsonImportSession.SourceFilePath,
            sourceJsonText: ExternalLevelJsonImportSession.SourceJsonText,
            formatId: workingProfile.formatId,
            formatDisplayName: workingProfile.displayName,
            profile: workingProfile);

        ExternalJsonProfileStorage.SaveProjectProfile(workingProfile);
        LevelProjectDirtyState.MarkDirty();
        EditorPopupService.ShowToast("Profile saved");
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
            source.discriminatorField = row.DiscriminatorField.value?.Trim() ?? string.Empty;
            source.discriminatorValue = row.DiscriminatorValueField.value?.Trim() ?? string.Empty;
            ApplySpriteChoiceToSource(source, row.SpriteField?.value);
            sources.Add(source);
        }

        workingProfile.SetObjectSources(sources);
    }

    static void ApplySpriteChoiceToSource(ExternalJsonObjectSourceProfile source, string key)
    {
        if (source == null)
            return;

        if (string.IsNullOrEmpty(key)
            || string.Equals(key, PlaceholderKey, StringComparison.Ordinal)
            || string.Equals(key, CustomSpriteKey, StringComparison.Ordinal))
        {
            source.spriteMode = ExternalJsonSpriteMode.Placeholder;
            source.spriteAssetId = string.Empty;
            return;
        }

        if (key.StartsWith(AssetKeyPrefix, StringComparison.Ordinal))
        {
            source.spriteMode = ExternalJsonSpriteMode.Custom;
            source.spriteAssetId = key.Substring(AssetKeyPrefix.Length);
        }
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
