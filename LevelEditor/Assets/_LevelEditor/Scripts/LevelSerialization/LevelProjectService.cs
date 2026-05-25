using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Saves / loads level JSON plus optional <c>BundledAssets/</c> (sprites + fragment of <c>asset_registry.json</c>).
/// </summary>
public static class LevelProjectService
{
    public const string DefaultLevelFileName = "level.json";
    public const string BundledAssetsFolderName = "BundledAssets";
    /// <summary>Full workspace asset registry snapshot next to <see cref="DefaultLevelFileName"/> (merged on load before bundled overrides).</summary>
    public const string ProjectAssetRegistryFileName = "project_asset_registry.json";

    public static void SaveLevelToPath(string levelJsonFilePath, string levelDisplayName = "")
    {
        if (string.IsNullOrWhiteSpace(levelJsonFilePath))
            throw new ArgumentException("Path is empty.", nameof(levelJsonFilePath));

        LevelProjectFile file = BuildProjectFileFromScene(string.IsNullOrEmpty(levelDisplayName)
            ? Path.GetFileNameWithoutExtension(levelJsonFilePath)
            : levelDisplayName);

        string json = JsonUtility.ToJson(file, true);
        string directory = Path.GetDirectoryName(levelJsonFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(levelJsonFilePath, json, Encoding.UTF8);

        HashSet<string> assetIds = new();
        foreach (LevelObjectRecord rec in file.objects)
        {
            if (!rec.isGroup && !string.IsNullOrEmpty(rec.assetId))
                assetIds.Add(rec.assetId);
        }

        if (!string.IsNullOrEmpty(directory))
        {
            TryWriteBundledAssets(directory, assetIds);
            AssetStorageService.WriteProjectRegistrySnapshot(Path.Combine(directory, ProjectAssetRegistryFileName));
            LevelProjectSession.SetProjectDirectory(directory);
        }

        LevelProjectSession.SetCurrentLevelJsonPath(Path.GetFullPath(levelJsonFilePath));
    }

    public static void LoadLevelFromPath(string levelJsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(levelJsonFilePath) || !File.Exists(levelJsonFilePath))
        {
            Debug.LogError("Level JSON not found: " + levelJsonFilePath);
            return;
        }

        string fullLevelPath = Path.GetFullPath(levelJsonFilePath);
        if (LevelProjectSession.IsSameLevelJsonLoaded(fullLevelPath))
            return;

        EventManager.Instance.TriggerDelegate(
            SelectionEvents.ReplaceSelectionWithObject,
            Enumerable.Empty<GameObject>());

        string levelDirectory = Path.GetDirectoryName(levelJsonFilePath);
        if (!string.IsNullOrEmpty(levelDirectory))
        {
            LevelProjectSession.SetProjectDirectory(levelDirectory);

            string projectReg = Path.Combine(levelDirectory, ProjectAssetRegistryFileName);
            if (File.Exists(projectReg))
                AssetStorageService.MergeProjectRegistrySnapshot(projectReg, levelDirectory);

            AssetStorageService.MergeBundledAssetsFromLevelFolder(levelDirectory);
        }

        if (ObjectLibraryManager.Instance != null)
            ObjectLibraryManager.Instance.RebuildLibraryFromAssetStorage();

        string json = File.ReadAllText(levelJsonFilePath, Encoding.UTF8);
        LevelProjectFile file = JsonUtility.FromJson<LevelProjectFile>(json);
        if (file == null || file.objects == null)
        {
            Debug.LogError("Failed to parse level JSON.");
            return;
        }

        if (LevelObjectsRoot.Instance != null)
            LevelObjectsRoot.Instance.DestroyAllRootLevelObjects();

        ObjectRegistry.ClearAllForNewLevel();

        InstantiateFromRecords(file.objects);

        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RebuildEntireHierarchy);

        EventManager.Instance.TriggerDelegate(
            SelectionEvents.ReplaceSelectionWithObject,
            Enumerable.Empty<GameObject>());

        LevelProjectSession.SetCurrentLevelJsonPath(fullLevelPath);
    }

    static LevelProjectFile BuildProjectFileFromScene(string levelName)
    {
        LevelProjectFile file = new LevelProjectFile
        {
            formatVersion = 1,
            levelName = levelName ?? "",
            objects = new List<LevelObjectRecord>()
        };

        if (LevelObjectsRoot.Instance == null)
            return file;

        List<GameObject> roots = LevelObjectsRoot.Instance.GetRootLevelObjectsSnapshot();
        roots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        List<LevelObject> ordered = new();
        HashSet<LevelObject> seen = new();

        foreach (GameObject rootGo in roots)
        {
            if (rootGo == null)
                continue;
            LevelObject rootLo = rootGo.GetComponent<LevelObject>();
            if (rootLo == null)
                continue;
            CollectDepthFirstPreOrder(rootLo, ordered, seen);
        }

        Dictionary<LevelObject, int> idMap = new();
        for (int i = 0; i < ordered.Count; i++)
            idMap[ordered[i]] = i;

        foreach (LevelObject lo in ordered)
        {
            LevelObjectRecord rec = new LevelObjectRecord
            {
                instanceId = idMap[lo],
                isGroup = lo is LevelObjectGroup,
                objectName = GetStableObjectName(lo.gameObject.name, lo.AssetID),
                assetId = lo.AssetID ?? string.Empty,
                prefabGuid = LevelPrefabPathUtil.GetPrefabAssetGuid(lo.PrefabReference)
            };

            Transform t = lo.transform;
            rec.px = t.position.x;
            rec.py = t.position.y;
            rec.pz = t.position.z;
            Quaternion q = t.rotation;
            rec.qx = q.x;
            rec.qy = q.y;
            rec.qz = q.z;
            rec.qw = q.w;
            Vector3 ls = t.localScale;
            rec.sx = ls.x;
            rec.sy = ls.y;
            rec.sz = ls.z;

            if (t.parent != null && t.parent.TryGetComponent(out LevelObject parentLo) && idMap.TryGetValue(parentLo, out int pid))
                rec.parentInstanceId = pid;
            else
                rec.parentInstanceId = -1;

            if (!rec.isGroup && t.TryGetComponent(out SpriteRenderer sr))
            {
                rec.sortingOrder = sr.sortingOrder;
                rec.hasCollision = lo.HasCollision && IsSpriteAsset(lo.AssetID);
            }

            file.objects.Add(rec);
        }

        return file;
    }

    static void CollectDepthFirstPreOrder(LevelObject lo, List<LevelObject> ordered, HashSet<LevelObject> seen)
    {
        if (lo == null || seen.Contains(lo))
            return;

        seen.Add(lo);
        ordered.Add(lo);

        List<LevelObject> children = new();
        for (int i = 0; i < lo.transform.childCount; i++)
        {
            Transform ch = lo.transform.GetChild(i);
            if (ch.TryGetComponent(out LevelObject childLo))
                children.Add(childLo);
        }

        children.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        foreach (LevelObject child in children)
            CollectDepthFirstPreOrder(child, ordered, seen);
    }

    static bool IsSpriteAsset(string assetId)
    {
        ImportedAssetMetaData asset = AssetStorageService.GetAssetByID(assetId);
        return asset != null
            && string.Equals(asset.AssetType, ImportedAssetTypes.Sprite, StringComparison.OrdinalIgnoreCase);
    }

    static void TryWriteBundledAssets(string levelDirectory, HashSet<string> assetIds)
    {
        if (assetIds == null || assetIds.Count == 0)
            return;

        string bundleRoot = Path.Combine(levelDirectory, BundledAssetsFolderName);
        string spritesDir = Path.Combine(bundleRoot, "Sprites");
        Directory.CreateDirectory(spritesDir);

        AssetMetaDataCollection fragment = new AssetMetaDataCollection { Assets = new List<ImportedAssetMetaData>() };

        foreach (string assetId in assetIds)
        {
            ImportedAssetMetaData meta = AssetStorageService.GetAssetByID(assetId);
            if (meta == null || string.IsNullOrEmpty(meta.LocalFilePath) || !File.Exists(meta.LocalFilePath))
                continue;

            string ext = Path.GetExtension(meta.LocalFilePath);
            if (string.IsNullOrEmpty(ext))
                ext = ".png";

            string destFileName = $"{assetId}{ext}";
            string destAbs = Path.Combine(spritesDir, destFileName);

            string sourceAbs = Path.GetFullPath(meta.LocalFilePath);
            string destAbsFull = Path.GetFullPath(destAbs);

            // After load, LocalFilePath often points at this same bundled file — File.Copy onto itself fails / locks on Windows.
            if (!sourceAbs.Equals(destAbsFull, StringComparison.OrdinalIgnoreCase))
            {
                AssetRuntimeLoader.RemoveFromCache(assetId);
                CopyFileReplacing(sourceAbs, destAbsFull);
            }

            string json = JsonUtility.ToJson(meta);
            ImportedAssetMetaData clone = JsonUtility.FromJson<ImportedAssetMetaData>(json);
            clone.LocalFilePath = $"{BundledAssetsFolderName}/Sprites/{destFileName}".Replace('\\', '/');
            fragment.Assets.Add(clone);
        }

        if (fragment.Assets.Count == 0)
            return;

        string registryPath = Path.Combine(bundleRoot, "asset_registry.json");
        File.WriteAllText(registryPath, JsonUtility.ToJson(fragment, true), Encoding.UTF8);
    }

    /// <summary>Overwrites destination from disk bytes (avoids <see cref="File.Copy"/> exclusive-lock issues on Windows).</summary>
    static void CopyFileReplacing(string sourceFullPath, string destFullPath)
    {
        byte[] bytes = File.ReadAllBytes(sourceFullPath);
        File.WriteAllBytes(destFullPath, bytes);
    }

    static List<LevelObjectRecord> SortRecordsParentBeforeChildren(List<LevelObjectRecord> records)
    {
        HashSet<int> knownIds = new();
        foreach (LevelObjectRecord r in records)
        {
            if (r != null)
                knownIds.Add(r.instanceId);
        }

        List<LevelObjectRecord> ordered = new();
        HashSet<int> done = new();
        int guard = 0;
        while (done.Count < records.Count && guard++ <= records.Count + 5)
        {
            int beforeCount = done.Count;

            foreach (LevelObjectRecord r in records)
            {
                if (r == null || done.Contains(r.instanceId))
                    continue;

                bool parentReady = r.parentInstanceId < 0
                    || !knownIds.Contains(r.parentInstanceId)
                    || done.Contains(r.parentInstanceId);

                if (parentReady)
                {
                    ordered.Add(r);
                    done.Add(r.instanceId);
                }
            }

            if (done.Count == beforeCount)
                break;
        }

        foreach (LevelObjectRecord r in records)
        {
            if (r != null && !done.Contains(r.instanceId))
            {
                ordered.Add(r);
                done.Add(r.instanceId);
            }
        }

        return ordered;
    }

    static void InstantiateFromRecords(List<LevelObjectRecord> records)
    {
        if (records == null || records.Count == 0)
            return;

        records = SortRecordsParentBeforeChildren(records);

        GameObject template = ObjectLibraryManager.Instance != null
            ? ObjectLibraryManager.Instance.SpawnPrefabTemplate
            : null;

        Dictionary<int, GameObject> spawnedById = new();

        foreach (LevelObjectRecord rec in records)
        {
            Transform parentTransform = null;
            if (rec.parentInstanceId >= 0 && spawnedById.TryGetValue(rec.parentInstanceId, out GameObject parentGo))
                parentTransform = parentGo.transform;

            if (rec.isGroup)
            {
                GameObject groupGo = CreateEmptyGroup(rec, parentTransform);
                spawnedById[rec.instanceId] = groupGo;
                continue;
            }

            GameObject prefab = LevelPrefabPathUtil.LoadPrefabByGuid(rec.prefabGuid);
            if (prefab == null)
                prefab = template;

            if (prefab == null)
            {
                Debug.LogError($"Level load: no prefab for instance {rec.instanceId} (asset {rec.assetId}). Assign ObjectLibraryManager in scene.");
                continue;
            }

            Vector3 pos = new Vector3(rec.px, rec.py, rec.pz);
            Quaternion rot = new Quaternion(rec.qx, rec.qy, rec.qz, rec.qw);
            Vector3 scale = new Vector3(rec.sx, rec.sy, rec.sz);

            LevelObject.Memento memento = new LevelObject.Memento(pos, rot, scale, prefab, rec.assetId, null, null);
            GameObject spawned = LevelObjectSpawner.Spawn(memento, false, parentTransform, false, deferHierarchyNotification: true);
            if (spawned == null)
                continue;

            spawned.name = GetStableObjectName(rec.objectName, rec.assetId);
            spawnedById[rec.instanceId] = spawned;

            if (spawned.TryGetComponent(out LevelObject spawnedLevelObject))
                spawnedLevelObject.HasCollision = rec.hasCollision;

            if (parentTransform != null
                && parentTransform.TryGetComponent(out LevelObjectGroup parentGroup)
                && spawned.TryGetComponent(out LevelObject childLo))
            {
                parentGroup.AddChild(childLo);
            }

            if (spawned.TryGetComponent(out LevelObject loNotify))
                LevelObjectSpawner.NotifyHierarchyForSpawned(loNotify, false);

            if (spawned.TryGetComponent(out SpriteRenderer sr))
                sr.sortingOrder = rec.sortingOrder;
        }
    }

    static string GetStableObjectName(string objectName, string assetId)
    {
        if (!string.IsNullOrWhiteSpace(objectName) && !IsGeneratedLevelObjectName(objectName))
            return objectName;

        ImportedAssetMetaData asset = AssetStorageService.GetAssetByID(assetId);
        string assetName = GetAssetDisplayName(asset);
        if (!string.IsNullOrWhiteSpace(assetName))
            return assetName;

        return string.IsNullOrWhiteSpace(objectName)
            ? "Level Object"
            : objectName;
    }

    static bool IsGeneratedLevelObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return true;

        return objectName.StartsWith("New Game Object", StringComparison.OrdinalIgnoreCase);
    }

    static string GetAssetDisplayName(ImportedAssetMetaData asset)
    {
        if (asset == null)
            return null;

        string fileName = string.IsNullOrWhiteSpace(asset.FileName)
            ? asset.AssetRelativePath
            : asset.FileName;

        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(nameWithoutExtension)
            ? fileName
            : nameWithoutExtension;
    }

    static GameObject CreateEmptyGroup(LevelObjectRecord rec, Transform parentTransform)
    {
        GameObject go = new GameObject(string.IsNullOrEmpty(rec.objectName) ? "Group" : rec.objectName);
        LevelObjectGroup group = go.AddComponent<LevelObjectGroup>();
        go.AddComponent<SelectableObject>();

        Vector3 pos = new Vector3(rec.px, rec.py, rec.pz);
        Quaternion rot = new Quaternion(rec.qx, rec.qy, rec.qz, rec.qw);
        Vector3 scale = new Vector3(rec.sx, rec.sy, rec.sz);

        if (parentTransform == null)
        {
            LevelObjectsRoot.Instance.AddObjectToLevelObjectRoot(go);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
        }
        else
        {
            go.transform.SetParent(parentTransform, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
        }

        ObjectRegistry.OnObjectCreated(group);

        if (parentTransform != null
            && parentTransform.TryGetComponent(out LevelObjectGroup parentGroup))
        {
            parentGroup.AddChild(group);
        }

        HierarchyChange change = new HierarchyChange(group, HierarchyChangeType.AddedParent);
        EventManager.Instance.TriggerDelegate(ObjectHierarchyEvents.RefreshMenu, new List<HierarchyChange> { change });

        return go;
    }
}
