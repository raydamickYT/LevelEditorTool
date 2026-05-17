using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ImportedAssetMetaData
{
    public string AssetID;
    public string FileName;
    public string OriginalFilePath;
    public string LocalFilePath;
    public string AssetType;
    public string SourceProjectRoot;
    public string AssetRelativePath;
    public string FolderPath;

    public int Width;
    public int Height;
    public float SpriteRectX;
    public float SpriteRectY;
    public float SpriteRectWidth;
    public float SpriteRectHeight;
    public float PixelsPerUnit = 100f;
}

[Serializable]
public class ImportedSpriteData : ImportedAssetMetaData
{
    [NonSerialized]
    public Sprite Sprite;
}

public class AssetMetaDataCollection
{
    public List<ImportedAssetMetaData> Assets = new();
}

public static class ImportedAssetSourceKinds
{
    public const string UnityProject = "UnityProject";
}
