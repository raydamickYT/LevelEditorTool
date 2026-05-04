using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// this class is responsible for storing all imported assets in local folders and creating links to meta data files. 
/// this'll make it possible to reference these assets later on. 
/// **note: I've added a lot of comments here since this is new to me so I need them**
/// </summary>
public static class AssetStorageService
{
    //appcontext.BaseDirectory takes the root folder of where the application is stored.
    //but when we're in the editor it should store it in the project directory
#if UNITY_EDITOR
    private static readonly string RootFolder = Path.Combine(Application.dataPath, "..", "UserData");
#else
    private static readonly string RootFolder = Path.Combine(System.AppContext.BaseDirectory, "UserData");
#endif


    private static readonly string AssetFolder = Path.Combine(RootFolder, "Assets");

    private static readonly string SpriteFolder = Path.Combine(AssetFolder, "Sprites");

    private static readonly string MetaDataPath = Path.Combine(RootFolder, "asset_registry.json");

    //caching meta data
    private static AssetMetaDataCollection cachedMetaData;
    private static readonly Dictionary<string, ImportedAssetMetaData> assetLookup = new();
    private static bool metaDataLoaded;

    //this function fills the ImportedSpriteData completely and sends it to the metaData Function
    public static void SaveLocalCopy(string originalPath, ImportedSpriteData data, string type)
    {
        Debug.Log(RootFolder); //to check where it stores it on editor runtime while testing.

        //create the sprite dir
        Directory.CreateDirectory(SpriteFolder);

        //setup some data we want to save later
        string extension = Path.GetExtension(originalPath);
        string safeFileName = data.AssetID + extension;
        string destinationPath = Path.Combine(SpriteFolder, safeFileName);

        //create a copy of the asset and store it in the pre-determined folder
        File.Copy(originalPath, destinationPath, overwrite: true);

        //save all the paths and names in the data class
        data.OriginalFilePath = originalPath;
        data.FileName = Path.GetFileName(originalPath);
        data.LocalFilePath = destinationPath;
        data.AssetType = type;

        if (type == ImportedAssetTypes.Sprite) //todo this can become a switch case when there's different types of assets we import.
        {
            SaveSprite(data);
        }


        SaveMetaData(data);
    }

    //this function stores the earlier made ImportedSpriteData and stores it in a metadata file
    public static void SaveMetaData(ImportedSpriteData data)
    {
        //create the dir for metadatafiles
        Directory.CreateDirectory(RootFolder);

        //check if there's already metadata stored
        AssetMetaDataCollection collection = GetCachedMetaData();

        int existingIndex = collection.Assets.FindIndex(asset => asset.AssetID == data.AssetID);

        //if not
        if (existingIndex >= 0)
        {
            //create a new one
            collection.Assets[existingIndex] = data;
        }
        else
        {
            //else add this asset
            collection.Assets.Add(data);
        }

        assetLookup[data.AssetID] = data;

        //create a json string
        string json = JsonUtility.ToJson(collection, prettyPrint: true);
        File.WriteAllText(MetaDataPath, json); //and store it in an actual location.
    }

    //loading the meta data from the created path
    private static AssetMetaDataCollection LoadMetaDataFromDisk()
    {
        //check if the file exists if not return a new collection
        if (!File.Exists(MetaDataPath))
        {
            return new AssetMetaDataCollection();
        }

        string json = File.ReadAllText(MetaDataPath);

        //check if the existing file containts data, if not return a new collection
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AssetMetaDataCollection();
        }

        //create a new collection from the data that we got from the json
        AssetMetaDataCollection collection = JsonUtility.FromJson<AssetMetaDataCollection>(json);

        //return that but if the collection happens to be null for some reason return a new collection.
        return collection ?? new AssetMetaDataCollection();
    }

    private static AssetMetaDataCollection GetCachedMetaData()
    {
        if (metaDataLoaded && cachedMetaData != null)
        {
            return cachedMetaData;
        }

        cachedMetaData = LoadMetaDataFromDisk();
        RebuildAssetLookup();

        metaDataLoaded = true;

        return cachedMetaData;
    }
    private static void RebuildAssetLookup()
    {
        assetLookup.Clear();

        if (cachedMetaData == null || cachedMetaData.Assets == null)
        {
            return;
        }

        foreach (ImportedAssetMetaData asset in cachedMetaData.Assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.AssetID))
            {
                continue;
            }

            assetLookup[asset.AssetID] = asset;
        }
    }

    private static void SaveSprite(ImportedSpriteData data)
    {
        //save the sprite in the data class.
        if (data.Sprite != null)
        {
            data.Width = (int)data.Sprite.rect.width;
            data.Height = (int)data.Sprite.rect.height;
        }
    }

    public static ImportedAssetMetaData GetAssetByID(string assetID)
    {
        if (string.IsNullOrWhiteSpace(assetID))
        {
            return null;
        }

        GetCachedMetaData();

        if (assetLookup.TryGetValue(assetID, out ImportedAssetMetaData asset))
        {
            return asset;
        }

        return null;
    }

    public static void ClearMetaDataCache()
    {
        cachedMetaData = null;
        assetLookup.Clear();
        metaDataLoaded = false;
    }
    public static void ReloadMetaDataCache()
    {
        ClearMetaDataCache();
        GetCachedMetaData();
    }

}


public static class ImportedAssetTypes
{
    public const string Sprite = "Sprite";
}