using System.Text;
using DocumentManagement_Botones_Rovic_Joy.Controllers;
using DocumentManagement_Botones_Rovic_Joy.Models;
using DocumentManagement_Botones_Rovic_Joy.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocumentManagement_Botones_Rovic_Joy.Tests;

public sealed class DocumentsControllerTests
{
    private static readonly byte[] ValidPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nTest PDF");

    [Fact]
    public async Task Upload_ValidPdf_ReturnsCreatedDocument()
    {
        var storage = new InMemoryDocumentStorage();
        var controller = CreateController(storage);

        var action = await controller.Upload(CreateFile("report.pdf", "application/pdf", ValidPdf), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(action.Result);
        var document = Assert.IsType<DocumentResponse>(created.Value);
        Assert.Equal("report.pdf", document.FileName);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(ValidPdf.Length, document.Size);
    }

    [Fact]
    public async Task Upload_MissingFile_ReturnsBadRequest()
    {
        var controller = CreateController(new InMemoryDocumentStorage());

        var action = await controller.Upload(null, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<ObjectResult>(action.Result).StatusCode);
    }

    [Fact]
    public async Task Upload_UnsupportedExtension_ReturnsUnsupportedMediaType()
    {
        var controller = CreateController(new InMemoryDocumentStorage());

        var action = await controller.Upload(CreateFile("payload.exe", "application/octet-stream", [1, 2, 3]), CancellationToken.None);

        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, Assert.IsType<ObjectResult>(action.Result).StatusCode);
    }

    [Fact]
    public async Task Upload_ContentTypeMismatch_ReturnsUnsupportedMediaType()
    {
        var controller = CreateController(new InMemoryDocumentStorage());

        var action = await controller.Upload(CreateFile("report.pdf", "text/plain", ValidPdf), CancellationToken.None);

        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, Assert.IsType<ObjectResult>(action.Result).StatusCode);
    }

    [Fact]
    public async Task Upload_InvalidPdfSignature_ReturnsUnsupportedMediaType()
    {
        var controller = CreateController(new InMemoryDocumentStorage());

        var action = await controller.Upload(CreateFile("report.pdf", "application/pdf", Encoding.ASCII.GetBytes("not a PDF")), CancellationToken.None);

        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, Assert.IsType<ObjectResult>(action.Result).StatusCode);
    }

    [Fact]
    public async Task Upload_OverTenMegabytes_ReturnsPayloadTooLarge()
    {
        var controller = CreateController(new InMemoryDocumentStorage());

        var action = await controller.Upload(
            CreateFile("large.pdf", "application/pdf", new byte[DocumentUploadValidator.MaximumFileSize + 1]),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, Assert.IsType<ObjectResult>(action.Result).StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingDocument_ReturnsMetadata()
    {
        var storage = new InMemoryDocumentStorage();
        var controller = CreateController(storage);
        var upload = await controller.Upload(CreateFile("report.pdf", "application/pdf", ValidPdf), CancellationToken.None);
        var id = Assert.IsType<DocumentResponse>(Assert.IsType<CreatedAtActionResult>(upload.Result).Value).Id;

        var action = await controller.GetById(id, CancellationToken.None);

        Assert.Equal(id, Assert.IsType<DocumentResponse>(Assert.IsType<OkObjectResult>(action.Result).Value).Id);
    }

    [Fact]
    public async Task GetById_MissingDocument_ReturnsNotFound()
    {
        var controller = CreateController(new InMemoryDocumentStorage());

        var action = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<ObjectResult>(action.Result).StatusCode);
    }

    [Fact]
    public async Task List_ReturnsUploadedDocuments()
    {
        var storage = new InMemoryDocumentStorage();
        var controller = CreateController(storage);
        await controller.Upload(CreateFile("report.pdf", "application/pdf", ValidPdf), CancellationToken.None);

        var action = await controller.List(CancellationToken.None);

        var documents = Assert.IsType<OkObjectResult>(action.Result).Value as IReadOnlyList<DocumentResponse>;
        Assert.NotNull(documents);
        Assert.Single(documents);
        Assert.Equal("report.pdf", documents[0].FileName);
    }

    [Fact]
    public async Task Download_ReturnsFileContentAndType()
    {
        var storage = new InMemoryDocumentStorage();
        var controller = CreateController(storage);
        var upload = await controller.Upload(CreateFile("report.pdf", "application/pdf", ValidPdf), CancellationToken.None);
        var id = Assert.IsType<DocumentResponse>(Assert.IsType<CreatedAtActionResult>(upload.Result).Value).Id;

        var action = await controller.Download(id, CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(action);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("report.pdf", file.FileDownloadName);
        using var downloaded = new MemoryStream();
        await file.FileStream.CopyToAsync(downloaded);
        Assert.Equal(ValidPdf, downloaded.ToArray());
    }

    [Fact]
    public async Task Delete_ExistingDocument_RemovesIt()
    {
        var storage = new InMemoryDocumentStorage();
        var controller = CreateController(storage);
        var upload = await controller.Upload(CreateFile("report.pdf", "application/pdf", ValidPdf), CancellationToken.None);
        var id = Assert.IsType<DocumentResponse>(Assert.IsType<CreatedAtActionResult>(upload.Result).Value).Id;

        var action = await controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(action);
        Assert.Null(await storage.GetAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_MissingDocument_ReturnsNotFound()
    {
        var controller = CreateController(new InMemoryDocumentStorage());

        var action = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<ObjectResult>(action).StatusCode);
    }

    private static DocumentsController CreateController(InMemoryDocumentStorage storage)
    {
        var services = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
        return new DocumentsController(storage, new DocumentUploadValidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services }
            }
        };
    }

    private static FormFile CreateFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class InMemoryDocumentStorage : IDocumentStorage
    {
        private readonly Dictionary<Guid, (DocumentResponse Metadata, byte[] Content)> _documents = [];

        public async Task<DocumentResponse> SaveAsync(IFormFile file, CancellationToken cancellationToken)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            var document = new DocumentResponse(Guid.NewGuid(), Path.GetFileName(file.FileName), file.ContentType, file.Length, DateTimeOffset.UtcNow);
            _documents.Add(document.Id, (document, stream.ToArray()));
            return document;
        }

        public Task<IReadOnlyList<DocumentResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DocumentResponse>>(_documents.Values.Select(item => item.Metadata).ToArray());

        public Task<DocumentResponse?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_documents.TryGetValue(id, out var document) ? document.Metadata : null);

        public Task<(DocumentResponse Metadata, Stream Content)?> OpenReadAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!_documents.TryGetValue(id, out var document))
                return Task.FromResult<(DocumentResponse Metadata, Stream Content)?>(null);

            return Task.FromResult<(DocumentResponse Metadata, Stream Content)?>(
                (document.Metadata, new MemoryStream(document.Content, writable: false)));
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_documents.Remove(id));
    }
}
