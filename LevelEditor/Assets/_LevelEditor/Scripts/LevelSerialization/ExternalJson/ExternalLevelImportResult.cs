using System.Collections.Generic;

public sealed class ExternalLevelImportResult
{
    public bool Success;
    public string FormatId;
    public string FormatDisplayName;
    public string SourcePath;
    public int SpawnedObjectCount;
    public readonly List<string> Warnings = new();
    public string ErrorMessage;
}
