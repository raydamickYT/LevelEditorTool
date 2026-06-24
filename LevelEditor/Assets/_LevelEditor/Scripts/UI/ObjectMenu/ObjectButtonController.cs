using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// For spawning:
/// this object will make a new game object when in the scene, and pass it's location through to an action after which the spawning will be finalized.
/// </summary>

public class ObjectButtonController : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public GameObject ObjectToSpawnPrefab;
    public string AssetID;
    public string DisplayName;

    private GameObject spawnedObject, spawnedPreviewObject;// actual game object; preview object containing a sprite
    public Canvas parentCanvas;
    public Sprite previewSprite;
    private bool previewExists => spawnedPreviewObject != null;
    private bool gameObjectExists => spawnedObject != null;
    private DragVisualiseState currentVisualiseState = DragVisualiseState.None;
    Vector3 spawnDragStartPosition;
    Bounds spawnDragStartBounds;
    bool hasSpawnDragStartBounds;
    [SerializeField] private float minSpriteSize = 0.5f;
    [SerializeField] private float maxSpriteSize = 4f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning($"No parent canvas found on {gameObject.name}");
        }

        if (ObjectToSpawnPrefab == null)
        {
            Debug.LogWarning($"Object to spawned is not assigned on {gameObject.name}");
            return;
        }

        if (previewSprite == null)
        {
            previewSprite = ObjectToSpawnPrefab.GetComponent<SpriteRenderer>().sprite;
            if (previewSprite == null)
                Debug.LogWarning($"No sprite was found on {ObjectToSpawnPrefab.name}. Please add a sprite to the object to be able to see a preview when dragging.");
        }

    }

    //Interface implementations for the drag and drop functionality of the buttons.
    public void OnBeginDrag(PointerEventData eventData)
    {
        SpawnSpritePreview(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //cleanup preview and remove spawned object reference if they exist
        if (gameObjectExists)
        {
            var spawnObjectAction = new SpawnObjectAction(spawnedObject, ObjectToSpawnPrefab, AssetID);
            spawnObjectAction.Execute();
            EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, spawnObjectAction);

            EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, spawnedObject, SelectionCommand.Select);

            spawnedObject = null;
        }

        if (previewExists) //this check also makes sure that there is no objects spawned behind the menu.
            RemoveSpawnedPreview();

        ResetSpawnDragSnapState();
        currentVisualiseState = DragVisualiseState.None;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //check if there's an object spawned and if the mouse is hovering over a preview. if so we'd like to switch to the preview
        if (UIHelper.IsPointerOverUI() && currentVisualiseState == DragVisualiseState.SpawnedObject)
        {
            if (previewExists)
            {
                EnableSpawnedPreviewIfActive(eventData);//if the preview is already spawned, toggle it on, 
                return;
            }

            SpawnSpritePreview(eventData); //if not spawn it. this way we can make sure that the preview is always active when hovering over the UI.
            return;
        }

        if (UIHelper.IsPointerOverUI() && currentVisualiseState == DragVisualiseState.Preview)
        {
            spawnedPreviewObject.transform.position = eventData.position;
            return;
        }

        if (currentVisualiseState != DragVisualiseState.SpawnedObject)
            SpawnObjectOnDrag(eventData);

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = 0f;
        if (GizmoInteractionHandler.Instance != null)
        {
            mouseWorldPos = GizmoInteractionHandler.Instance.SnapSpawnWorldPosition(
                mouseWorldPos,
                spawnedObject,
                spawnDragStartPosition,
                spawnDragStartBounds,
                hasSpawnDragStartBounds);
        }

        spawnedObject.transform.position = mouseWorldPos;
    }

    void ResetSpawnDragSnapState()
    {
        spawnDragStartPosition = default;
        spawnDragStartBounds = default;
        hasSpawnDragStartBounds = false;
    }

    //helper function to spawn the actual object.
    void SpawnObjectOnDrag(PointerEventData eventData)
    {
        if (currentVisualiseState == DragVisualiseState.Preview)
            DisableSpawnedPreviewIfActive();

        currentVisualiseState = DragVisualiseState.SpawnedObject;

        Vector3 pos = Camera.main.ScreenToWorldPoint(eventData.position);
        pos.z = 0f;

        spawnedObject = Instantiate(ObjectToSpawnPrefab, pos, Quaternion.identity);
        spawnedObject.SetActive(true);
        spawnedObject.hideFlags = HideFlags.None;
        if (!string.IsNullOrWhiteSpace(DisplayName))
            spawnedObject.name = DisplayName;

        SpriteRenderer spriteRenderer = spawnedObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = previewSprite;

        ClampSpawnedObjectSize(spriteRenderer);

        if (spawnedObject.TryGetComponent(out LevelObject levelObject))
            levelObject.ApplyCollisionState();

        spawnDragStartPosition = pos;
        hasSpawnDragStartBounds = GizmoInteractionHandler.TryGetColliderBoundsWorld(
            spawnedObject,
            out spawnDragStartBounds);
    }

    private void ClampSpawnedObjectSize(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);

        if (largestSide <= 0f)
            return;

        float targetSize = Mathf.Clamp(largestSide, minSpriteSize, maxSpriteSize);
        float scaleMultiplier = targetSize / largestSide;

        spriteRenderer.transform.localScale *= scaleMultiplier;
    }

    //helper function to remove the spawned object.
    void RemoveSpawnedObject()
    {
        if (currentVisualiseState == DragVisualiseState.SpawnedObject) //object spawned
        {
            Destroy(spawnedObject);
            spawnedObject = null;
            ResetSpawnDragSnapState();
        }
    }

    //helper function to spawn the preview object.
    void SpawnSpritePreview(PointerEventData eventData)
    {
        if (previewExists) return; //preview already exists
        if (currentVisualiseState == DragVisualiseState.SpawnedObject) //if we were in that state that likely means there's a spawned object.
            RemoveSpawnedObject(); //in the case that the user drags back to ui, remove the spawned gameobject to prevent it from being spawned behind the menu when dragging back out.

        currentVisualiseState = DragVisualiseState.Preview;

        Vector3 pos = Camera.main.ScreenToWorldPoint(eventData.position);
        pos.z = 0f;

        spawnedPreviewObject = new GameObject("Preview", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        spawnedPreviewObject.transform.SetParent(parentCanvas.gameObject.transform, false);
        spawnedPreviewObject.transform.SetAsLastSibling();

        var image = spawnedPreviewObject.GetComponent<UnityEngine.UI.Image>();
        image.sprite = previewSprite;
        image.raycastTarget = false;

        RectTransform rect = spawnedPreviewObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(64, 64);
        rect.position = eventData.position;
    }

    //helper function to remove the preview object.
    void RemoveSpawnedPreview()
    {
        if (!previewExists) return;

        Destroy(spawnedPreviewObject);
        spawnedPreviewObject = null;
    }
    //helper function to toggle between the preview and the spawned object when hovering over the UI.
    void DisableSpawnedPreviewIfActive()
    {
        if (!previewExists) return;

        if (spawnedPreviewObject.activeSelf)
            spawnedPreviewObject.SetActive(false);
        currentVisualiseState = DragVisualiseState.SpawnedObject;
    }
    //helper function to toggle between the preview and the spawned object when hovering over the UI
    void EnableSpawnedPreviewIfActive(PointerEventData eventData)
    {
        if (!previewExists) return;

        spawnedPreviewObject.transform.position = eventData.position;

        if (!spawnedPreviewObject.activeSelf)
            spawnedPreviewObject.SetActive(true);

        RemoveSpawnedObject();
        currentVisualiseState = DragVisualiseState.Preview;
    }

}

enum DragVisualiseState
{
    None,
    Preview,
    SpawnedObject
}