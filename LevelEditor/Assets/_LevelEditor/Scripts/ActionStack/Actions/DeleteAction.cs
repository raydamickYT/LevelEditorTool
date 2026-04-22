using UnityEditor;
using UnityEngine;

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
        instantiatedGameObject = GameObject.Instantiate(targetPrefabGameObject, beforeState.Position, beforeState.Rotation, beforeState.parent);
        instantiatedGameObject.transform.localScale = beforeState.Scale;

        ObjectRegistry.RegisterObject(instantiatedGameObject, targetID);
    }
}
