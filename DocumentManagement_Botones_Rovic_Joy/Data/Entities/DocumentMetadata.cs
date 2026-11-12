namespace DocumentManagement_Botones_Rovic_Joy.Data.Entities;

public sealed class DocumentMetadata
{
    public Guid DocumentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Document Document { get; set; } = null!;
}
