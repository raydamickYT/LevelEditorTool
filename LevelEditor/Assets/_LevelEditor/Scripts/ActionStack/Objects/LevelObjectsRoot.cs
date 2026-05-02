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
    private List<GameObject> levelObjects = new();

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

    void Start()
    {
        EventManager.Instance.AddUnityEventListener(ObjectHierarchyEvents.OnCreateGroupParentObject, CreateParentObject);
    }

    void OnEnable()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<LevelObject>())
            {
                levelObjects.Add(child.gameObject);
            }
        }
    }

    void OnDestroy()
    {
        levelObjects.Clear();
    }

    public void AddLevelObject(GameObject child)
    {
        if (child == null) return;
        if (rootTransform == null)
        {
            Debug.LogWarning("Cannot add level object because rootTransform is null.");
            return;
        }

        if (levelObjects == null)
        {
            Debug.LogWarning("Cannot add level object because levelObjects list is null.");
            return;
        }

        if (child.GetComponent<LevelObject>())
        {
            levelObjects.Add(child.gameObject);
            child.transform.SetParent(rootTransform, true);
        }
        else
        {
            Debug.LogWarning("object is not a level object: " + child.name);
        }
    }

    public void RemoveChildFromParent(GameObject child)
    {
        if (levelObjects.Contains(child.gameObject))
        {
            levelObjects.Remove(child);
        }
    }

    //for creating an empty gameobject as a parent in the scene
    private void CreateParentObject()
    {
        List<LevelObject> selectedObjects = EditorBlackBoard.CurrentSelectedLevelObjects.Where(x => x != null).ToList(); //cache the selection

        if (selectedObjects.Count < 2)
        {
            Debug.Log("Select atleast 2 objects to create a parent");
        }

        GameObject itemObject = new GameObject("EmptyParent");
        itemObject.transform.position = SelectionBoundsCalculator.GetSelectionBoundsCenter(EditorBlackBoard.CurrentSelection.ToHashSet());

        itemObject.transform.SetParent(this.transform, true);

        var levelObjectGroup = itemObject.AddComponent<LevelObjectGroup>();
        itemObject.AddComponent<SelectableObject>();
        ObjectRegistry.OnObjectCreated(levelObjectGroup);


        //update the selection
        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, new List<GameObject> { itemObject });

        //add the objects to the parent transform
        foreach (LevelObject levelObject in selectedObjects)
        {
            levelObject.transform.SetParent(itemObject.transform, true);
            levelObjectGroup.AddChild(levelObject);
        }

        //refresh the objectHierarchy menu
        var HierarchyChange = new HierarchyChange(levelObjectGroup, HierarchyChangeType.AddedParent);
        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RefreshMenu, new List<HierarchyChange> { HierarchyChange });
    }
}
