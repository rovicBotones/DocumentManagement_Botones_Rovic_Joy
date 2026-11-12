using DocumentManagement_Botones_Rovic_Joy.Models;

namespace DocumentManagement_Botones_Rovic_Joy.Services;

public interface IDocumentStorage
{
    Task<DocumentResponse> SaveAsync(IFormFile file, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentResponse>> ListAsync(CancellationToken cancellationToken);
    Task<DocumentResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<(DocumentResponse Metadata, Stream Content)?> OpenReadAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
