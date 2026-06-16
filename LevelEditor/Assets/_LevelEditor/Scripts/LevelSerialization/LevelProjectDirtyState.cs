using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Tracks whether the open level has unsaved changes.
/// </summary>
public static class LevelProjectDirtyState
{
    static bool _isDirty;
    static string _savedBaselineHash;

    public static bool HasUnsavedChanges()
    {
        if (!LevelProjectSession.HasOpenProject)
            return SceneHasLevelObjects();

        if (_savedBaselineHash == null)
            return _isDirty;

        return ComputeHash(LevelProjectService.BuildCurrentLevelJson()) != _savedBaselineHash;
    }

    static bool SceneHasLevelObjects()
    {
        foreach (var kv in ObjectRegistry.objects)
        {
            if (kv.Value != null)
                return true;
        }

        return false;
    }

    public static void SetSavedBaseline(string levelJson)
    {
        _savedBaselineHash = string.IsNullOrEmpty(levelJson) ? null : ComputeHash(levelJson);
        _isDirty = false;
    }

    public static void SetSavedBaselineFromCurrentScene()
        => SetSavedBaseline(LevelProjectService.BuildCurrentLevelJson());

    public static void MarkDirty() => _isDirty = true;

    public static void MarkClean()
    {
        _isDirty = false;
        _savedBaselineHash = null;
    }

    static string ComputeHash(string text)
    {
        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
