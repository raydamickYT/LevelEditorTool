/// <summary>
/// Remembers the last external JSON import for future export / profile mapping.
/// </summary>
public static class ExternalLevelJsonImportSession
{
    public static string SourceFilePath { get; private set; }
    public static string SourceJsonText { get; private set; }
    public static string FormatId { get; private set; }
    public static string FormatDisplayName { get; private set; }

    public static bool HasActiveImport =>
        !string.IsNullOrEmpty(SourceJsonText) && !string.IsNullOrEmpty(FormatId);

    public static void Set(string sourceFilePath, string sourceJsonText, string formatId, string formatDisplayName)
    {
        SourceFilePath = sourceFilePath ?? string.Empty;
        SourceJsonText = sourceJsonText ?? string.Empty;
        FormatId = formatId ?? string.Empty;
        FormatDisplayName = formatDisplayName ?? string.Empty;
    }

    public static void Clear()
    {
        SourceFilePath = string.Empty;
        SourceJsonText = string.Empty;
        FormatId = string.Empty;
        FormatDisplayName = string.Empty;
    }
}
