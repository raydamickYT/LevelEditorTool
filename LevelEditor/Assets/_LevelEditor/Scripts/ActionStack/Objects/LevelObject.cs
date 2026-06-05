using UnityEngine;

public class LevelObject : MonoBehaviour
{
    public int ObjectID;
    public string AssetID;
    public bool HasCollision;
    public GameObject PrefabReference;
    public HierarchyObjectItem hierarchyObjectItem;
    public Sprite sprite1;

    public LevelObjectGroup levelObjectGroup;
    public virtual bool HasParent => levelObjectGroup != null ? true : false;
    public virtual bool IsGroup => false;

    public class Memento
    {
        public Transform parent;
        public GameObject PrefabReference;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public int ObjectID;
        public string AssetID;
        public Sprite Sprite;
        public LevelObjectGroup LevelObjectGroup;
        public string ObjectName;
        public bool HasCollision;
        public int SortingOrder;
        public bool HasSortingOrder;
        public virtual bool HasParent => LevelObjectGroup != null ? true : false;

        public Memento(Transform t, GameObject obj, int id, Sprite sprite, string assetID, LevelObjectGroup levelObjectGroup)
        {
            Position = t.position;
            Rotation = t.rotation;
            Scale = t.localScale;
            PrefabReference = obj;
            parent = t.transform.parent;
            ObjectID = id;
            Sprite = sprite;
            AssetID = assetID;
            LevelObjectGroup = levelObjectGroup;
            ObjectName = t != null ? t.gameObject.name : string.Empty;
            if (t != null && t.TryGetComponent(out LevelObject levelObject))
                HasCollision = levelObject.HasCollision;
            if (t != null && t.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                SortingOrder = spriteRenderer.sortingOrder;
                HasSortingOrder = true;
            }
        }

        /// <summary>For level load / tooling: build a memento without a live Transform.</summary>
        public Memento(
            Vector3 position,
            Quaternion rotation,
            Vector3 localScale,
            GameObject prefabReference,
            string assetID,
            Sprite sprite,
            LevelObjectGroup levelObjectGroup = null,
            string objectName = null,
            bool hasCollision = false)
        {
            Position = position;
            Rotation = rotation;
            Scale = localScale;
            PrefabReference = prefabReference;
            parent = null;
            ObjectID = 0;
            Sprite = sprite;
            AssetID = assetID ?? string.Empty;
            LevelObjectGroup = levelObjectGroup;
            ObjectName = objectName ?? string.Empty;
            HasCollision = hasCollision;
        }
    }
    void OnEnable()
    {
        sprite1 = getSprite();
    }
    void OnDisable()
    {
        if (HasParent)
            levelObjectGroup.RemoveChild(this);
    }

    //NOTE: this is called at the end of a frame. So there's a slight chance this could lead to problems. If so, here's your reminder.
    public void OnDestroy()
    {
        ObjectRegistry.DeregisterObject(this);
    }

    // has to be called from the action classes
    public virtual Memento Save()
    {
        sprite1 = getSprite();

        if (sprite1 == null)
            Debug.LogWarning("Sprite could not be found");

        if (string.IsNullOrEmpty(AssetID))
            Debug.LogWarning("No asset ID assigned to this object");
        // else
        // Debug.Log("AssetID: " + AssetID);

        return new Memento(transform, PrefabReference, ObjectID, sprite1, AssetID, levelObjectGroup);
    }

    private Sprite getSprite()
    {
        if (transform.TryGetComponent(out SpriteRenderer renderer))
            return renderer.sprite;

        return null;
    }

    public virtual void Restore(Memento m)
    {
        if (m == null)
            return;

        transform.position = m.Position;
        transform.rotation = m.Rotation;
        transform.localScale = m.Scale;

        Sprite spriteToRestore = m.Sprite;
        if (spriteToRestore == null && !string.IsNullOrEmpty(m.AssetID))
            spriteToRestore = AssetRuntimeLoader.LoadSpriteByAssetID(m.AssetID);

        if (spriteToRestore != null)
        {
            if (transform.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.sprite = spriteToRestore;
                sprite1 = spriteToRestore;
            }
        }

        if (!string.IsNullOrEmpty(m.AssetID))
            AssetID = m.AssetID;

        if (!string.IsNullOrEmpty(m.ObjectName))
            gameObject.name = m.ObjectName;

        HasCollision = m.HasCollision;
        ApplyCollisionState();
        ApplySortingOrder(m);
    }

    public void ApplySortingOrder(Memento m)
    {
        if (m == null || !m.HasSortingOrder)
            return;

        if (TryGetComponent(out SpriteRenderer spriteRenderer))
            spriteRenderer.sortingOrder = m.SortingOrder;
    }

    public void ApplySortingOrder(int sortingOrder)
    {
        if (TryGetComponent(out SpriteRenderer spriteRenderer))
            spriteRenderer.sortingOrder = sortingOrder;
    }

    public void ApplyCollisionState()
    {
        if (!TryGetComponent(out SpriteRenderer spriteRenderer) || spriteRenderer.sprite == null)
            return;

        if (!TryGetComponent(out BoxCollider2D boxCollider))
            boxCollider = gameObject.AddComponent<BoxCollider2D>();

        Bounds bounds = spriteRenderer.sprite.bounds;
        boxCollider.size = bounds.size;
        boxCollider.offset = bounds.center;
        // Editor selection uses Physics2D overlap on this collider. HasCollision only controls level export.
        boxCollider.enabled = true;
        EnsurePickColliderOutline();
    }

    void EnsurePickColliderOutline()
    {
        PickColliderOutline outline = GetComponent<PickColliderOutline>();
        if (outline == null)
            outline = gameObject.AddComponent<PickColliderOutline>();

        outline.Refresh();
    }

    public void UpdateParent(LevelObjectGroup levelObjectGroup)
    {
        if (this.levelObjectGroup == levelObjectGroup) return;
        if (this.levelObjectGroup != null) ClearParent();

        this.levelObjectGroup = levelObjectGroup;
    }

    public void ClearParent()
    {
        levelObjectGroup = null;
    }
}
