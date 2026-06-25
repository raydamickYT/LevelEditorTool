public interface IExternalLevelJsonExporter
{
    string FormatId { get; }
    string DisplayName { get; }

    bool CanExport();

    ExternalLevelExportResult Export();
}
