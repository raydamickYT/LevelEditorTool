using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    static class SpritePhysicsColliderUtility
    {
        const float OutlineDetail = 0.25f;
        const byte OutlineAlphaTolerance = 200;

        public static void TryAddSpritePhysicsCollider(GameObject gameObject, Sprite sprite)
        {
            if (gameObject == null || sprite == null)
                return;

            Sprite resolvedSprite = EnsureSpriteHasPhysicsShape(sprite);

            if (TryAddPolygonFromPhysicsShapes(gameObject, resolvedSprite))
                return;

            if (TryAddPolygonFromGeneratedOutline(gameObject, resolvedSprite))
                return;

            Debug.LogWarning(
                $"Level import: could not build polygon collider for '{resolvedSprite.name}'. "
                + "Reimport the sprite with Generate Physics Shape enabled, or enable Read/Write on the texture.",
                gameObject);

            AddBoxFallback(gameObject, resolvedSprite);
        }

        static Sprite EnsureSpriteHasPhysicsShape(Sprite sprite)
        {
            if (sprite == null)
                return null;

            if (sprite.GetPhysicsShapeCount() > 0)
                return sprite;

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrWhiteSpace(assetPath))
                return sprite;

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return sprite;

            SpriteTextureImporterUtility.ApplyPhysicsShapeImportSettings(importer);
            importer.SaveAndReimport();

            Sprite reloaded = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            return reloaded != null ? reloaded : sprite;
        }

        static bool TryAddPolygonFromPhysicsShapes(GameObject gameObject, Sprite sprite)
        {
            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount <= 0)
                return false;

            List<Vector2[]> validPaths = new List<Vector2[]>();
            List<Vector2> points = new List<Vector2>();

            for (int i = 0; i < shapeCount; i++)
            {
                points.Clear();
                sprite.GetPhysicsShape(i, points);
                if (points.Count < 3)
                    continue;

                validPaths.Add(points.ToArray());
            }

            return TryApplyPaths(gameObject, validPaths);
        }

        static bool TryAddPolygonFromGeneratedOutline(GameObject gameObject, Sprite sprite)
        {
            if (sprite.texture == null)
                return false;

            List<Vector2[]> validPaths = new List<Vector2[]>();

            if (SpriteEditorOutlineUtility.TryGenerateOutlineFromSprite(
                    sprite,
                    OutlineDetail,
                    OutlineAlphaTolerance,
                    holeDetection: false,
                    out Vector2[][] spritePaths))
            {
                CollectValidPaths(validPaths, spritePaths);
            }

            if (validPaths.Count > 0)
                return TryApplyPaths(gameObject, validPaths);

            if (SpriteEditorOutlineUtility.TryGenerateOutline(
                    sprite.texture,
                    sprite.rect,
                    OutlineDetail,
                    OutlineAlphaTolerance,
                    holeDetection: false,
                    out Vector2[][] texturePaths))
            {
                CollectValidPaths(validPaths, ConvertTexturePathsToLocal(sprite, texturePaths));
            }

            return TryApplyPaths(gameObject, validPaths);
        }

        static void CollectValidPaths(List<Vector2[]> destination, Vector2[][] source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null || source[i].Length < 3)
                    continue;

                destination.Add(source[i]);
            }
        }

        static Vector2[][] ConvertTexturePathsToLocal(Sprite sprite, Vector2[][] texturePaths)
        {
            if (texturePaths == null)
                return Array.Empty<Vector2[]>();

            Vector2[][] localPaths = new Vector2[texturePaths.Length][];
            for (int i = 0; i < texturePaths.Length; i++)
            {
                if (texturePaths[i] == null)
                    continue;

                Vector2[] local = new Vector2[texturePaths[i].Length];
                for (int j = 0; j < texturePaths[i].Length; j++)
                {
                    Vector2 point = texturePaths[i][j];
                    local[j] = (point - sprite.pivot) / sprite.pixelsPerUnit;
                }

                localPaths[i] = local;
            }

            return localPaths;
        }

        static bool TryApplyPaths(GameObject gameObject, List<Vector2[]> validPaths)
        {
            if (validPaths == null || validPaths.Count == 0)
                return false;

            PolygonCollider2D polygon = gameObject.AddComponent<PolygonCollider2D>();
            polygon.pathCount = validPaths.Count;
            for (int i = 0; i < validPaths.Count; i++)
                polygon.SetPath(i, validPaths[i]);

            return true;
        }

        static void AddBoxFallback(GameObject gameObject, Sprite sprite)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.size = sprite.bounds.size;
            box.offset = sprite.bounds.center;
        }
    }
}
