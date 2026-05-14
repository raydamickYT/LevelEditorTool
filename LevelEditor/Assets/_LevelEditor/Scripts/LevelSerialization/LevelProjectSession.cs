using System.IO;

/// <summary>
/// Tracks the folder of the level project currently open in the tool (standalone runtime).
/// Registry and imports are session-only unless the user saves into a project folder.
/// </summary>
public static class LevelProjectSession
{
    public static string CurrentProjectDirectory { get; private set; }

    public static bool HasOpenProject => !string.IsNullOrEmpty(CurrentProjectDirectory);

    public static void SetProjectDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            CurrentProjectDirectory = null;
            return;
        }

        CurrentProjectDirectory = Path.GetFullPath(directoryPath);
    }

    public static void ClearProject()
    {
        CurrentProjectDirectory = null;
    }
}
