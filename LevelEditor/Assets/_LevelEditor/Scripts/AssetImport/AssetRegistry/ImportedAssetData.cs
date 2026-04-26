using System;
using UnityEngine;

[Serializable]
public class ImportedAssetData
{
    public string AssetID;
    public string FileName;
    public string FilePath;
}

[Serializable]
public class ImportedSpriteData : ImportedAssetData
{
    public Sprite Sprite;
}
