using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LevelEditorJsonImporter;
using UnityEditor;
using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    public sealed class LevelJsonSceneImporterWindow : EditorWindow
    {
        const string ProjectRegistryFileName = "project_asset_registry.json";
        const string BundledRegistryRelativePath = "BundledAssets/asset_registry.json";

        string levelJsonPath = "";
        string importRootName = "Imported Level";
        bool clearExistingImportRoot = true;
        bool selectImportedRoot = true;

        [MenuItem("Tools/Level Editor JSON/Import Level JSON")]
        public static void Open()
        {
            GetWindow<LevelJsonSceneImporterWindow>("Level JSON Importer");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Level Editor JSON Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Imports a level.json from the Level Editor Tool. Assets resolve via project_asset_registry.json (Assets/... paths), bundled copies, or unity_project_link.json when the source Unity project moved to another PC.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                levelJsonPath = EditorGUILayout.TextField("Level JSON", levelJsonPath);
                if (GUILayout.Button("Browse", GUILayout.Width(80)))
                {
                    string selected = EditorUtility.OpenFilePanel("Select level.json", "", "json");
                    if (!string.IsNullOrEmpty(selected))
                        levelJsonPath = selected;
                }
            }

            importRootName = EditorGUILayout.TextField("Import Root Name", importRootName);
            clearExistingImportRoot = EditorGUILayout.ToggleLeft("Clear existing root with same name before import", clearExistingImportRoot);
            selectImportedRoot = EditorGUILayout.ToggleLeft("Select imported root after import", selectImportedRoot);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(levelJsonPath)))
            {
                if (GUILayout.Button("Import Into Current Scene", GUILayout.Height(30)))
                    Import();
            }
        }

        void Import()
        {
            try
            {
                LevelJsonSceneImporter.ImportFromPath(
                    levelJsonPath,
                    importRootName,
                    clearExistingImportRoot,
                    selectImportedRoot);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Import failed", ex.Message, "OK");
            }
        }

        static LevelEditorProjectFile LoadLevelFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("Level JSON not found.", path);

            return JsonUtility.FromJson<LevelEditorProjectFile>(File.ReadAllText(path));
        }

        Dictionary<string, LevelEditorAssetMetaData> LoadRegistries(string levelDirectory)
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

            if (registry?.Assets == null)
                return;

            foreach (LevelEditorAssetMetaData asset in registry.Assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.AssetID))
                    continue;

                assetsById[asset.AssetID] = asset;
            }
        }

        GameObject ImportLevel(LevelEditorProjectFile level, Dictionary<string, LevelEditorAssetMetaData> assetsById)
        {
            string rootName = string.IsNullOrWhiteSpace(importRootName)
                ? "Imported Level"
                : importRootName.Trim();

            if (clearExistingImportRoot)
            {
                GameObject existingRoot = GameObject.Find(rootName);
                if (existingRoot != null)
                    Undo.DestroyObjectImmediate(existingRoot);
            }

            GameObject root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Import Level JSON");

            Dictionary<int, Transform> spawnedById = new Dictionary<int, Transform>();
            foreach (LevelEditorObjectRecord record in SortRecordsParentBeforeChildren(level.objects))
            {
                Transform parent = root.transform;
                if (record.parentInstanceId >= 0 && spawnedById.TryGetValue(record.parentInstanceId, out Transform spawnedParent))
                    parent = spawnedParent;

                GameObject spawned = record.isGroup
                    ? CreateGroupObject(record)
                    : CreateLevelObject(record, assetsById);

                if (spawned == null)
                    continue;

                Undo.RegisterCreatedObjectUndo(spawned, "Import Level JSON");
                ApplyRecordTransform(spawned.transform, parent, record);
                ApplyRecordName(spawned, record);
                ApplySortingOrder(spawned, record.sortingOrder);
                spawnedById[record.instanceId] = spawned.transform;
            }

            return root;
        }

        static GameObject CreateGroupObject(LevelEditorObjectRecord record)
        {
            return new GameObject(string.IsNullOrWhiteSpace(record.objectName) ? "Group" : record.objectName);
        }

        GameObject CreateLevelObject(LevelEditorObjectRecord record, Dictionary<string, LevelEditorAssetMetaData> assetsById)
        {
            if (TryResolvePrefab(record, assetsById, out GameObject prefab))
                return (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            if (TryResolveSprite(record, assetsById, out Sprite sprite))
            {
                GameObject spriteObject = new GameObject(string.IsNullOrWhiteSpace(record.objectName) ? sprite.name : record.objectName);
                SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                return spriteObject;
            }

            Debug.LogWarning($"Could not resolve asset for record '{record.objectName}' (assetId: {record.assetId}, prefabGuid: {record.prefabGuid}). Created empty placeholder.");
            return new GameObject(string.IsNullOrWhiteSpace(record.objectName) ? "Missing Level Object" : record.objectName);
        }

        static bool TryResolvePrefab(LevelEditorObjectRecord record, Dictionary<string, LevelEditorAssetMetaData> assetsById, out GameObject prefab)
        {
            prefab = null;

            if (assetsById.TryGetValue(record.assetId ?? "", out LevelEditorAssetMetaData asset)
                && string.Equals(asset.AssetType, LevelEditorImportedAssetTypes.Prefab, StringComparison.OrdinalIgnoreCase)
                && TryLoadAssetAtRelativePath(asset.AssetRelativePath, out prefab))
            {
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

        static bool TryResolveSprite(LevelEditorObjectRecord record, Dictionary<string, LevelEditorAssetMetaData> assetsById, out Sprite sprite)
        {
            sprite = null;
            if (!assetsById.TryGetValue(record.assetId ?? "", out LevelEditorAssetMetaData asset))
                return false;

            if (string.IsNullOrWhiteSpace(asset.AssetRelativePath))
                return false;

            sprite = LoadSpriteAtPath(asset.AssetRelativePath, asset);
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
            Vector3 worldPosition = new Vector3(record.px, record.py, record.pz);
            Quaternion worldRotation = new Quaternion(record.qx, record.qy, record.qz, record.qw);
            Vector3 localScale = new Vector3(record.sx, record.sy, record.sz);

            transform.SetPositionAndRotation(worldPosition, worldRotation);
            transform.localScale = localScale;
            transform.SetParent(parent, true);
        }

        static void ApplyRecordName(GameObject gameObject, LevelEditorObjectRecord record)
        {
            if (!string.IsNullOrWhiteSpace(record.objectName))
                gameObject.name = record.objectName;
        }

        static void ApplySortingOrder(GameObject gameObject, int sortingOrder)
        {
            SpriteRenderer renderer = gameObject.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
                renderer.sortingOrder = sortingOrder;
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
