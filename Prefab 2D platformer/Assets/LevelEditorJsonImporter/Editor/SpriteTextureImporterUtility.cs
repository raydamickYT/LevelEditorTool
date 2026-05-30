using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LevelEditorJsonImporter.Editor
{
    static class SpriteTextureImporterUtility
    {
        public static void ApplyPhysicsShapeImportSettings(TextureImporter importer)
        {
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.isReadable = true;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);

            SerializedObject serializedImporter = new SerializedObject(importer);
            SerializedProperty generatePhysicsShape = serializedImporter.FindProperty("m_SpriteGenerateFallbackPhysicsShape");
            if (generatePhysicsShape != null)
                generatePhysicsShape.boolValue = true;

            SerializedProperty meshType = serializedImporter.FindProperty("m_TextureSettings.m_SpriteMeshType");
            if (meshType != null)
                meshType.intValue = (int)SpriteMeshType.Tight;

            serializedImporter.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static class SpriteEditorOutlineUtility
    {
        static readonly MethodInfo GenerateOutlineFromSpriteMethod = FindGenerateOutlineFromSpriteMethod();
        static readonly MethodInfo GenerateOutlineMethod = FindGenerateOutlineMethod();

        static MethodInfo FindGenerateOutlineFromSpriteMethod()
        {
            System.Type spriteUtilityType = System.Type.GetType("UnityEditor.Sprites.SpriteUtility, UnityEditor");
            if (spriteUtilityType == null)
                return null;

            foreach (MethodInfo method in spriteUtilityType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "GenerateOutlineFromSprite")
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 5 || parameters[0].ParameterType != typeof(Sprite))
                    continue;

                return method;
            }

            return null;
        }

        static MethodInfo FindGenerateOutlineMethod()
        {
            System.Type spriteUtilityType = System.Type.GetType("UnityEditor.Sprites.SpriteUtility, UnityEditor");
            if (spriteUtilityType == null)
                return null;

            foreach (MethodInfo method in spriteUtilityType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "GenerateOutline")
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 6 || parameters[0].ParameterType != typeof(Texture2D))
                    continue;

                return method;
            }

            return null;
        }

        public static bool TryGenerateOutlineFromSprite(
            Sprite sprite,
            float detail,
            byte alphaTolerance,
            bool holeDetection,
            out Vector2[][] paths)
        {
            paths = null;
            if (GenerateOutlineFromSpriteMethod == null || sprite == null)
                return false;

            ParameterInfo[] parameters = GenerateOutlineFromSpriteMethod.GetParameters();
            object[] args = new object[parameters.Length];
            args[0] = sprite;
            args[1] = detail;
            args[2] = alphaTolerance;
            args[3] = holeDetection;
            args[parameters.Length - 1] = null;

            GenerateOutlineFromSpriteMethod.Invoke(null, args);
            paths = args[parameters.Length - 1] as Vector2[][];
            return paths != null && paths.Length > 0;
        }

        public static bool TryGenerateOutline(
            Texture2D texture,
            Rect rect,
            float detail,
            byte alphaTolerance,
            bool holeDetection,
            out Vector2[][] paths)
        {
            paths = null;
            if (GenerateOutlineMethod == null || texture == null)
                return false;

            ParameterInfo[] parameters = GenerateOutlineMethod.GetParameters();
            object[] args = new object[parameters.Length];
            args[0] = texture;
            args[1] = rect;
            args[2] = detail;
            args[3] = alphaTolerance;
            args[4] = holeDetection;
            args[parameters.Length - 1] = null;

            GenerateOutlineMethod.Invoke(null, args);
            paths = args[parameters.Length - 1] as Vector2[][];
            return paths != null && paths.Length > 0;
        }
    }
}
