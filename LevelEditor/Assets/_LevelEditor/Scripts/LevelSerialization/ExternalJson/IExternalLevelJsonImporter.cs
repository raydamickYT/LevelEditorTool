public interface IExternalLevelJsonImporter
{
    string FormatId { get; }
    string DisplayName { get; }

    bool CanImport(string json);

    ExternalLevelImportResult Import(string json, string sourcePath);
}
