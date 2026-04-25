using System.Collections.Generic;
using UnityEngine;

public class GizmoTempParent : MonoBehaviour
{
    private List<Transform> children  = new();


    public void Attach(IEnumerable<GameObject> objects)
    {
        children.Clear();

        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;

            LevelObjectsRoot.Instance.RemoveChildFromParent(obj);
            
            if (!obj.TryGetComponent(out LevelObject _)) continue;

            children.Add(obj.transform);
            obj.transform.SetParent(transform, true);
        }
    }

    public void DetachAllToRoot()
    {
        foreach (Transform child in children)
        {
            if (child == null) continue;

            LevelObjectsRoot.Instance.AddLevelObject(child.gameObject);
        }

        children.Clear();
    }
    void OnDisable()
    {
        DetachAllToRoot();
    }
}
