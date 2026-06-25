using UnityEngine;

public static class LevelViewportFrameUtil
{
    public static Bounds PixelRectToWorldBounds(float pixelX, float pixelY, float pixelWidth, float pixelHeight, float pixelScale)
    {
        float safeScale = Mathf.Max(0.0001f, pixelScale);
        float worldLeft = pixelX * safeScale;
        float worldRight = (pixelX + pixelWidth) * safeScale;
        float worldTop = -pixelY * safeScale;
        float worldBottom = -(pixelY + pixelHeight) * safeScale;

        Vector3 center = new Vector3(
            (worldLeft + worldRight) * 0.5f,
            (worldTop + worldBottom) * 0.5f,
            0f);

        Vector3 size = new Vector3(
            Mathf.Abs(worldRight - worldLeft),
            Mathf.Abs(worldTop - worldBottom),
            0f);

        return new Bounds(center, size);
    }

    public static void WorldBoundsToPixelRect(Bounds worldBounds, float pixelScale, out float pixelX, out float pixelY, out float pixelWidth, out float pixelHeight)
    {
        float safeScale = Mathf.Max(0.0001f, pixelScale);
        pixelWidth = worldBounds.size.x / safeScale;
        pixelHeight = worldBounds.size.y / safeScale;
        pixelX = worldBounds.min.x / safeScale;
        pixelY = -worldBounds.max.y / safeScale;
    }

    public static Vector2 WorldDeltaToPixelDelta(Vector2 worldDelta, float pixelScale)
    {
        float safeScale = Mathf.Max(0.0001f, pixelScale);
        return new Vector2(worldDelta.x / safeScale, -worldDelta.y / safeScale);
    }
}
