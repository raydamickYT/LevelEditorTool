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
    public LevelViewportRecord viewport;
}

/// <summary>Editor-only game viewport / camera frame (not exported to external game JSON).</summary>
[Serializable]
public class LevelViewportRecord
{
    public bool enabled;
    public float pixelX;
    public float pixelY;
    public float pixelWidth = 600f;
    public float pixelHeight = 600f;
    public float pixelScale = 0.01f;
    public bool lockAspectRatio;
    public float outlineR = 1f;
    public float outlineG;
    public float outlineB;
    public float outlineA = 0.85f;
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
    public bool hasCollision;
}
