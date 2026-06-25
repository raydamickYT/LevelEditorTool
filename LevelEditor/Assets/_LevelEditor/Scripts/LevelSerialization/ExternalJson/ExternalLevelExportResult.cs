using System.Collections.Generic;

public sealed class ExternalLevelExportResult
{
    public bool Success;
    public string FormatId;
    public string FormatDisplayName;
    public string OutputPath;
    public string Json;
    public int ExportedObjectCount;
    public readonly List<string> Warnings = new();
    public string ErrorMessage;
}
