namespace DocumentManagement_Botones_Rovic_Joy.Data.Entities;

public sealed class Document
{
    public Guid DocumentId { get; set; }
    public string StoredFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DocumentMetadata Metadata { get; set; } = null!;
}
