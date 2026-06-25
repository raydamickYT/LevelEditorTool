using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SFB;
using UnityEngine;

public static class ExternalLevelJsonExportService
{
    static readonly IExternalLevelJsonExporter[] Exporters =
    {
        new PlatformerLevelJsonExporter(),
    };

    public static IReadOnlyList<IExternalLevelJsonExporter> RegisteredExporters => Exporters;

    public static IExternalLevelJsonExporter ResolveExporter()
    {
        if (ExternalLevelJsonImportSession.HasActiveImport)
        {
            IExternalLevelJsonExporter sessionExporter = Exporters.FirstOrDefault(
                exporter => string.Equals(exporter.FormatId, ExternalLevelJsonImportSession.FormatId, StringComparison.Ordinal));

            if (sessionExporter != null && sessionExporter.CanExport())
                return sessionExporter;
        }

        return Exporters.FirstOrDefault(exporter => exporter.CanExport());
    }

    public static ExternalLevelExportResult ExportActiveFormat()
    {
        IExternalLevelJsonExporter exporter = ResolveExporter();
        if (exporter == null)
        {
            return new ExternalLevelExportResult
            {
                Success = false,
                ErrorMessage = "No exporter found for the current level.",
            };
        }

        return exporter.Export();
    }

    public static ExternalLevelExportResult ExportToFile(string outputPath)
    {
        ExternalLevelExportResult result = ExportActiveFormat();
        if (!result.Success || string.IsNullOrWhiteSpace(result.Json))
            return result;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            result.Success = false;
            result.ErrorMessage = "Export path is empty.";
            return result;
        }

        string fullPath = Path.GetFullPath(outputPath);
        string directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, result.Json, Encoding.UTF8);
        result.OutputPath = fullPath;
        return result;
    }

    public static ExternalLevelExportResult PromptAndExportToFile()
    {
        IExternalLevelJsonExporter exporter = ResolveExporter();
        if (exporter == null)
        {
            return new ExternalLevelExportResult
            {
                Success = false,
                ErrorMessage = "No exporter found. Import a platformer JSON or place platformer placeholder sprites first.",
            };
        }

        string defaultDirectory = string.Empty;
        string defaultName = "level.json";

        if (ExternalLevelJsonImportSession.HasActiveImport
            && !string.IsNullOrEmpty(ExternalLevelJsonImportSession.SourceFilePath))
        {
            defaultDirectory = Path.GetDirectoryName(ExternalLevelJsonImportSession.SourceFilePath) ?? string.Empty;
            defaultName = Path.GetFileName(ExternalLevelJsonImportSession.SourceFilePath);
        }

        ExtensionFilter[] extensions =
        {
            new ExtensionFilter("JSON Files", "json"),
            new ExtensionFilter("All Files", "*"),
        };

        string outputPath = StandaloneFileBrowser.SaveFilePanel(
            $"Export {exporter.DisplayName}",
            defaultDirectory,
            defaultName,
            extensions);

        if (string.IsNullOrEmpty(outputPath))
        {
            return new ExternalLevelExportResult
            {
                Success = false,
                ErrorMessage = "Export cancelled.",
            };
        }

        if (!outputPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            outputPath += ".json";

        return ExportToFile(outputPath);
    }
}
