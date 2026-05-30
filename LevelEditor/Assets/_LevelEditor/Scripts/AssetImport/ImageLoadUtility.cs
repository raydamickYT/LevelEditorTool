using System.IO;
using UnityEngine;

/// <summary>
/// Loads image files the same way Unity's texture importer displays them (including JPEG EXIF orientation).
/// <see cref="Texture2D.LoadImage"/> ignores EXIF, which makes phone photos look rotated in the tool but correct in Unity.
/// </summary>
public static class ImageLoadUtility
{
    const ushort ExifOrientationTag = 0x0112;

    public static Texture2D LoadTextureFromBytes(byte[] fileBytes)
    {
        if (fileBytes == null || fileBytes.Length == 0)
            return null;

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(fileBytes))
        {
            Object.Destroy(texture);
            return null;
        }

        int orientation = TryReadJpegExifOrientation(fileBytes);
        if (orientation > 1)
            texture = ApplyExifOrientation(texture, orientation);

        return texture;
    }

    public static Texture2D LoadTextureFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        return LoadTextureFromBytes(File.ReadAllBytes(filePath));
    }

    public static Sprite CreateSpriteFromFile(string filePath, Rect rect, float pixelsPerUnit, string spriteName = null)
    {
        Texture2D texture = LoadTextureFromFile(filePath);
        if (texture == null)
            return null;

        rect = ClampRectToTexture(rect, texture);
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
        sprite.name = string.IsNullOrWhiteSpace(spriteName)
            ? Path.GetFileNameWithoutExtension(filePath)
            : spriteName;
        return sprite;
    }

    public static Rect ClampRectToTexture(Rect rect, Texture2D texture)
    {
        if (texture == null)
            return rect;

        if (rect.width <= 0f || rect.height <= 0f)
            return new Rect(0, 0, texture.width, texture.height);

        float x = Mathf.Clamp(rect.x, 0f, texture.width);
        float y = Mathf.Clamp(rect.y, 0f, texture.height);
        float width = Mathf.Min(rect.width, texture.width - x);
        float height = Mathf.Min(rect.height, texture.height - y);

        if (width <= 0f || height <= 0f)
            return new Rect(0, 0, texture.width, texture.height);

        return new Rect(x, y, width, height);
    }

    static int TryReadJpegExifOrientation(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            return 1;

        int offset = 2;
        while (offset + 4 < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
                return 1;

            byte marker = bytes[offset + 1];
            if (marker == 0xDA || marker == 0xD9)
                break;

            int segmentLength = (bytes[offset + 2] << 8) | bytes[offset + 3];
            if (segmentLength < 2 || offset + 2 + segmentLength > bytes.Length)
                return 1;

            if (marker == 0xE1 && offset + 10 < bytes.Length
                && bytes[offset + 4] == 'E'
                && bytes[offset + 5] == 'x'
                && bytes[offset + 6] == 'i'
                && bytes[offset + 7] == 'f'
                && bytes[offset + 8] == 0
                && bytes[offset + 9] == 0)
            {
                return ReadOrientationFromTiff(bytes, offset + 10);
            }

            offset += 2 + segmentLength;
        }

        return 1;
    }

    static int ReadOrientationFromTiff(byte[] bytes, int tiffStart)
    {
        if (tiffStart + 8 >= bytes.Length)
            return 1;

        bool littleEndian = bytes[tiffStart] == 0x49 && bytes[tiffStart + 1] == 0x49;
        bool bigEndian = bytes[tiffStart] == 0x4D && bytes[tiffStart + 1] == 0x4D;
        if (!littleEndian && !bigEndian)
            return 1;

        int ifdOffset = ReadInt32(bytes, tiffStart + 4, littleEndian);
        int ifdStart = tiffStart + ifdOffset;
        if (ifdStart + 2 >= bytes.Length)
            return 1;

        int entryCount = ReadUInt16(bytes, ifdStart, littleEndian);
        for (int i = 0; i < entryCount; i++)
        {
            int entry = ifdStart + 2 + i * 12;
            if (entry + 12 > bytes.Length)
                break;

            ushort tag = ReadUInt16(bytes, entry, littleEndian);
            if (tag != ExifOrientationTag)
                continue;

            ushort type = ReadUInt16(bytes, entry + 2, littleEndian);
            uint count = (uint)ReadInt32(bytes, entry + 4, littleEndian);
            if (type != 3 || count < 1)
                return 1;

            int value = count == 1
                ? ReadUInt16(bytes, entry + 8, littleEndian)
                : ReadUInt16(bytes, tiffStart + ReadInt32(bytes, entry + 8, littleEndian), littleEndian);

            return value is >= 1 and <= 8 ? value : 1;
        }

        return 1;
    }

    static Texture2D ApplyExifOrientation(Texture2D source, int orientation)
    {
        int width = source.width;
        int height = source.height;
        Color[] pixels = source.GetPixels();
        Color[] transformed;
        int newWidth;
        int newHeight;

        switch (orientation)
        {
            case 2:
                transformed = TransformPixels(pixels, width, height, width, height, (x, y) => (width - 1 - x, y));
                newWidth = width;
                newHeight = height;
                break;
            case 3:
                transformed = TransformPixels(pixels, width, height, width, height, (x, y) => (width - 1 - x, height - 1 - y));
                newWidth = width;
                newHeight = height;
                break;
            case 4:
                transformed = TransformPixels(pixels, width, height, width, height, (x, y) => (x, height - 1 - y));
                newWidth = width;
                newHeight = height;
                break;
            case 5:
                transformed = TransformPixels(pixels, width, height, height, width, (x, y) => (y, x));
                newWidth = height;
                newHeight = width;
                break;
            case 6:
                transformed = TransformPixels(pixels, width, height, height, width, (x, y) => (y, width - 1 - x));
                newWidth = height;
                newHeight = width;
                break;
            case 7:
                transformed = TransformPixels(pixels, width, height, height, width, (x, y) => (height - 1 - y, width - 1 - x));
                newWidth = height;
                newHeight = width;
                break;
            case 8:
                transformed = TransformPixels(pixels, width, height, height, width, (x, y) => (height - 1 - y, x));
                newWidth = height;
                newHeight = width;
                break;
            default:
                return source;
        }

        Object.Destroy(source);

        Texture2D result = new(newWidth, newHeight, TextureFormat.RGBA32, false);
        result.SetPixels(transformed);
        result.Apply();
        return result;
    }

    delegate (int x, int y) PixelMap(int x, int y);

    static Color[] TransformPixels(
        Color[] source,
        int sourceWidth,
        int sourceHeight,
        int destWidth,
        int destHeight,
        PixelMap map)
    {
        Color[] result = new Color[destWidth * destHeight];
        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                (int mappedX, int mappedY) = map(x, y);
                result[mappedY * destWidth + mappedX] = source[y * sourceWidth + x];
            }
        }

        return result;
    }

    static ushort ReadUInt16(byte[] bytes, int offset, bool littleEndian)
    {
        return (ushort)(littleEndian
            ? bytes[offset] | (bytes[offset + 1] << 8)
            : (bytes[offset] << 8) | bytes[offset + 1]);
    }

    static int ReadInt32(byte[] bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return bytes[offset]
                | (bytes[offset + 1] << 8)
                | (bytes[offset + 2] << 16)
                | (bytes[offset + 3] << 24);
        }

        return (bytes[offset] << 24)
            | (bytes[offset + 1] << 16)
            | (bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }
}
