using DocumentManagement_Botones_Rovic_Joy.Data;
using DocumentManagement_Botones_Rovic_Joy.Data.Entities;
using DocumentManagement_Botones_Rovic_Joy.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement_Botones_Rovic_Joy.Services;

public sealed class LocalDocumentStorage : IDocumentStorage
{
    private const string StoredFileExtension = ".data";
    private readonly string _rootPath;
    private readonly DocumentManagementDbContext _dbContext;

    public LocalDocumentStorage(IWebHostEnvironment environment, DocumentManagementDbContext dbContext)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "App_Data", "Documents");
        Directory.CreateDirectory(_rootPath);
        _dbContext = dbContext;
    }

    public async Task<DocumentResponse> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var storedFileName = id.ToString("N") + StoredFileExtension;
        var contentPath = GetContentPath(storedFileName);
        var temporaryPath = contentPath + ".tmp";

        try
        {
            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await file.CopyToAsync(output, cancellationToken);

            File.Move(temporaryPath, contentPath);
            var entity = new Document
            {
                DocumentId = id,
                StoredFileName = storedFileName,
                FileSize = file.Length,
                UploadedAt = DateTimeOffset.UtcNow,
                Metadata = new DocumentMetadata
                {
                    DocumentId = id,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    ContentType = file.ContentType
                }
            };

            _dbContext.Documents.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToResponse(entity);
        }
        catch
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            if (File.Exists(contentPath))
                File.Delete(contentPath);
            throw;
        }
    }

    public async Task<IReadOnlyList<DocumentResponse>> ListAsync(CancellationToken cancellationToken) =>
        await _dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => new DocumentResponse(
                document.DocumentId,
                document.Metadata.OriginalFileName,
                document.Metadata.ContentType,
                document.FileSize,
                document.UploadedAt))
            .ToArrayAsync(cancellationToken);

    public async Task<DocumentResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .Include(item => item.Metadata)
            .SingleOrDefaultAsync(item => item.DocumentId == id, cancellationToken);

        return document is null ? null : ToResponse(document);
    }

    public async Task<(DocumentResponse Metadata, Stream Content)?> OpenReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .Include(item => item.Metadata)
            .SingleOrDefaultAsync(item => item.DocumentId == id, cancellationToken);

        if (document is null)
            return null;

        var contentPath = GetContentPath(document.StoredFileName);
        if (!File.Exists(contentPath))
            return null;

        return (ToResponse(document), new FileStream(contentPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _dbContext.Documents
            .SingleOrDefaultAsync(item => item.DocumentId == id, cancellationToken);

        if (document is null)
            return false;

        var contentPath = GetContentPath(document.StoredFileName);
        _dbContext.Documents.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (File.Exists(contentPath))
            File.Delete(contentPath);

        return true;
    }

    private string GetContentPath(string storedFileName)
    {
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(storedFileName), "N", out var id) ||
            !string.Equals(storedFileName, id.ToString("N") + StoredFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The stored file identifier is invalid.");
        }

        return Path.Combine(_rootPath, id.ToString("N") + StoredFileExtension);
    }

    private static DocumentResponse ToResponse(Document document) => new(
        document.DocumentId,
        document.Metadata.OriginalFileName,
        document.Metadata.ContentType,
        document.FileSize,
        document.UploadedAt);
}
