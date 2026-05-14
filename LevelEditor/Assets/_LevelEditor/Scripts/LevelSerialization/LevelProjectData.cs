using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Root JSON document for a saved level (Unity JsonUtility).</summary>
[Serializable]
public class LevelProjectFile
{
    public int formatVersion = 1;
    public string levelName = "";
    public List<LevelObjectRecord> objects = new();
}

/// <summary>One node in the level tree (sprite object or empty group).</summary>
[Serializable]
public class LevelObjectRecord
{
    public int instanceId;
    /// <summary>-1 when parent is the level root (no LevelObject parent).</summary>
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
