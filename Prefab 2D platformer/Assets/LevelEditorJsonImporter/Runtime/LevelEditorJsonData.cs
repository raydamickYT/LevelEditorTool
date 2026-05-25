using System;
using System.Collections.Generic;

namespace LevelEditorJsonImporter
{
    [Serializable]
    public sealed class LevelEditorProjectFile
    {
        public int formatVersion = 1;
        public string levelName = "";
        public List<LevelEditorObjectRecord> objects = new List<LevelEditorObjectRecord>();
    }

    [Serializable]
    public sealed class LevelEditorObjectRecord
    {
        public int instanceId;
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

    [Serializable]
    public sealed class LevelEditorAssetMetaData
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
    public sealed class LevelEditorAssetMetaDataCollection
    {
        public List<LevelEditorAssetMetaData> Assets = new List<LevelEditorAssetMetaData>();
    }

    public static class LevelEditorImportedAssetTypes
    {
        public const string Sprite = "Sprite";
        public const string Prefab = "Prefab";
    }
}
