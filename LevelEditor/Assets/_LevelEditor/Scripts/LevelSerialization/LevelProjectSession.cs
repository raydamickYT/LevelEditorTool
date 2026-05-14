using System;
using System.IO;

/// <summary>
/// Tracks the folder of the level project currently open in the tool (standalone runtime).
/// Registry and imports are session-only unless the user saves into a project folder.
/// </summary>
public static class LevelProjectSession
{
    public static string CurrentProjectDirectory { get; private set; }

    /// <summary>Full path to the level JSON last loaded or saved (e.g. …/MyLevel/level.json).</summary>
    public static string CurrentLevelJsonPath { get; private set; }

    public static bool HasOpenProject => !string.IsNullOrEmpty(CurrentProjectDirectory);

    public static void SetProjectDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            CurrentProjectDirectory = null;
            CurrentLevelJsonPath = null;
            return;
        }

        CurrentProjectDirectory = Path.GetFullPath(directoryPath);
    }

    public static void SetCurrentLevelJsonPath(string levelJsonAbsolutePath)
    {
        CurrentLevelJsonPath = string.IsNullOrWhiteSpace(levelJsonAbsolutePath)
            ? null
            : Path.GetFullPath(levelJsonAbsolutePath);
    }

    public static bool IsSameLevelJsonLoaded(string levelJsonAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(CurrentLevelJsonPath) || string.IsNullOrWhiteSpace(levelJsonAbsolutePath))
            return false;

        return string.Equals(
            Path.GetFullPath(levelJsonAbsolutePath),
            CurrentLevelJsonPath,
            StringComparison.OrdinalIgnoreCase);
    }

    public static void ClearProject()
    {
        CurrentProjectDirectory = null;
        CurrentLevelJsonPath = null;
    }
}
