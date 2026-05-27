using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LevelEditorJsonImporter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    public static class LevelJsonSceneImporter
    {
        const string ProjectRegistryFileName = "project_asset_registry.json";
        const string BundledRegistryRelativePath = "BundledAssets/asset_registry.json";
        public static GameObject ImportFromPath(
            string levelJsonPath,
            string importRootName,
            bool clearExistingImportRoot,
            bool selectImportedRoot,
            bool logResult = true)
        {
            LevelEditorProjectFile level = LoadLevelFile(levelJsonPath);
            if (level == null || level.objects == null)
                throw new InvalidDataException("Could not parse the selected level JSON.");

            string levelDirectory = Path.GetDirectoryName(levelJsonPath);
            Dictionary<string, LevelEditorAssetMetaData> assetsById = LoadRegistries(levelDirectory);
            GameObject root = ImportLevel(level, assetsById, levelJsonPath, importRootName, clearExistingImportRoot);

            if (selectImportedRoot && root != null)
                Selection.activeGameObject = root;

            if (root != null)
            {
                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(root.scene);
            }

            if (logResult)
                Debug.Log($"Imported '{level.levelName}' with {level.objects.Count} records. Registry assets loaded: {assetsById.Count}.");

            return root;
        }

        public static long GetLevelJsonUtcTicks(string levelJsonPath)
        {
            if (string.IsNullOrWhiteSpace(levelJsonPath) || !File.Exists(levelJsonPath))
                return 0;

            return File.GetLastWriteTimeUtc(levelJsonPath).Ticks;
        }

        static LevelEditorProjectFile LoadLevelFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("Level JSON not found.", path);

            return JsonUtility.FromJson<LevelEditorProjectFile>(File.ReadAllText(path));
        }

        static Dictionary<string, LevelEditorAssetMetaData> LoadRegistries(string levelDirectory)
        {
            Dictionary<string, LevelEditorAssetMetaData> assetsById = new Dictionary<string, LevelEditorAssetMetaData>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(levelDirectory))
                return assetsById;

            MergeRegistry(Path.Combine(levelDirectory, ProjectRegistryFileName), assetsById);
            MergeRegistry(Path.Combine(levelDirectory, BundledRegistryRelativePath), assetsById);
            return assetsById;
        }

        static void MergeRegistry(string path, Dictionary<string, LevelEditorAssetMetaData> assetsById)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            LevelEditorAssetMetaDataCollection registry =
                JsonUtility.FromJson<LevelEditorAssetMetaDataCollection>(File.ReadAllText(path));

            if (registry == null || registry.Assets == null)
                return;

            foreach (LevelEditorAssetMetaData asset in registry.Assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.AssetID))
                    continue;

                assetsById[asset.AssetID] = asset;
            }
        }

        static GameObject ImportLevel(
            LevelEditorProjectFile level,
            Dictionary<string, LevelEditorAssetMetaData> assetsById,
            string levelJsonPath,
            string importRootName,
            bool clearExistingImportRoot)
        {
            string rootName = string.IsNullOrWhiteSpace(importRootName)
                ? "Imported Level"
                : importRootName.Trim();

            GameObject existingRoot = GameObject.Find(rootName);
            if (clearExistingImportRoot)
            {
                if (existingRoot != null)
                {
                    Undo.DestroyObjectImmediate(existingRoot);
                    existingRoot = null;
                }
            }

            GameObject root = existingRoot;
            if (root == null)
            {
                root = new GameObject(rootName);
                Undo.RegisterCreatedObjectUndo(root, "Import Level JSON");
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(root, "Import Level JSON");
            }

            LevelJsonImportSource importSource = root.GetComponent<LevelJsonImportSource>();
            if (importSource == null)
                importSource = root.AddComponent<LevelJsonImportSource>();

            importSource.levelJsonPath = Path.GetFullPath(levelJsonPath);
            importSource.importRootName = rootName;
            importSource.lastImportedUtcTicks = GetLevelJsonUtcTicks(levelJsonPath);

            SyncRecordsIntoRoot(root, level.objects, assetsById);

            return root;
        }

        static void SyncRecordsIntoRoot(
            GameObject root,
            List<LevelEditorObjectRecord> records,
            Dictionary<string, LevelEditorAssetMetaData> assetsById)
        {
            Dictionary<int, LevelJsonImportedObject> existingById = root
                .GetComponentsInChildren<LevelJsonImportedObject>(true)
                .Where(component => component != null)
                .GroupBy(component => component.instanceId)
                .ToDictionary(group => group.Key, group => group.First());

            if (existingById.Count == 0 && root.transform.childCount > 0)
            {
                // Migration path for roots imported before incremental metadata existed.
                List<GameObject> rootChildren = new List<GameObject>();
                for (int i = 0; i < root.transform.childCount; i++)
                    rootChildren.Add(root.transform.GetChild(i).gameObject);

                foreach (GameObject child in rootChildren)
                    Undo.DestroyObjectImmediate(child);
            }

            Dictionary<int, Transform> spawnedById = new Dictionary<int, Transform>();
            HashSet<int> seenIds = new HashSet<int>();
            List<LevelEditorObjectRecord> orderedRecords = SortRecordsParentBeforeChildren(records);

            foreach (LevelEditorObjectRecord record in orderedRecords)
            {
                if (record == null)
                    continue;

                Transform parent = root.transform;
                if (record.parentInstanceId >= 0 && spawnedById.TryGetValue(record.parentInstanceId, out Transform spawnedParent))
                    parent = spawnedParent;

                bool useWrapper = record.parentInstanceId >= 0;
                string signature = BuildRecordSignature(record, useWrapper);
                GameObject spawned;

                if (existingById.TryGetValue(record.instanceId, out LevelJsonImportedObject existingMeta)
                    && existingMeta != null
                    && existingMeta.gameObject != null)
                {
                    spawned = existingMeta.gameObject;

                    // Rebuild only when structural/asset identity changed; otherwise keep object identity.
                    if (!string.Equals(existingMeta.recordSignature, signature, StringComparison.Ordinal))
                        spawned = ReplaceImportedObjectKeepingChildren(existingMeta, record, assetsById, useWrapper);
                }
                else
                {
                    spawned = CreateImportedObject(record, assetsById, useWrapper);
                }

                if (spawned == null)
                    continue;

                ApplyRecordTransform(spawned.transform, parent, record);
                if (!record.isGroup && !useWrapper)
                    MatchRootObjectVisualToEditorPreview(spawned, record, assetsById);

                ApplyRecordName(spawned, record);
                ApplySortingOrder(spawned, record.sortingOrder);
                EnsureImportMetadata(spawned, record, useWrapper, signature);

                spawnedById[record.instanceId] = spawned.transform;
                seenIds.Add(record.instanceId);
            }

            foreach (KeyValuePair<int, LevelJsonImportedObject> pair in existingById)
            {
                if (seenIds.Contains(pair.Key))
                    continue;

                LevelJsonImportedObject stale = pair.Value;
                if (stale != null && stale.gameObject != null)
                    Undo.DestroyObjectImmediate(stale.gameObject);
            }
        }

        static GameObject ReplaceImportedObjectKeepingChildren(
            LevelJsonImportedObject existingMeta,
            LevelEditorObjectRecord record,
            Dictionary<string, LevelEditorAssetMetaData> assetsById,
            bool useWrapper)
        {
            GameObject oldObject = existingMeta.gameObject;
            Transform oldTransform = oldObject.transform;
            Transform oldParent = oldTransform.parent;

            GameObject replacement = CreateImportedObject(record, assetsById, useWrapper);
            if (replacement == null)
                return oldObject;

            replacement.transform.SetParent(oldParent, false);

            List<Transform> children = new List<Transform>();
            for (int i = 0; i < oldTransform.childCount; i++)
                children.Add(oldTransform.GetChild(i));

            foreach (Transform child in children)
                child.SetParent(replacement.transform, true);

            Undo.DestroyObjectImmediate(oldObject);
            return replacement;
        }

        static GameObject CreateImportedObject(
            LevelEditorObjectRecord record,
            Dictionary<string, LevelEditorAssetMetaData> assetsById,
            bool useWrapper)
        {
            GameObject spawned = record.isGroup
                ? CreateGroupObject(record)
                : CreateLevelObject(record, assetsById, useWrapper);

            if (spawned != null)
                Undo.RegisterCreatedObjectUndo(spawned, "Import Level JSON");

            return spawned;
        }

        static void EnsureImportMetadata(
            GameObject gameObject,
            LevelEditorObjectRecord record,
            bool useWrapper,
            string signature)
        {
            LevelJsonImportedObject metadata = gameObject.GetComponent<LevelJsonImportedObject>();
            if (metadata == null)
                metadata = gameObject.AddComponent<LevelJsonImportedObject>();

            metadata.instanceId = record.instanceId;
            metadata.parentInstanceId = record.parentInstanceId;
            metadata.isGroup = record.isGroup;
            metadata.usesWrapper = useWrapper;
            metadata.assetId = record.assetId ?? "";
            metadata.prefabGuid = record.prefabGuid ?? "";
            metadata.hasCollision = record.hasCollision;
            metadata.recordSignature = signature ?? "";
        }

        static string BuildRecordSignature(LevelEditorObjectRecord record, bool useWrapper)
        {
            return string.Join("|",
                record.isGroup ? "1" : "0",
                useWrapper ? "1" : "0",
                record.assetId ?? "",
                record.prefabGuid ?? "",
                record.hasCollision ? "1" : "0");
        }

        static GameObject CreateGroupObject(LevelEditorObjectRecord record)
        {
            return new GameObject(string.IsNullOrWhiteSpace(record.objectName) ? "Group" : record.objectName);
        }

        static GameObject CreateLevelObject(LevelEditorObjectRecord record, Dictionary<string, LevelEditorAssetMetaData> assetsById, bool useWrapper)
        {
            if (TryResolvePrefab(record, assetsById, out GameObject prefab, out LevelEditorAssetMetaData prefabMetaData))
                return useWrapper
                    ? CreatePrefabWrapper(record, prefab, prefabMetaData)
                    : CreatePrefabInstance(record, prefab);

            if (TryResolveSprite(record, assetsById, out Sprite sprite, out LevelEditorAssetMetaData spriteMetaData))
                return useWrapper
                    ? CreateSpriteWrapper(record, sprite, spriteMetaData)
                    : CreateSpriteObject(record, sprite);

            Debug.LogWarning($"Could not resolve asset for record '{record.objectName}' (assetId: {record.assetId}, prefabGuid: {record.prefabGuid}). Created empty placeholder.");
            return new GameObject(string.IsNullOrWhiteSpace(record.objectName) ? "Missing Level Object" : record.objectName);
        }

        static GameObject CreatePrefabInstance(LevelEditorObjectRecord record, GameObject prefab)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = GetImportedObjectName(record, prefab.name);
            return instance;
        }

        static GameObject CreateSpriteObject(LevelEditorObjectRecord record, Sprite sprite)
        {
            GameObject spriteObject = new GameObject(GetImportedObjectName(record, sprite.name));
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            AddSpriteColliderIfEnabled(record, spriteObject, renderer);
            return spriteObject;
        }

        static GameObject CreateSpriteWrapper(LevelEditorObjectRecord record, Sprite sprite, LevelEditorAssetMetaData spriteMetaData)
        {
            GameObject wrapper = new GameObject(GetImportedObjectName(record, sprite.name));
            GameObject spriteObject = new GameObject(sprite.name);
            spriteObject.transform.SetParent(wrapper.transform, false);
            spriteObject.transform.localPosition = Vector3.zero;
            spriteObject.transform.localRotation = Quaternion.identity;
            spriteObject.transform.localScale = Vector3.one;

            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            AddSpriteColliderIfEnabled(record, spriteObject, renderer);

            MatchSpriteVisualToEditorPreview(renderer, spriteMetaData);
            return wrapper;
        }

        static void AddSpriteColliderIfEnabled(LevelEditorObjectRecord record, GameObject spriteObject, SpriteRenderer renderer)
        {
            if (record == null || !record.hasCollision)
                return;

            if (spriteObject == null || renderer == null || renderer.sprite == null)
                return;

            BoxCollider2D collider = spriteObject.AddComponent<BoxCollider2D>();
            collider.size = renderer.sprite.bounds.size;
            collider.offset = renderer.sprite.bounds.center;
        }

        static GameObject CreatePrefabWrapper(LevelEditorObjectRecord record, GameObject prefab, LevelEditorAssetMetaData prefabMetaData)
        {
            GameObject wrapper = new GameObject(GetImportedObjectName(record, prefab.name));
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name;
            instance.transform.SetParent(wrapper.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            MatchPrefabVisualToEditorPreview(instance.transform, prefabMetaData);
            return wrapper;
        }

        static string GetImportedObjectName(LevelEditorObjectRecord record, string assetName)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.objectName) || IsGeneratedLevelEditorName(record.objectName))
                return string.IsNullOrWhiteSpace(assetName) ? "Level Object" : assetName;

            return record.objectName;
        }

        static bool IsGeneratedLevelEditorName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return true;

            return objectName.StartsWith("New Game Object", StringComparison.OrdinalIgnoreCase);
        }

        static void MatchPrefabVisualToEditorPreview(Transform prefabInstance, LevelEditorAssetMetaData prefabMetaData)
        {
            if (prefabInstance == null)
                return;

            Vector3 targetWorldPosition = prefabInstance.position;
            Bounds? currentBounds = GetPrefabPreviewBounds(prefabInstance, prefabMetaData);
            if (!currentBounds.HasValue)
                return;

            float targetLargestSide = GetEditorPreviewRawLargestSide(prefabMetaData)
                * GetLargestAbsXYScale(prefabInstance);
            Bounds bounds = currentBounds.Value;
            float currentLargestSide = Mathf.Max(bounds.size.x, bounds.size.y);

            if (targetLargestSide > 0f && currentLargestSide > 0f)
            {
                float scaleMultiplier = targetLargestSide / currentLargestSide;
                prefabInstance.localScale *= scaleMultiplier;
                currentBounds = GetPrefabPreviewBounds(prefabInstance, prefabMetaData);
                if (!currentBounds.HasValue)
                    return;

                bounds = currentBounds.Value;
            }

            prefabInstance.position += targetWorldPosition - bounds.center;
        }

        static Bounds? GetPrefabPreviewBounds(Transform prefabInstance, LevelEditorAssetMetaData prefabMetaData)
        {
            SpriteRenderer previewRenderer = FindPreviewSpriteRenderer(prefabInstance, prefabMetaData);
            if (previewRenderer != null)
                return previewRenderer.bounds;

            return GetRendererBounds(prefabInstance);
        }

        static SpriteRenderer FindPreviewSpriteRenderer(Transform prefabInstance, LevelEditorAssetMetaData prefabMetaData)
        {
            if (prefabInstance == null || prefabMetaData == null)
                return null;

            SpriteRenderer[] renderers = prefabInstance.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers == null || renderers.Length == 0)
                return null;

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || renderer.sprite == null)
                    continue;

                if (SpriteRectMatches(renderer.sprite.rect, prefabMetaData))
                    return renderer;
            }

            return renderers.Length == 1 ? renderers[0] : null;
        }

        static bool SpriteRectMatches(Rect rect, LevelEditorAssetMetaData metaData)
        {
            return metaData.SpriteRectWidth > 0f
                && metaData.SpriteRectHeight > 0f
                && Approximately(rect.x, metaData.SpriteRectX)
                && Approximately(rect.y, metaData.SpriteRectY)
                && Approximately(rect.width, metaData.SpriteRectWidth)
                && Approximately(rect.height, metaData.SpriteRectHeight);
        }

        static void MatchRootObjectVisualToEditorPreview(
            GameObject spawned,
            LevelEditorObjectRecord record,
            Dictionary<string, LevelEditorAssetMetaData> assetsById)
        {
            if (spawned == null
                || record == null
                || string.IsNullOrWhiteSpace(record.assetId)
                || !assetsById.TryGetValue(record.assetId, out LevelEditorAssetMetaData metaData))
            {
                return;
            }

            if (string.Equals(metaData.AssetType, LevelEditorImportedAssetTypes.Sprite, StringComparison.OrdinalIgnoreCase))
            {
                SpriteRenderer renderer = spawned.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    MatchSpriteVisualToEditorPreview(renderer, metaData);

                return;
            }

            if (string.Equals(metaData.AssetType, LevelEditorImportedAssetTypes.Prefab, StringComparison.OrdinalIgnoreCase))
                MatchPrefabVisualToEditorPreview(spawned.transform, metaData);
        }

        static void MatchSpriteVisualToEditorPreview(SpriteRenderer renderer, LevelEditorAssetMetaData spriteMetaData)
        {
            if (renderer == null || renderer.sprite == null)
                return;

            Vector3 targetWorldPosition = renderer.transform.position;
            float targetLargestSide = GetEditorPreviewRawLargestSide(spriteMetaData)
                * GetLargestAbsXYScale(renderer.transform);
            Vector2 currentSize = renderer.bounds.size;
            float currentLargestSide = Mathf.Max(currentSize.x, currentSize.y);

            if (targetLargestSide > 0f && currentLargestSide > 0f)
            {
                float scaleMultiplier = targetLargestSide / currentLargestSide;
                renderer.transform.localScale *= scaleMultiplier;
            }

            Vector3 visualCenter = renderer.transform.TransformPoint(renderer.sprite.bounds.center);
            renderer.transform.position += targetWorldPosition - visualCenter;
        }

        static Bounds? GetRendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds? bounds = null;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (bounds.HasValue)
                {
                    Bounds merged = bounds.Value;
                    merged.Encapsulate(renderer.bounds);
                    bounds = merged;
                }
                else
                {
                    bounds = renderer.bounds;
                }
            }

            return bounds;
        }

        static float GetEditorPreviewRawLargestSide(LevelEditorAssetMetaData metaData)
        {
            if (metaData == null || metaData.SpriteRectWidth <= 0f || metaData.SpriteRectHeight <= 0f)
                return 0f;

            float pixelsPerUnit = metaData.PixelsPerUnit > 0f ? metaData.PixelsPerUnit : 100f;
            return Mathf.Max(metaData.SpriteRectWidth, metaData.SpriteRectHeight) / pixelsPerUnit;
        }

        static float GetLargestAbsXYScale(Transform transform)
        {
            if (transform == null)
                return 1f;

            Vector3 scale = transform.lossyScale;
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        static bool TryResolvePrefab(
            LevelEditorObjectRecord record,
            Dictionary<string, LevelEditorAssetMetaData> assetsById,
            out GameObject prefab,
            out LevelEditorAssetMetaData prefabMetaData)
        {
            prefab = null;
            prefabMetaData = null;

            if (assetsById.TryGetValue(record.assetId ?? "", out LevelEditorAssetMetaData asset)
                && string.Equals(asset.AssetType, LevelEditorImportedAssetTypes.Prefab, StringComparison.OrdinalIgnoreCase)
                && TryLoadAssetAtRelativePath(asset.AssetRelativePath, out prefab))
            {
                prefabMetaData = asset;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(record.prefabGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(record.prefabGuid);
                if (!string.IsNullOrWhiteSpace(path))
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return prefab != null;
        }

        static bool TryResolveSprite(
            LevelEditorObjectRecord record,
            Dictionary<string, LevelEditorAssetMetaData> assetsById,
            out Sprite sprite,
            out LevelEditorAssetMetaData spriteMetaData)
        {
            sprite = null;
            spriteMetaData = null;
            if (!assetsById.TryGetValue(record.assetId ?? "", out LevelEditorAssetMetaData asset))
                return false;

            if (string.IsNullOrWhiteSpace(asset.AssetRelativePath))
                return false;

            sprite = LoadSpriteAtPath(asset.AssetRelativePath, asset);
            spriteMetaData = asset;
            return sprite != null;
        }

        static bool TryLoadAssetAtRelativePath<T>(string assetPath, out T asset)
            where T : UnityEngine.Object
        {
            asset = null;
            if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.Replace('\\', '/').StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return false;

            asset = AssetDatabase.LoadAssetAtPath<T>(assetPath.Replace('\\', '/'));
            return asset != null;
        }

        static Sprite LoadSpriteAtPath(string assetPath, LevelEditorAssetMetaData asset)
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            Sprite directSprite = AssetDatabase.LoadAssetAtPath<Sprite>(normalizedPath);
            if (directSprite != null)
                return directSprite;

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(normalizedPath)
                .OfType<Sprite>()
                .ToArray();

            if (sprites.Length == 0)
                return null;

            if (asset.SpriteRectWidth <= 0f || asset.SpriteRectHeight <= 0f)
                return sprites[0];

            return sprites.FirstOrDefault(sprite =>
                Approximately(sprite.rect.x, asset.SpriteRectX)
                && Approximately(sprite.rect.y, asset.SpriteRectY)
                && Approximately(sprite.rect.width, asset.SpriteRectWidth)
                && Approximately(sprite.rect.height, asset.SpriteRectHeight))
                ?? sprites[0];
        }

        static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.5f;
        }

        static void ApplyRecordTransform(Transform transform, Transform parent, LevelEditorObjectRecord record)
        {
            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(
                new Vector3(record.px, record.py, record.pz),
                new Quaternion(record.qx, record.qy, record.qz, record.qw));
            transform.localScale = new Vector3(record.sx, record.sy, record.sz);
        }

        static void ApplyRecordName(GameObject gameObject, LevelEditorObjectRecord record)
        {
            if (!string.IsNullOrWhiteSpace(record.objectName))
                gameObject.name = record.objectName;
        }

        static void ApplySortingOrder(GameObject gameObject, int sortingOrder)
        {
            SpriteRenderer[] renderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers == null || renderers.Length == 0)
                return;

            int lowestSortingOrder = renderers.Min(renderer => renderer.sortingOrder);
            int offset = sortingOrder - lowestSortingOrder;
            foreach (SpriteRenderer renderer in renderers)
                renderer.sortingOrder += offset;
        }

        static List<LevelEditorObjectRecord> SortRecordsParentBeforeChildren(List<LevelEditorObjectRecord> records)
        {
            if (records == null)
                return new List<LevelEditorObjectRecord>();

            HashSet<int> knownIds = new HashSet<int>(
                records.Where(record => record != null)
                    .Select(record => record.instanceId));
            List<LevelEditorObjectRecord> ordered = new List<LevelEditorObjectRecord>();
            HashSet<int> done = new HashSet<int>();

            int guard = 0;
            while (done.Count < records.Count && guard++ <= records.Count + 5)
            {
                int beforeCount = done.Count;
                foreach (LevelEditorObjectRecord record in records)
                {
                    if (record == null || done.Contains(record.instanceId))
                        continue;

                    bool parentReady = record.parentInstanceId < 0
                        || !knownIds.Contains(record.parentInstanceId)
                        || done.Contains(record.parentInstanceId);

                    if (!parentReady)
                        continue;

                    ordered.Add(record);
                    done.Add(record.instanceId);
                }

                if (done.Count == beforeCount)
                    break;
            }

            foreach (LevelEditorObjectRecord record in records)
            {
                if (record != null && !done.Contains(record.instanceId))
                    ordered.Add(record);
            }

            return ordered;
        }
    }
}
