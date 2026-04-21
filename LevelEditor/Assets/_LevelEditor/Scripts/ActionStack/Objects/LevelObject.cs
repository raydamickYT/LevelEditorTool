using UnityEngine;

public class LevelObject : MonoBehaviour
{
    public class Memento
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public Memento(Transform t)
        {
            Position = t.position;
            Rotation = t.rotation;
            Scale = t.localScale;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    // has to be called from the action classes
    public Memento Save()
    {
        return new Memento(transform);
    }

    public void Restore(Memento m)
    {
        transform.position = m.Position;
        transform.rotation = m.Rotation;
        transform.localScale = m.Scale;
    }
}
