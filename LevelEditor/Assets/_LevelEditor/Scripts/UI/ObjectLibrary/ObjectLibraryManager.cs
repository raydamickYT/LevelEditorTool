using System;
using System.Collections.Generic;
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

    [Tooltip("When enabled, on Play the asset palette rebuilds from UserData/asset_registry.json (every registered sprite). Disable to keep the grid empty until import or level load repopulates it.")]
    [SerializeField]
    bool refreshEntireLibraryFromDiskOnStart = true;

    readonly HashSet<string> renderedAssetIds = new();

    VisualElement toolkitRoot;
    VisualElement toolkitLibraryRoot;
    VisualElement toolkitGrid;
    ToolkitImage dragPreview;
    VisualElement dragSource;
    string draggedAssetId;
    Sprite draggedSprite;
    GameObject draggedSpawnedObject;
    int draggedPointerId = InvalidPointerId;

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

        if (sprite == null && asset.AssetType == ImportedAssetTypes.Sprite)
            sprite = AssetRuntimeLoader.LoadSpriteByAssetID(asset.AssetID);

        // Do not use DefaultSprite (magenta) in the palette — skip broken paths so the grid stays readable.
        if (sprite == null)
            return;

        if (UsesToolkit)
        {
            AddToolkitEntry(asset, sprite);
            return;
        }

        if (ContentObject == null)
            return;

        GameObject obj = Instantiate(PreviewPrefab, ContentObject.transform);
        obj.SetActive(true);
        obj.hideFlags = HideFlags.None;
        obj.name = string.IsNullOrEmpty(asset.FileName) ? asset.AssetID : asset.FileName;

        if (obj.TryGetComponent(out UiImage image))
            image.sprite = sprite;

        if (obj.TryGetComponent(out ObjectButtonController controller))
        {
            controller.previewSprite = sprite;
            controller.AssetID = asset.AssetID;
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

        toolkitRoot = uiDocument.rootVisualElement;
        toolkitGrid = toolkitRoot?.Q<VisualElement>(gridElementName);
        toolkitLibraryRoot = toolkitRoot?.Q<VisualElement>(libraryRootElementName) ?? toolkitGrid;
    }

    void ClearLibraryContent()
    {
        renderedAssetIds.Clear();
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
        VisualElement tile = new();
        tile.name = string.IsNullOrEmpty(asset.FileName) ? asset.AssetID : asset.FileName;
        tile.AddToClassList("asset-tile");

        ToolkitImage image = new();
        image.AddToClassList("asset-thumbnail");
        image.scaleMode = ScaleMode.ScaleToFit;
        image.image = sprite.texture;
        tile.Add(image);

        Label label = new(string.IsNullOrEmpty(asset.FileName) ? asset.AssetID : asset.FileName);
        label.AddToClassList("asset-label");
        tile.Add(label);

        string assetId = asset.AssetID;
        tile.RegisterCallback<PointerDownEvent>(evt => BeginToolkitDrag(evt, tile, assetId, sprite));
        tile.RegisterCallback<PointerMoveEvent>(UpdateToolkitDrag);
        tile.RegisterCallback<PointerUpEvent>(EndToolkitDrag);
        tile.RegisterCallback<PointerCancelEvent>(CancelToolkitDrag);

        toolkitGrid.Add(tile);
        renderedAssetIds.Add(asset.AssetID);
    }

    void BeginToolkitDrag(PointerDownEvent evt, VisualElement source, string assetId, Sprite sprite)
    {
        if (evt.button != 0 || source == null || sprite == null)
            return;

        CancelToolkitDrag();

        dragSource = source;
        draggedAssetId = assetId;
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

        if (IsPointerInsideLibrary(evt.position))
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
    }

    void ShowDragPreview(Vector2 panelPosition)
    {
        if (dragPreview == null)
            return;

        dragPreview.style.display = DisplayStyle.Flex;
        dragPreview.style.left = panelPosition.x - 32f;
        dragPreview.style.top = panelPosition.y - 32f;
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

    void MoveOrCreateDraggedSpawnedObject(Vector2 panelPosition)
    {
        Vector3 worldPosition = PanelPositionToWorld(panelPosition);

        if (draggedSpawnedObject == null)
        {
            draggedSpawnedObject = Instantiate(GameObjectPrefab, worldPosition, Quaternion.identity);
            draggedSpawnedObject.SetActive(true);
            draggedSpawnedObject.hideFlags = HideFlags.None;
            ConfigureSpawnedSprite(draggedSpawnedObject, draggedSprite);
        }

        draggedSpawnedObject.transform.position = worldPosition;
    }

    Vector3 PanelPositionToWorld(Vector2 panelPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Vector3.zero;

        Vector2 screenPosition2D = PanelPositionToScreenPosition(panelPosition);
        Vector3 screenPosition = new(screenPosition2D.x, screenPosition2D.y, 0f);
        Vector3 worldPosition = cam.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    Vector2 PanelPositionToScreenPosition(Vector2 panelPosition)
    {
        Rect panelBounds = toolkitRoot != null
            ? toolkitRoot.worldBound
            : new Rect(0f, 0f, Screen.width, Screen.height);

        float width = Mathf.Max(1f, panelBounds.width);
        float height = Mathf.Max(1f, panelBounds.height);

        float normalizedX = (panelPosition.x - panelBounds.xMin) / width;
        float normalizedYFromTop = (panelPosition.y - panelBounds.yMin) / height;

        return new Vector2(
            normalizedX * Screen.width,
            Screen.height - (normalizedYFromTop * Screen.height));
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

        BoxCollider2D boxCollider = spawnedObject.GetComponent<BoxCollider2D>();
        if (boxCollider != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            boxCollider.size = spriteRenderer.sprite.bounds.size;
            boxCollider.offset = spriteRenderer.sprite.bounds.center;
        }
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
