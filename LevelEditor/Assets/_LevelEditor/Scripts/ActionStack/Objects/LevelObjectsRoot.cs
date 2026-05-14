using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelObjectsRoot : MonoBehaviour
{
    //static setup
    private static LevelObjectsRoot instance;
    public static LevelObjectsRoot Instance => instance != null ? instance : instance = FindAnyObjectByType<LevelObjectsRoot>()
    ?? new GameObject("LevelObjectsRoot").AddComponent<LevelObjectsRoot>(); //technically it should be impossible for there to never be a levelobjectsroot, but just in case


    [SerializeField] private Transform rootTransform;
    public Transform RootTransform => rootTransform != null ? rootTransform : transform; //if rootTransform is null, get the transform of this object.

    //libraries
    private List<GameObject> levelObjectsInRoot = new();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (rootTransform == null)
            rootTransform = transform;
    }


    void OnEnable()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<LevelObject>())
            {
                levelObjectsInRoot.Add(child.gameObject);
            }
        }
    }

    void OnDestroy()
    {
        levelObjectsInRoot.Clear();
    }

    public void AddObjectToLevelObjectRoot(GameObject child)
    {
        if (child == null) return;
        if (rootTransform == null)
        {
            Debug.LogWarning("Cannot add level object because rootTransform is null.");
            return;
        }

        if (levelObjectsInRoot == null)
        {
            Debug.LogWarning("Cannot add level object because levelObjects list is null.");
            return;
        }

        if (child.GetComponent<LevelObject>())
        {
            levelObjectsInRoot.Add(child.gameObject);
            child.transform.SetParent(rootTransform, true);
        }
        else
        {
            Debug.LogWarning("object is not a level object: " + child.name);
        }
    }

    public void RemoveChildFromParent(GameObject child)
    {
        if (levelObjectsInRoot.Contains(child.gameObject))
        {
            levelObjectsInRoot.Remove(child);
        }
    }

    /// <summary>Snapshot of direct root level objects (under <see cref="RootTransform"/>).</summary>
    public List<GameObject> GetRootLevelObjectsSnapshot()
    {
        return levelObjectsInRoot.Where(x => x != null).ToList();
    }

    /// <summary>Removes every root level object via <see cref="LevelObjectSpawner.Despawn"/>.</summary>
    public void DestroyAllRootLevelObjects()
    {
        List<GameObject> copy = GetRootLevelObjectsSnapshot();
        foreach (GameObject go in copy)
        {
            if (go != null)
                LevelObjectSpawner.Despawn(go);
        }
    }

    /// <summary>
    /// Appends world-space <see cref="Bounds"/> for every <see cref="Collider2D"/> under tracked level objects,
    /// skipping roots listed in <paramref name="excludedLevelObjectRoots"/>.
    /// </summary>
    public void AppendLevelColliderBounds(List<Bounds> buffer, HashSet<GameObject> excludedLevelObjectRoots)
    {
        if (buffer == null || levelObjectsInRoot == null)
            return;

        foreach (GameObject go in levelObjectsInRoot)
        {
            if (go == null)
                continue;
            if (excludedLevelObjectRoots != null && excludedLevelObjectRoots.Contains(go))
                continue;

            Collider2D[] colliders = go.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null)
                    continue;
                buffer.Add(col.bounds);
            }
        }
    }
}
