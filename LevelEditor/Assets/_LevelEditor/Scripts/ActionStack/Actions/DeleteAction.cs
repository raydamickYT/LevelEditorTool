using UnityEditor;
using UnityEngine;

/// <summary>
/// This action will allow us to undo the deletion of a gameobject
/// it needs to posses:
/// - said gameobject's info before it got removed
/// 
/// and it will:
/// - create a copy of that object on undo and regiser it to the <see cref="ObjectRegistry"/>
/// </summary>
public class DeleteAction : IUndoableAction
{
    GameObject targetPrefabGameObject;
    GameObject instantiatedGameObject;
    LevelObject.Memento beforeState;
    int targetID;
    string debugLabel;

    public DeleteAction(LevelObject target)
    {
        debugLabel = target.name;
        targetPrefabGameObject = target.PrefabReference;
        beforeState = target.Save();
        targetID = target.ObjectID;
    }
    public string DebugLabel => debugLabel;

    public void Redo()
    {
        if(instantiatedGameObject != null)
        {
            //destroy gameobject
            GameObject.Destroy(instantiatedGameObject);
        }
    }

    public void Undo()
    {
        instantiatedGameObject = GameObject.Instantiate(targetPrefabGameObject, beforeState.Position, beforeState.Rotation);
        instantiatedGameObject.transform.SetParent(beforeState.parent, true);
        
        instantiatedGameObject.transform.localScale = beforeState.Scale;
        instantiatedGameObject.GetComponent<LevelObject>().PrefabReference = targetPrefabGameObject;
        instantiatedGameObject.GetComponent<LevelObject>().ObjectID = targetID;

        ObjectRegistry.RegisterObject(instantiatedGameObject, targetID);
    }
}
