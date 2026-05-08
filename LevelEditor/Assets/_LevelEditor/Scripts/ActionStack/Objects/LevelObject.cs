using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class LevelObject : MonoBehaviour
{
    public int ObjectID;
    public string AssetID;
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
        transform.position = m.Position;
        transform.rotation = m.Rotation;
        transform.localScale = m.Scale;

        if (m.Sprite != null)
        {
            if (transform.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.sprite = m.Sprite;
            }
        }
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
