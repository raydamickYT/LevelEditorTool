using System.Collections.Generic;
using UnityEngine;

public class LevelObjectGroup : LevelObject
{
    public bool IsGroup => true;
    List<LevelObject> levelObjects = new();


    public void AddChild(LevelObject child)
    {
        if(child == null) return;

        levelObjects.Add(child);
    }
}
