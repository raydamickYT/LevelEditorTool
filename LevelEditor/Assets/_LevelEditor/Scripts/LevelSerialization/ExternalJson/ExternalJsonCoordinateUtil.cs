using UnityEngine;

public static class ExternalJsonCoordinateUtil
{
    public const float DefaultPixelScale = 0.01f;

    public static Vector2 WorldCenterToPixelPoint(Vector3 worldPosition, float pixelScale = DefaultPixelScale)
    {
        float safeScale = Mathf.Max(0.0001f, pixelScale);
        return new Vector2(
            worldPosition.x / safeScale,
            -worldPosition.y / safeScale);
    }

    public static void WorldBoundsToPixelRect(Bounds worldBounds, float pixelScale, out float x, out float y, out float width, out float height)
    {
        float safeScale = Mathf.Max(0.0001f, pixelScale);
        width = worldBounds.size.x / safeScale;
        height = worldBounds.size.y / safeScale;

        Vector2 center = WorldCenterToPixelPoint(worldBounds.center, safeScale);
        x = center.x - width * 0.5f;
        y = center.y - height * 0.5f;
    }

    public static bool TryGetWorldSpriteBounds(GameObject go, out Bounds bounds)
    {
        bounds = default;
        if (go == null)
            return false;

        if (go.TryGetComponent(out SpriteRenderer spriteRenderer) && spriteRenderer.sprite != null)
        {
            bounds = spriteRenderer.bounds;
            return true;
        }

        Collider2D collider = go.GetComponent<Collider2D>();
        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }

        bounds = new Bounds(go.transform.position, Vector3.zero);
        return true;
    }

    public static int RoundPixel(float value) => Mathf.RoundToInt(value);
}
