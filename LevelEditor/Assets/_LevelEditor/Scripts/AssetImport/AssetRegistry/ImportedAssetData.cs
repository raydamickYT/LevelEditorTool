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

    public int Width;
    public int Height;
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
