using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ObjectLibraryManager : MonoBehaviour
{
    public Sprite DefaultSprite; //used in case no sprite was found on extraction.
    public GameObject ContentObject;

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

    void Awake()
    {
        DefaultSprite = SetupDefaultSprite();

        if (ContentObject == null)
        {
            ContentObject = GetComponentInChildren<GridLayoutGroup>().gameObject;
            if (ContentObject == null)
                Debug.LogWarning("ContentObject not assigned: " + name);
        }

        EventManager.Instance.AddDelegateListener(ObjectLibraryManagerEvents.UpdateObjectLibrary, (Action<IEnumerable<ImportedAssetData>>)updateContentObject);
    }

    void updateContentObject(IEnumerable<ImportedAssetData> data)
    {
        foreach (ImportedAssetData asset in data)
        {
            if (asset is ImportedSpriteData item) //for now we need this, because there's a sprite needed to setup the preview
            {
                GameObject obj = Instantiate(PreviewPrefab, ContentObject.transform);
                obj.SetActive(true);
                obj.hideFlags = HideFlags.None;
                obj.name = item.FileName;

                if (obj.TryGetComponent(out Image image))
                    image.sprite = item.Sprite != null ? item.Sprite : DefaultSprite;

                if (obj.TryGetComponent(out ObjectButtonController controller))
                    controller.previewSprite = item.Sprite != null ? item.Sprite : DefaultSprite;
            }
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

        obj.AddComponent<BoxCollider2D>(); //TODO: might want to adjust the collider size according to the sprite size, but that might be something for ObjectButtonController
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
}

public static class ObjectLibraryManagerEvents
{
    public const string UpdateObjectLibrary = "UpdateObjectLibrary";
}
