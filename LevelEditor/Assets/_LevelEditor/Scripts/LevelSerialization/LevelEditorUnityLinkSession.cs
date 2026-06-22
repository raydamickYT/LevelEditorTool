using System.IO;

/// <summary>
/// Tracks the Unity game project successfully linked in the current tool session.
/// Assets from the Unity project are only loaded after a successful link (plugin present).
/// </summary>
public static class LevelEditorUnityLinkSession
{
    public static string LinkedUnityProjectRoot { get; private set; }

    public static bool HasLinkedUnityProject =>
        !string.IsNullOrWhiteSpace(LinkedUnityProjectRoot) && Directory.Exists(LinkedUnityProjectRoot);

    public static void SetLinked(string absoluteUnityProjectRoot)
    {
        LinkedUnityProjectRoot = string.IsNullOrWhiteSpace(absoluteUnityProjectRoot)
            ? null
            : Path.GetFullPath(absoluteUnityProjectRoot);
    }

    public static void Clear()
    {
        LinkedUnityProjectRoot = null;
    }
}
