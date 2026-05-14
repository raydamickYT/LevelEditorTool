using System;
using System.Collections.Generic;
using UnityEngine;

//Root JSON document for a saved level (Unity JsonUtility).
[Serializable]
public class LevelProjectFile
{
    public int formatVersion = 1;
    public string levelName = "";
    public List<LevelObjectRecord> objects = new();
}

//One node in the level tree (sprite object or empty group).
[Serializable]
public class LevelObjectRecord
{
    public int instanceId;
    //-1 when parent is the level root (no LevelObject parent).
    public int parentInstanceId = -1;
    public bool isGroup;
    public string objectName = "";
    public string assetId = "";
    public string prefabGuid = "";
    public float px, py, pz;
    public float qx, qy, qz, qw;
    public float sx, sy, sz;
    public int sortingOrder;
}
