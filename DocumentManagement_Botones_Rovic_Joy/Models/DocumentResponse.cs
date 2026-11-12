namespace DocumentManagement_Botones_Rovic_Joy.Models;

public sealed record DocumentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset UploadedAt);
