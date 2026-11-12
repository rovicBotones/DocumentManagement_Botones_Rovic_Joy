using DocumentManagement_Botones_Rovic_Joy.Models;
using DocumentManagement_Botones_Rovic_Joy.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagement_Botones_Rovic_Joy.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private const long MultipartOverheadAllowance = 64 * 1024;
    private readonly IDocumentStorage _storage;
    private readonly DocumentUploadValidator _validator;

    public DocumentsController(IDocumentStorage storage, DocumentUploadValidator validator)
    {
        _storage = storage;
        _validator = validator;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(DocumentUploadValidator.MaximumFileSize + MultipartOverheadAllowance)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentUploadValidator.MaximumFileSize + MultipartOverheadAllowance)]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<DocumentResponse>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        var validationError = _validator.Validate(file);
        if (validationError is null && file is not null)
            validationError = await _validator.ValidateFileContentAsync(file, cancellationToken);
        if (validationError is not null)
        {
            var status = validationError.Contains("10 MB", StringComparison.Ordinal)
                ? StatusCodes.Status413PayloadTooLarge
                : validationError.StartsWith("Unsupported", StringComparison.Ordinal) || validationError.StartsWith("The content type", StringComparison.Ordinal) || validationError.StartsWith("The uploaded content", StringComparison.Ordinal)
                    ? StatusCodes.Status415UnsupportedMediaType
                    : StatusCodes.Status400BadRequest;

            return Problem(statusCode: status, title: "Upload rejected", detail: validationError);
        }

        var document = await _storage.SaveAsync(file!, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await _storage.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var document = await _storage.GetAsync(id, cancellationToken);
        return document is null ? NotFoundProblem(id) : Ok(document);
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await _storage.OpenReadAsync(id, cancellationToken);
        if (result is null)
            return NotFoundProblem(id);

        return File(result.Value.Content, result.Value.Metadata.ContentType, result.Value.Metadata.FileName);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _storage.DeleteAsync(id, cancellationToken))
            return NotFoundProblem(id);

        return NoContent();
    }

    private ObjectResult NotFoundProblem(Guid id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Document not found", detail: $"No document exists with ID '{id}'.");
}
