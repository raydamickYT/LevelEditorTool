using UnityEngine;

public static class ViewportOutlineColorUtil
{
    public static string ColorToHexRgb(Color color)
        => "#" + ColorUtility.ToHtmlStringRGB(color);

    public static bool TryParseHex(string text, Color fallback, out Color color)
    {
        color = fallback;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string value = text.Trim();
        if (!value.StartsWith("#"))
            value = "#" + value;

        if (!ColorUtility.TryParseHtmlString(value, out Color parsed))
            return false;

        if (value.Length == 7)
            parsed.a = fallback.a;

        color = parsed;
        return true;
    }
}
