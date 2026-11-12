using System.IO.Compression;
using Microsoft.AspNetCore.Http;

namespace DocumentManagement_Botones_Rovic_Joy.Services;

public sealed class DocumentUploadValidator
{
    public const long MaximumFileSize = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> SupportedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv"
        };

    public string? Validate(IFormFile? file)
    {
        if (file is null)
            return "A file is required.";

        if (file.Length == 0)
            return "The uploaded file is empty.";

        if (file.Length > MaximumFileSize)
            return "The file exceeds the 10 MB per-file limit.";

        if (string.IsNullOrWhiteSpace(file.FileName))
            return "A filename is required.";

        var extension = Path.GetExtension(file.FileName);
        if (!SupportedTypes.TryGetValue(extension, out var expectedContentType))
            return "Unsupported file type. Supported types: PDF, DOCX, XLSX, PPTX, TXT, and CSV.";

        if (!string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            return $"The content type does not match the {extension} file extension.";

        return null;
    }

    public async Task<string?> ValidateFileContentAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        await using var stream = file.OpenReadStream();

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            var header = new byte[Math.Min(1024, (int)file.Length)];
            var bytesRead = await stream.ReadAsync(header, cancellationToken);
            if (!header.AsSpan(0, bytesRead).IndexOf("%PDF-"u8).Equals(-1))
                return null;

            return "The uploaded content is not a valid PDF file.";
        }

        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase))
        {
            var requiredEntry = extension.ToLowerInvariant() switch
            {
                ".docx" => "word/document.xml",
                ".xlsx" => "xl/workbook.xml",
                _ => "ppt/presentation.xml"
            };

            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                if (archive.GetEntry(requiredEntry) is not null)
                    return null;
            }
            catch (InvalidDataException)
            {
                // Report malformed archives using the same useful validation error below.
            }

            return $"The uploaded content is not a valid {extension} document.";
        }

        var sample = new byte[Math.Min(8192, (int)file.Length)];
        var sampleLength = await stream.ReadAsync(sample, cancellationToken);
        return sample.AsSpan(0, sampleLength).Contains((byte)0)
            ? "Text uploads cannot contain binary null bytes."
            : null;
    }
}
