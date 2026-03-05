using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for document management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("customer/v{version:apiVersion}/documents")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentController"/> class
    /// </summary>
    /// <param name="documentService">Document service</param>
    /// <param name="logger">Logger instance</param>
    public DocumentController(
        IDocumentService documentService,
        ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    private string GetActorId() => User.FindFirst("sub")?.Value ?? "Unknown";
    private string GetActorType() => User.FindFirst("role")?.Value ?? "Unknown";

    /// <summary>
    /// Create a new document reference
    /// </summary>
    [HttpPost]
    [RequirePermission(CustomerPermissions.DocumentsCreate)]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
    {
        var actorId = GetActorId();
        var actorType = GetActorType();

        _logger.LogInformation("Creating document for owner {OwnerType}/{OwnerId} by {ActorType}/{ActorId}",
            request.OwnerType, request.OwnerId, actorType, actorId);

        // ModelState validation via DataAnnotations
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validation failed for create document request: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        try
        {
            var document = await _documentService.CreateAsync(request, actorId, actorType);

            _logger.LogInformation("Document {DocumentId} created successfully", document.Id);
            return CreatedAtAction(nameof(GetDocumentsByOwner),
                new { ownerType = document.OwnerType, ownerId = document.OwnerId },
                document);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Upload Service unavailable or invalid file reference");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get documents by owner
    /// </summary>
    [HttpGet]
    [RequirePermission(CustomerPermissions.DocumentsRead)]
    [ProducesResponseType(typeof(List<DocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDocumentsByOwner([FromQuery] string ownerType, [FromQuery] Guid ownerId)
    {
        var actorId = GetActorId();
        var actorType = GetActorType();

        _logger.LogInformation("Retrieving documents for owner {OwnerType}/{OwnerId} by {ActorType}/{ActorId}",
            ownerType, ownerId, actorType, actorId);

        var documents = await _documentService.GetByOwnerAsync(ownerType, ownerId);

        _logger.LogInformation("Retrieved {Count} documents for owner {OwnerType}/{OwnerId}",
            documents.Count, ownerType, ownerId);

        return Ok(documents);
    }

    /// <summary>
    /// Update document (versioning)
    /// </summary>
    [HttpPatch("{id}")]
    [RequirePermission(CustomerPermissions.DocumentsCreate)]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentRequest request)
    {
        var actorId = GetActorId();
        var actorType = GetActorType();

        _logger.LogInformation("Updating document {DocumentId} by {ActorType}/{ActorId}",
            id, actorType, actorId);

        // ModelState validation via DataAnnotations
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validation failed for update document request: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        try
        {
            var document = await _documentService.UpdateAsync(id, request, actorId, actorType);

            _logger.LogInformation("Document {DocumentId} updated successfully to version {Version}",
                id, document.Version);

            return Ok(document);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Document {DocumentId} not found", id);
            return NotFound(new { error = $"Document with ID {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid file reference for document {DocumentId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating document {DocumentId}", id);
            return Conflict(new { error = "The document was modified by another user. Please refresh and try again." });
        }
    }

    /// <summary>
    /// Mark document as complete
    /// </summary>
    [HttpPatch("{id}/complete")]
    [RequirePermission(CustomerPermissions.DocumentsCreate)]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteDocument(Guid id, [FromBody] CompleteDocumentRequest? request = null)
    {
        var actorId = GetActorId();
        var actorType = GetActorType();

        _logger.LogInformation("Marking document {DocumentId} as complete by {ActorType}/{ActorId}",
            id, actorType, actorId);

        try
        {
            var document = await _documentService.MarkCompleteAsync(
                id,
                request?.SignedBy,
                request?.SignedAt,
                actorId,
                actorType);

            _logger.LogInformation("Document {DocumentId} marked as complete", id);
            return Ok(document);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Document {DocumentId} not found", id);
            return NotFound(new { error = $"Document with ID {id} not found" });
        }
    }

    /// <summary>
    /// Delete document
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="request">Delete request containing xmin for concurrency control</param>
    [HttpDelete("{id}")]
    [RequirePermission(CustomerPermissions.DocumentsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDocument(Guid id, [FromBody] DeleteDocumentRequest request)
    {
        var actorId = GetActorId();
        var actorType = GetActorType();

        _logger.LogInformation("Deleting document {DocumentId} by {ActorType}/{ActorId}",
            id, actorType, actorId);

        try
        {
            await _documentService.DeleteAsync(id, request.xmin, actorId, actorType);
            _logger.LogInformation("Document {DocumentId} deleted successfully", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Document {DocumentId} not found", id);
            return NotFound(new { error = $"Document with ID {id} not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            return Conflict(new ErrorResponse { Code = "VERSION_CONFLICT", Message = ex.Message });
        }
    }
}
