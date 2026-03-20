using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ObjectButtonController : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public GameObject ObjectToSpawn;
    private GameObject spawnedObject, spawnedPreviewObject;
    public Canvas parentCanvas;
    private Sprite previewSprite;
    private bool previewActive => spawnedPreviewObject != null;
    private bool objectSpawned => spawnedObject != null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning($"No parent canvas found on {gameObject.name}");
        }

        previewSprite = ObjectToSpawn.GetComponent<SpriteRenderer>().sprite;
        if (previewSprite == null)
        {
            Debug.LogWarning($"No sprite was found on {ObjectToSpawn.name}. Please add a sprite to the object to be able to see a preview when dragging.");
        }
    }

    void SpawnObjectOnDrag(PointerEventData eventData)
    {
        if (spawnedPreviewObject != null)
        {
            Destroy(spawnedPreviewObject);
            spawnedPreviewObject = null;
        }

        Vector3 pos = Camera.main.ScreenToWorldPoint(eventData.position);
        pos.z = 0f;

        spawnedObject = Instantiate(ObjectToSpawn, pos, Quaternion.identity);
    }

    void SpawnSpritePreview(PointerEventData eventData)
    {
        if (previewSprite == null) return;

        Vector3 pos = Camera.main.ScreenToWorldPoint(eventData.position);
        pos.z = 0f;

        spawnedPreviewObject = new GameObject("Preview", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        spawnedPreviewObject.transform.SetParent(parentCanvas.gameObject.transform, false);
        spawnedPreviewObject.transform.SetAsLastSibling();

        spawnedPreviewObject.transform.position = pos;
        spawnedPreviewObject.transform.SetAsLastSibling();

        var image = spawnedPreviewObject.GetComponent<UnityEngine.UI.Image>();
        image.sprite = previewSprite;
        image.raycastTarget = false;

        RectTransform rect = spawnedPreviewObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(64, 64);
        rect.position = eventData.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        SpawnSpritePreview(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //cleanup preview and spawned object if they exist
        if (objectSpawned)
            spawnedObject = null;

        if (previewActive) //this check also makes sure that there is no objects spawned behind the menu.
        {
            Destroy(spawnedPreviewObject);
            spawnedPreviewObject = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = 0f; // Assuming a 2D game, set z to 0
        if (UIHelper.IsPointerOverUI() && previewActive)
        {
            spawnedPreviewObject.transform.position = eventData.position;
            return;
        }

        if (!objectSpawned)
            SpawnObjectOnDrag(eventData);

        spawnedObject.transform.position = mouseWorldPos;
    }
}
