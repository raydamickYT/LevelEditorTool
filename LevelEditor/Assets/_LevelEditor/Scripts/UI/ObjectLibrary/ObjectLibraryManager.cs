using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using GridLayoutGroup = UnityEngine.UI.GridLayoutGroup;
using UiButton = UnityEngine.UI.Button;
using UiImage = UnityEngine.UI.Image;
using ToolkitImage = UnityEngine.UIElements.Image;

public class ObjectLibraryManager : MonoBehaviour
{
    const int InvalidPointerId = -1;

    public static ObjectLibraryManager Instance { get; private set; }

    public Sprite DefaultSprite; //used in case no sprite was found on extraction.
    public GameObject ContentObject;

    [Header("UI Toolkit")]
    [SerializeField] UIDocument uiDocument;
    [SerializeField] string gridElementName = "asset-grid";
    [SerializeField] string libraryRootElementName = "object-library-root";
    [SerializeField] string resizeHandleElementName = "object-library-resize-handle";
    [SerializeField] string collapseButtonElementName = "object-library-collapse-button";
    [SerializeField] int dragPreviewSortingOrder = 32767;
    [SerializeField] float minLibraryWidth = 260f;
    [SerializeField] float maxLibraryWidth = 900f;
    [SerializeField] float expandedLibraryTop = 300f;
    [SerializeField] float collapsedLibraryHeight = 30f;

    [Tooltip("When enabled, on Play the asset palette rebuilds from UserData/asset_registry.json (every registered sprite). Disable to keep the grid empty until import or level load repopulates it.")]
    [SerializeField]
    bool refreshEntireLibraryFromDiskOnStart = true;

    readonly HashSet<string> renderedAssetIds = new();
    readonly Dictionary<string, VisualElement> toolkitCategoryGrids = new();

    VisualElement toolkitRoot;
    VisualElement toolkitLibraryRoot;
    VisualElement toolkitGrid;
    VisualElement resizeHandle;
    Button collapseButton;
    ToolkitImage dragPreview;
    VisualElement dragSource;
    string draggedAssetId;
    string draggedAssetDisplayName;
    Sprite draggedSprite;
    GameObject draggedSpawnedObject;
    int draggedPointerId = InvalidPointerId;
    int resizePointerId = InvalidPointerId;
    bool isResizingLibrary;
    bool isLibraryCollapsed;

    private GameObject gameObjectPrefab;
    private GameObject GameObjectPrefab
    {
        get
        {
            if (gameObjectPrefab == null)
            {
                gameObjectPrefab = createPrefabObject();
            }
            return gameObjectPrefab;
        }
    }
    private GameObject previewPrefab;
    private GameObject PreviewPrefab
    {
        get
        {
            if (previewPrefab == null)
            {
                previewPrefab = createPreviewPrefabObject();
            }
            return previewPrefab;
        }
    }

    /// <summary>Same template used for library spawns (sprite + collider + <see cref="LevelObject"/>).</summary>
    public GameObject SpawnPrefabTemplate => GameObjectPrefab;

    void Awake()
    {
        Instance = this;

        DefaultSprite = SetupDefaultSprite();
        SetupToolkitReferences();

        if (!UsesToolkit && ContentObject == null)
        {
            GridLayoutGroup gridLayoutGroup = GetComponentInChildren<GridLayoutGroup>();
            ContentObject = gridLayoutGroup != null ? gridLayoutGroup.gameObject : null;
            if (ContentObject == null)
                Debug.LogWarning("ContentObject not assigned: " + name);
        }

        EventManager.Instance.AddDelegateListener(ObjectLibraryManagerEvents.UpdateObjectLibrary, (Action<IEnumerable<ImportedAssetMetaData>>)updateContentObject);
    }

    void Start()
    {
        if (refreshEntireLibraryFromDiskOnStart)
            RebuildLibraryFromAssetStorage();
    }

    /// <summary>Clears the library grid and repopulates from <see cref="AssetStorageService"/> (e.g. after level import merges bundled assets).</summary>
    public void RebuildLibraryFromAssetStorage()
    {
        SetupToolkitReferences();
        ClearLibraryContent();

        if (!UsesToolkit && ContentObject == null)
            return;

        foreach (ImportedAssetMetaData asset in AssetStorageService.GetAllCachedImportedAssets())
            TryAddLibraryEntry(asset);
    }

    void updateContentObject(IEnumerable<ImportedAssetMetaData> data)
    {
        if (data == null)
            return;

        foreach (ImportedAssetMetaData asset in data)
            TryAddLibraryEntry(asset);
    }

    void TryAddLibraryEntry(ImportedAssetMetaData asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.AssetID))
            return;

        if (renderedAssetIds.Contains(asset.AssetID))
            return;

        Sprite sprite = null;
        if (asset is ImportedSpriteData spriteData && spriteData.Sprite != null)
            sprite = spriteData.Sprite;

        if (sprite == null
            && (asset.AssetType == ImportedAssetTypes.Sprite || asset.AssetType == ImportedAssetTypes.Prefab))
        {
            sprite = AssetRuntimeLoader.LoadSpriteByAssetID(asset.AssetID);
        }

        bool isPrefabReference = string.Equals(asset.AssetType, ImportedAssetTypes.Prefab, StringComparison.OrdinalIgnoreCase);

        // Do not use DefaultSprite (magenta) for broken sprites — skip broken paths so the grid stays readable.
        if (sprite == null && !isPrefabReference)
            return;

        if (UsesToolkit)
        {
            AddToolkitEntry(asset, sprite);
            return;
        }

        if (sprite == null)
            return;

        string displayName = GetAssetDisplayName(asset);
        if (ContentObject == null)
            return;

        GameObject obj = Instantiate(PreviewPrefab, ContentObject.transform);
        obj.SetActive(true);
        obj.hideFlags = HideFlags.None;
        obj.name = displayName;

        if (obj.TryGetComponent(out UiImage image))
            image.sprite = sprite;

        if (obj.TryGetComponent(out ObjectButtonController controller))
        {
            controller.previewSprite = sprite;
            controller.AssetID = asset.AssetID;
            controller.DisplayName = displayName;
        }

        renderedAssetIds.Add(asset.AssetID);
    }

    bool UsesToolkit => toolkitGrid != null;

    void SetupToolkitReferences()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            return;

        SetDocumentSortingOrder(uiDocument, dragPreviewSortingOrder);

        toolkitRoot = uiDocument.rootVisualElement;
        toolkitGrid = toolkitRoot?.Q<VisualElement>(gridElementName);
        toolkitLibraryRoot = toolkitRoot?.Q<VisualElement>(libraryRootElementName) ?? toolkitGrid;
        SetupResizeHandle();
        SetupCollapseButton();
    }

    static void SetDocumentSortingOrder(UIDocument document, int sortingOrder)
    {
        if (document == null)
            return;

        System.Reflection.PropertyInfo property = typeof(UIDocument).GetProperty("sortingOrder");
        if (property == null || !property.CanWrite)
            return;

        property.SetValue(document, sortingOrder);
    }

    void SetupResizeHandle()
    {
        VisualElement foundHandle = toolkitRoot?.Q<VisualElement>(resizeHandleElementName);
        if (foundHandle == resizeHandle)
            return;

        if (resizeHandle != null)
        {
            resizeHandle.UnregisterCallback<PointerDownEvent>(BeginResizeLibrary);
            resizeHandle.UnregisterCallback<PointerMoveEvent>(ResizeLibrary);
            resizeHandle.UnregisterCallback<PointerUpEvent>(EndResizeLibrary);
            resizeHandle.UnregisterCallback<PointerCancelEvent>(CancelResizeLibrary);
        }

        resizeHandle = foundHandle;
        if (resizeHandle == null)
            return;

        resizeHandle.RegisterCallback<PointerDownEvent>(BeginResizeLibrary);
        resizeHandle.RegisterCallback<PointerMoveEvent>(ResizeLibrary);
        resizeHandle.RegisterCallback<PointerUpEvent>(EndResizeLibrary);
        resizeHandle.RegisterCallback<PointerCancelEvent>(CancelResizeLibrary);
    }

    void SetupCollapseButton()
    {
        Button foundButton = toolkitRoot?.Q<Button>(collapseButtonElementName);
        if (foundButton == collapseButton)
            return;

        if (collapseButton != null)
            collapseButton.clicked -= ToggleLibraryCollapsed;

        collapseButton = foundButton;
        if (collapseButton == null)
            return;

        collapseButton.clicked += ToggleLibraryCollapsed;
        ApplyLibraryCollapsedState();
    }

    void ToggleLibraryCollapsed()
    {
        isLibraryCollapsed = !isLibraryCollapsed;
        ApplyLibraryCollapsedState();
    }

    void ApplyLibraryCollapsedState()
    {
        if (toolkitLibraryRoot == null)
            return;

        if (isLibraryCollapsed)
        {
            CancelToolkitDrag();
            FinishResizeLibrary();
            toolkitLibraryRoot.AddToClassList("object-library-collapsed");
            toolkitLibraryRoot.style.top = StyleKeyword.Auto;
            toolkitLibraryRoot.style.bottom = 0f;
            toolkitLibraryRoot.style.height = collapsedLibraryHeight;
        }
        else
        {
            toolkitLibraryRoot.RemoveFromClassList("object-library-collapsed");
            toolkitLibraryRoot.style.top = expandedLibraryTop;
            toolkitLibraryRoot.style.bottom = 0f;
            toolkitLibraryRoot.style.height = StyleKeyword.Auto;
        }

        if (collapseButton != null)
            collapseButton.text = isLibraryCollapsed ? "Show" : "Hide";
    }

    void BeginResizeLibrary(PointerDownEvent evt)
    {
        if (evt.button != 0 || isLibraryCollapsed || toolkitLibraryRoot == null || toolkitRoot == null)
            return;

        isResizingLibrary = true;
        resizePointerId = evt.pointerId;
        resizeHandle.CapturePointer(resizePointerId);
        ResizeLibraryToPointer(evt.position);
        evt.StopPropagation();
    }

    void ResizeLibrary(PointerMoveEvent evt)
    {
        if (!isResizingLibrary || evt.pointerId != resizePointerId)
            return;

        ResizeLibraryToPointer(evt.position);
        evt.StopPropagation();
    }

    void EndResizeLibrary(PointerUpEvent evt)
    {
        if (evt.pointerId != resizePointerId)
            return;

        FinishResizeLibrary();
        evt.StopPropagation();
    }

    void CancelResizeLibrary(PointerCancelEvent _)
    {
        FinishResizeLibrary();
    }

    void FinishResizeLibrary()
    {
        if (resizeHandle != null && resizePointerId != InvalidPointerId)
            resizeHandle.ReleasePointer(resizePointerId);

        isResizingLibrary = false;
        resizePointerId = InvalidPointerId;
    }

    void ResizeLibraryToPointer(Vector2 panelPosition)
    {
        Rect panelBounds = toolkitRoot.worldBound;
        float panelRight = panelBounds.xMax;
        float desiredWidth = panelRight - panelPosition.x;
        float maxWidth = Mathf.Min(maxLibraryWidth, Mathf.Max(minLibraryWidth, panelBounds.width));

        toolkitLibraryRoot.style.width = Mathf.Clamp(desiredWidth, minLibraryWidth, maxWidth);
    }

    void ClearLibraryContent()
    {
        renderedAssetIds.Clear();
        toolkitCategoryGrids.Clear();
        CancelToolkitDrag();

        if (UsesToolkit)
        {
            toolkitGrid.Clear();
            return;
        }

        if (ContentObject == null)
            return;

        for (int i = ContentObject.transform.childCount - 1; i >= 0; i--)
            Destroy(ContentObject.transform.GetChild(i).gameObject);
    }

    void AddToolkitEntry(ImportedAssetMetaData asset, Sprite sprite)
    {
        bool canDragIntoLevel = sprite != null
            && (string.Equals(asset.AssetType, ImportedAssetTypes.Sprite, StringComparison.OrdinalIgnoreCase)
                || string.Equals(asset.AssetType, ImportedAssetTypes.Prefab, StringComparison.OrdinalIgnoreCase));

        string displayName = GetAssetDisplayName(asset);
        VisualElement tile = new();
        tile.name = displayName;
        tile.AddToClassList("asset-tile");
        if (!canDragIntoLevel)
            tile.AddToClassList("asset-tile-disabled");

        if (sprite != null)
        {
            ToolkitImage image = new();
            image.AddToClassList("asset-thumbnail");
            image.scaleMode = ScaleMode.ScaleToFit;
            image.image = sprite.texture;
            tile.Add(image);
        }
        else
        {
            Label placeholder = new("P");
            placeholder.AddToClassList("asset-prefab-placeholder");
            tile.Add(placeholder);
        }

        Label label = new(displayName);
        label.AddToClassList("asset-label");
        tile.Add(label);

        if (canDragIntoLevel)
        {
            string assetId = asset.AssetID;
            tile.RegisterCallback<PointerDownEvent>(evt => BeginToolkitDrag(evt, tile, assetId, displayName, sprite));
            tile.RegisterCallback<PointerMoveEvent>(UpdateToolkitDrag);
            tile.RegisterCallback<PointerUpEvent>(EndToolkitDrag);
            tile.RegisterCallback<PointerCancelEvent>(CancelToolkitDrag);
        }

        VisualElement categoryGrid = GetOrCreateToolkitFolderGrid(asset.AssetType, asset.FolderPath);
        categoryGrid.Add(tile);
        renderedAssetIds.Add(asset.AssetID);
    }

    VisualElement GetOrCreateToolkitFolderGrid(string assetType, string folderPath)
    {
        string categoryKey = string.IsNullOrWhiteSpace(assetType) ? "Unknown" : assetType.Trim();
        string normalizedFolder = NormalizeFolderPath(folderPath);
        string folderKey = $"{categoryKey}|{normalizedFolder}";

        if (toolkitCategoryGrids.TryGetValue(folderKey, out VisualElement existingGrid))
            return existingGrid;

        VisualElement categoryContainer = GetOrCreateToolkitCategoryContainer(categoryKey);
        if (string.IsNullOrEmpty(normalizedFolder))
            return categoryContainer;

        Foldout folder = new()
        {
            text = normalizedFolder,
            value = false
        };
        folder.AddToClassList("asset-subfolder");

        VisualElement folderGrid = new();
        folderGrid.AddToClassList("asset-folder-grid");
        folder.contentContainer.Add(folderGrid);
        folder.contentContainer.AddToClassList("asset-folder-content");

        categoryContainer.Add(folder);
        toolkitCategoryGrids.Add(folderKey, folderGrid);
        return folderGrid;
    }

    VisualElement GetOrCreateToolkitCategoryContainer(string categoryKey)
    {
        string categoryGridKey = $"{categoryKey}|";
        if (toolkitCategoryGrids.TryGetValue(categoryGridKey, out VisualElement existingContainer))
            return existingContainer;

        Foldout folder = new()
        {
            text = GetCategoryDisplayName(categoryKey),
            value = false
        };
        folder.AddToClassList("asset-folder");

        VisualElement categoryGrid = new();
        categoryGrid.AddToClassList("asset-folder-grid");
        folder.contentContainer.Add(categoryGrid);
        folder.contentContainer.AddToClassList("asset-folder-content");

        toolkitGrid.Add(folder);
        toolkitCategoryGrids.Add(categoryGridKey, categoryGrid);
        return categoryGrid;
    }

    static string NormalizeFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return string.Empty;

        return folderPath.Replace('\\', '/').Trim('/');
    }

    static string GetCategoryDisplayName(string assetType)
    {
        if (string.IsNullOrWhiteSpace(assetType))
            return "Unknown";

        return assetType.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            ? assetType
            : assetType + "s";
    }

    static string GetAssetDisplayName(ImportedAssetMetaData asset)
    {
        if (asset == null)
            return "Asset";

        string fileName = string.IsNullOrWhiteSpace(asset.FileName)
            ? asset.AssetRelativePath
            : asset.FileName;

        if (string.IsNullOrWhiteSpace(fileName))
            return string.IsNullOrWhiteSpace(asset.AssetID) ? "Asset" : asset.AssetID;

        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(nameWithoutExtension)
            ? fileName
            : nameWithoutExtension;
    }

    void BeginToolkitDrag(PointerDownEvent evt, VisualElement source, string assetId, string displayName, Sprite sprite)
    {
        if (evt.button != 0 || source == null || sprite == null)
            return;

        CancelToolkitDrag();

        dragSource = source;
        draggedAssetId = assetId;
        draggedAssetDisplayName = displayName;
        draggedSprite = sprite;
        draggedPointerId = evt.pointerId;
        dragSource.CapturePointer(draggedPointerId);

        CreateDragPreview(sprite);
        ShowDragPreview(evt.position);
        evt.StopPropagation();
    }

    void UpdateToolkitDrag(PointerMoveEvent evt)
    {
        if (evt.pointerId != draggedPointerId || draggedSprite == null)
            return;

        if (IsPointerInsideLibrary(evt.position) || IsPointerOverOtherUIDocument(evt.position))
        {
            ShowDragPreview(evt.position);
            RemoveDraggedSpawnedObject();
            evt.StopPropagation();
            return;
        }

        HideDragPreview();
        MoveOrCreateDraggedSpawnedObject(evt.position);
        evt.StopPropagation();
    }

    void EndToolkitDrag(PointerUpEvent evt)
    {
        if (evt.pointerId != draggedPointerId)
            return;

        if (draggedSpawnedObject != null)
        {
            var spawnObjectAction = new SpawnObjectAction(draggedSpawnedObject, GameObjectPrefab, draggedAssetId);
            spawnObjectAction.Execute();
            EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, spawnObjectAction);

            EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, draggedSpawnedObject, SelectionCommand.Select);
            draggedSpawnedObject = null;
        }

        CancelToolkitDrag();
        evt.StopPropagation();
    }

    void CancelToolkitDrag(PointerCancelEvent _)
    {
        CancelToolkitDrag();
    }

    void CancelToolkitDrag()
    {
        RemoveDraggedSpawnedObject();

        if (dragSource != null && draggedPointerId != InvalidPointerId)
            dragSource.ReleasePointer(draggedPointerId);

        dragPreview?.RemoveFromHierarchy();
        dragPreview = null;
        dragSource = null;
        draggedAssetId = null;
        draggedAssetDisplayName = null;
        draggedSprite = null;
        draggedPointerId = InvalidPointerId;
    }

    void CreateDragPreview(Sprite sprite)
    {
        if (toolkitRoot == null || sprite == null)
            return;

        dragPreview = new ToolkitImage();
        dragPreview.AddToClassList("asset-drag-preview");
        dragPreview.scaleMode = ScaleMode.ScaleToFit;
        dragPreview.image = sprite.texture;
        dragPreview.pickingMode = PickingMode.Ignore;
        toolkitRoot.Add(dragPreview);
        dragPreview.BringToFront();
    }

    void ShowDragPreview(Vector2 panelPosition)
    {
        if (dragPreview == null)
            return;

        dragPreview.style.display = DisplayStyle.Flex;
        dragPreview.style.left = panelPosition.x - 32f;
        dragPreview.style.top = panelPosition.y - 32f;
        dragPreview.BringToFront();
    }

    void HideDragPreview()
    {
        if (dragPreview != null)
            dragPreview.style.display = DisplayStyle.None;
    }

    bool IsPointerInsideLibrary(Vector2 panelPosition)
    {
        if (toolkitLibraryRoot == null)
            return false;

        // Pointer capture keeps events flowing to the tile even after leaving it.
        // Pick the visual under the pointer so empty library space still counts as UI
        // and the spawned world object never appears behind the asset panel.
        VisualElement picked = toolkitRoot?.panel?.Pick(panelPosition);
        while (picked != null)
        {
            if (picked == toolkitLibraryRoot)
                return true;

            picked = picked.parent;
        }

        return toolkitLibraryRoot.worldBound.Contains(panelPosition);
    }

    bool IsPointerOverOtherUIDocument(Vector2 panelPosition)
    {
        foreach (UIDocument document in FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (document == null || document == uiDocument)
                continue;

            VisualElement root = document.rootVisualElement;
            if (root == null || root.panel == null)
                continue;

            Vector2 otherPanelPosition = ConvertPanelPosition(panelPosition, toolkitRoot, root);
            VisualElement picked = root.panel.Pick(otherPanelPosition);
            if (picked == null || picked == root)
                continue;

            return true;
        }

        return false;
    }

    static Vector2 ConvertPanelPosition(Vector2 sourcePanelPosition, VisualElement sourceRoot, VisualElement targetRoot)
    {
        Vector2 screenPosition = PanelPositionToScreenPosition(sourcePanelPosition, sourceRoot);
        return ScreenPositionToPanelPosition(screenPosition, targetRoot);
    }

    static Vector2 PanelPositionToScreenPosition(Vector2 panelPosition, VisualElement root)
    {
        Rect panelBounds = root != null
            ? root.worldBound
            : new Rect(0f, 0f, Screen.width, Screen.height);

        float width = Mathf.Max(1f, panelBounds.width);
        float height = Mathf.Max(1f, panelBounds.height);

        float normalizedX = (panelPosition.x - panelBounds.xMin) / width;
        float normalizedYFromTop = (panelPosition.y - panelBounds.yMin) / height;

        return new Vector2(
            normalizedX * Screen.width,
            Screen.height - (normalizedYFromTop * Screen.height));
    }

    static Vector2 ScreenPositionToPanelPosition(Vector2 screenPosition, VisualElement root)
    {
        Rect panelBounds = root != null
            ? root.worldBound
            : new Rect(0f, 0f, Screen.width, Screen.height);

        float width = Mathf.Max(1f, panelBounds.width);
        float height = Mathf.Max(1f, panelBounds.height);

        return new Vector2(
            panelBounds.xMin + (screenPosition.x / Mathf.Max(1f, Screen.width)) * width,
            panelBounds.yMin + ((Screen.height - screenPosition.y) / Mathf.Max(1f, Screen.height)) * height);
    }

    void MoveOrCreateDraggedSpawnedObject(Vector2 panelPosition)
    {
        Vector3 worldPosition = PanelPositionToWorld(panelPosition);

        if (draggedSpawnedObject == null)
        {
            draggedSpawnedObject = Instantiate(GameObjectPrefab, worldPosition, Quaternion.identity);
            draggedSpawnedObject.SetActive(true);
            draggedSpawnedObject.hideFlags = HideFlags.None;
            if (!string.IsNullOrWhiteSpace(draggedAssetDisplayName))
                draggedSpawnedObject.name = draggedAssetDisplayName;
            ConfigureSpawnedSprite(draggedSpawnedObject, draggedSprite);
        }

        draggedSpawnedObject.transform.position = worldPosition;
    }

    Vector3 PanelPositionToWorld(Vector2 panelPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Vector3.zero;

        Vector2 screenPosition2D = PanelPositionToScreenPosition(panelPosition, toolkitRoot);
        Vector3 screenPosition = new(screenPosition2D.x, screenPosition2D.y, 0f);
        Vector3 worldPosition = cam.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    void ConfigureSpawnedSprite(GameObject spawnedObject, Sprite sprite)
    {
        if (spawnedObject == null || sprite == null)
            return;

        SpriteRenderer spriteRenderer = spawnedObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            ClampSpawnedObjectSize(spriteRenderer);
        }

        if (spawnedObject.TryGetComponent(out LevelObject levelObject))
            levelObject.ApplyCollisionState();
    }

    void RemoveDraggedSpawnedObject()
    {
        if (draggedSpawnedObject == null)
            return;

        Destroy(draggedSpawnedObject);
        draggedSpawnedObject = null;
    }

    void ClampSpawnedObjectSize(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        if (largestSide <= 0f)
            return;

        float targetSize = Mathf.Clamp(largestSide, 0.5f, 4f);
        float scaleMultiplier = targetSize / largestSide;
        spriteRenderer.transform.localScale *= scaleMultiplier;
    }

    //helper functions
    private GameObject createPreviewPrefabObject()
    {
        GameObject obj = new();
        obj.SetActive(false);
        obj.hideFlags = HideFlags.HideInHierarchy;

        obj.AddComponent<RectTransform>();
        obj.AddComponent<CanvasRenderer>();

        UiImage image = obj.AddComponent<UiImage>();
        image.sprite = DefaultSprite;

        var btn = obj.AddComponent<UiButton>();
        btn.targetGraphic = image;

        var controller = obj.AddComponent<ObjectButtonController>();
        controller.ObjectToSpawnPrefab = GameObjectPrefab;

        obj.layer = LayerMask.NameToLayer("UI");

        return obj;
    }

    private GameObject createPrefabObject()
    {
        GameObject obj = new();
        obj.SetActive(false);
        obj.hideFlags = HideFlags.HideInHierarchy;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = DefaultSprite; //no need to fully assign this since the ObjectButtonController also manages this

        obj.AddComponent<BoxCollider2D>(); 
        obj.AddComponent<SelectableObject>();
        obj.AddComponent<LevelObject>();

        obj.layer = LayerMask.NameToLayer("Selectable");

        return obj;
    }
    private Sprite SetupDefaultSprite()
    {
        Texture2D texture = new Texture2D(2, 2);

        Color missingTextureColor = Color.magenta;

        Color[] pixels =
        {
            missingTextureColor, missingTextureColor,
            missingTextureColor, missingTextureColor
        };

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return sprite;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

public static class ObjectLibraryManagerEvents
{
    public const string UpdateObjectLibrary = "UpdateObjectLibrary";
}
