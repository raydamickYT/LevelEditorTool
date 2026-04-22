using System.Runtime.Serialization;
using UnityEngine;

public class LevelObject : MonoBehaviour
{
    public int ObjectID;
    public GameObject PrefabReference;
    public class Memento
    {
        public Transform parent;
        public GameObject PrefabReference;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public int ObjectID;

        public Memento(Transform t, GameObject obj, int id)
        {
            Position = t.position;
            Rotation = t.rotation;
            Scale = t.localScale;
            PrefabReference = obj;
            parent = t.transform.parent;
            ObjectID = id;
        }
    }
    void OnEnable()
    {
    }

    public void OnDestroy()
    {
        ObjectRegistry.DeregisterObject(this);
    }

    // has to be called from the action classes
    public Memento Save()
    {
        return new Memento(transform, PrefabReference, ObjectID);
    }

    public void Restore(Memento m)
    {
        transform.position = m.Position;
        transform.rotation = m.Rotation;
        transform.localScale = m.Scale;
    }
}
