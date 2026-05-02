using System.Collections.Generic;
using UnityEngine;

public class LevelObjectGroup : LevelObject
{
    public bool IsGroup => true;
    private List<LevelObject> levelObjects = new();
    public IEnumerable<LevelObject> LevelObjects => levelObjects;


    public void AddChild(LevelObject child)
    {
        if(child == null) return;

        levelObjects.Add(child);
    }
}
