using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectLibraryManager : MonoBehaviour
{
    public static ObjectLibraryManager Instance { get; private set; }

    public Sprite DefaultSprite; //used in case no sprite was found on extraction.
    public GameObject ContentObject;

    [Tooltip("When enabled, on Play the asset palette rebuilds from UserData/asset_registry.json (every registered sprite). Disable to keep the grid empty until import or level load repopulates it.")]
    [SerializeField]
    bool refreshEntireLibraryFromDiskOnStart = true;

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

        if (ContentObject == null)
        {
            ContentObject = GetComponentInChildren<GridLayoutGroup>().gameObject;
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
        if (ContentObject == null)
            return;

        for (int i = ContentObject.transform.childCount - 1; i >= 0; i--)
            Destroy(ContentObject.transform.GetChild(i).gameObject);

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
        if (asset == null || string.IsNullOrEmpty(asset.AssetID) || ContentObject == null)
            return;

        foreach (Transform t in ContentObject.transform)
        {
            if (t != null && t.TryGetComponent(out ObjectButtonController existing) && existing.AssetID == asset.AssetID)
                return;
        }

        Sprite sprite = null;
        if (asset is ImportedSpriteData spriteData && spriteData.Sprite != null)
            sprite = spriteData.Sprite;

        if (sprite == null && asset.AssetType == ImportedAssetTypes.Sprite)
            sprite = AssetRuntimeLoader.LoadSpriteByAssetID(asset.AssetID);

        // Do not use DefaultSprite (magenta) in the palette — skip broken paths so the grid stays readable.
        if (sprite == null)
            return;

        GameObject obj = Instantiate(PreviewPrefab, ContentObject.transform);
        obj.SetActive(true);
        obj.hideFlags = HideFlags.None;
        obj.name = string.IsNullOrEmpty(asset.FileName) ? asset.AssetID : asset.FileName;

        if (obj.TryGetComponent(out Image image))
            image.sprite = sprite;

        if (obj.TryGetComponent(out ObjectButtonController controller))
        {
            controller.previewSprite = sprite;
            controller.AssetID = asset.AssetID;
        }
    }

    //helper functions
    private GameObject createPreviewPrefabObject()
    {
        GameObject obj = new();
        obj.SetActive(false);
        obj.hideFlags = HideFlags.HideInHierarchy;

        obj.AddComponent<RectTransform>();
        obj.AddComponent<CanvasRenderer>();

        Image image = obj.AddComponent<Image>();
        image.sprite = DefaultSprite;

        var btn = obj.AddComponent<Button>();
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
