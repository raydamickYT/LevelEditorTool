using System.Collections.Generic;
using UnityEngine;

public class GizmoTempParent : MonoBehaviour
{
    private readonly List<Transform> children = new();
    // Parallel to children: the parent each object had before it was pulled into the temp parent.
    // null means the object lived at the root (so it must return to the root list).
    private readonly List<Transform> originalParents = new();


    public void Attach(IEnumerable<GameObject> objects)
    {
        children.Clear();
        originalParents.Clear();

        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;
            if (!obj.TryGetComponent(out LevelObject _)) continue;

            // Remember where the object came from so it can return there (root OR its group)
            // instead of being force-detached to the root when the gizmo operation ends.
            Transform originalParent = obj.transform.parent;
            bool wasRootObject = originalParent == null
                || originalParent == LevelObjectsRoot.Instance.RootTransform;

            LevelObjectsRoot.Instance.RemoveChildFromParent(obj);

            children.Add(obj.transform);
            originalParents.Add(wasRootObject ? null : originalParent);
            obj.transform.SetParent(transform, true);
        }
    }

    public void DetachAllToOriginalParents()
    {
        for (int i = 0; i < children.Count; i++)
        {
            Transform child = children[i];
            if (child == null) continue;

            Transform originalParent = originalParents[i];

            // Root object: hand it back to the root list as before.
            if (originalParent == null)
            {
                LevelObjectsRoot.Instance.AddObjectToLevelObjectRoot(child.gameObject);
                continue;
            }

            // Group child: return it under its original parent instead of yanking it to root.
            child.SetParent(originalParent, true);

            if (originalParent.TryGetComponent(out LevelObjectGroup group)
                && child.TryGetComponent(out LevelObject childLo))
            {
                group.AddChild(childLo);
            }
        }

        children.Clear();
        originalParents.Clear();
    }

    public void DisableAndDetach()
    {
        DetachAllToOriginalParents();
        gameObject.SetActive(false);
    }

}
