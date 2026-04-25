using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectButtonController : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public GameObject ObjectToSpawnPrefab;
    private GameObject spawnedObject, spawnedPreviewObject;
    public Canvas parentCanvas;
    private Sprite previewSprite;
    private bool previewExists => spawnedPreviewObject != null;
    private bool objectExists => spawnedObject != null;
    private DragVisualiseState currentVisualiseState = DragVisualiseState.None;

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

        previewSprite = ObjectToSpawnPrefab.GetComponent<SpriteRenderer>().sprite;
        if (previewSprite == null)
        {
            Debug.LogWarning($"No sprite was found on {ObjectToSpawnPrefab.name}. Please add a sprite to the object to be able to see a preview when dragging.");
        }

    }

    void OnDestroy()
    {
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

        LevelObjectsRoot.Instance.AddLevelObject(spawnedObject);

        var lvlObj = spawnedObject.GetComponent<LevelObject>();
        if (lvlObj != null)
        {
            lvlObj.PrefabReference = ObjectToSpawnPrefab;
            ObjectRegistry.OnObjectCreated(lvlObj); //register it
        }
    }

    //helper function to remove the spawned object.
    void RemoveSpawnedObject()
    {
        if (currentVisualiseState == DragVisualiseState.SpawnedObject) //object spawned
        {
            Destroy(spawnedObject);
            spawnedObject = null;
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

    //Interface implementations for the drag and drop functionality of the buttons.
    public void OnBeginDrag(PointerEventData eventData)
    {
        SpawnSpritePreview(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //cleanup preview and remove spawned object reference if they exist
        if (objectExists)
        {
            EventManager.Instance.TriggerDelegate(SelectionEvents.OnTrySelection, spawnedObject);
            spawnedObject = null;
        }

        if (previewExists) //this check also makes sure that there is no objects spawned behind the menu.
            RemoveSpawnedPreview();

        currentVisualiseState = DragVisualiseState.None;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //get the mouse position in world space, this'll be used to move the spawned objects around with the mouse
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = 0f; // Assuming a 2D game, set z to 0

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

        spawnedObject.transform.position = mouseWorldPos;
    }
}

enum DragVisualiseState
{
    None,
    Preview,
    SpawnedObject
}