using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.CustomerService.Api.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for internal note management operations
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("customer/v{version:apiVersion}/internal-notes")]
public class InternalNoteController : ControllerBase
{
    private readonly IInternalNoteService _internalNoteService;
    private readonly ILogger<InternalNoteController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InternalNoteController"/> class
    /// </summary>
    /// <param name="internalNoteService">Internal note service</param>
    /// <param name="logger">Logger instance</param>
    public InternalNoteController(
        IInternalNoteService internalNoteService,
        ILogger<InternalNoteController> logger)
    {
        _internalNoteService = internalNoteService;
        _logger = logger;
    }

    private string GetCreatedBy() => User.FindFirst("sub")?.Value ?? "Unknown";

    /// <summary>
    /// Create a new internal note
    /// </summary>
    [RequirePermission(CustomerPermissions.NotesCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(InternalNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInternalNote([FromBody] CreateInternalNoteRequest request)
    {
        var createdBy = GetCreatedBy();

        _logger.LogInformation("Creating internal note for owner {OwnerType}/{OwnerId} by {CreatedBy}",
            request.OwnerType, request.OwnerId, createdBy);

        // ModelState validation via DataAnnotations
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validation failed for create internal note request: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        try
        {
            var note = await _internalNoteService.CreateAsync(request, createdBy);

            _logger.LogInformation("Internal note {NoteId} created successfully", note.Id);
            return CreatedAtAction(nameof(GetInternalNotesByOwner),
                new { ownerType = note.OwnerType, ownerId = note.OwnerId },
                note);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating internal note");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the internal note" });
        }
    }

    /// <summary>
    /// Get internal notes by owner
    /// </summary>
    [RequirePermission(CustomerPermissions.NotesRead)]
    [HttpGet]
    [ProducesResponseType(typeof(List<InternalNoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInternalNotesByOwner([FromQuery] string ownerType, [FromQuery] Guid ownerId)
    {
        var createdBy = GetCreatedBy();

        _logger.LogInformation("Retrieving internal notes for owner {OwnerType}/{OwnerId} by {CreatedBy}",
            ownerType, ownerId, createdBy);

        var notes = await _internalNoteService.GetByOwnerAsync(ownerType, ownerId);

        _logger.LogInformation("Retrieved {Count} internal notes for owner {OwnerType}/{OwnerId}",
            notes.Count, ownerType, ownerId);

        return Ok(notes);
    }

    /// <summary>
    /// Update an internal note
    /// </summary>
    [RequirePermission(CustomerPermissions.NotesUpdate)]
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(InternalNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateInternalNote(Guid id, [FromBody] UpdateInternalNoteRequest request)
    {
        var actorId = GetCreatedBy();

        _logger.LogInformation("Updating internal note {NoteId} by {ActorId}", id, actorId);

        // ModelState validation via DataAnnotations
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validation failed for update internal note request: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        try
        {
            var note = await _internalNoteService.UpdateAsync(id, request, actorId);

            _logger.LogInformation("Internal note {NoteId} updated successfully", id);
            return Ok(note);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Internal note {NoteId} not found", id);
            return NotFound(new { error = $"Internal note with ID {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating internal note {NoteId}", id);
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an internal note
    /// </summary>
    [RequirePermission(CustomerPermissions.NotesDelete)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteInternalNote(Guid id, [FromBody] DeleteInternalNoteRequest request)
    {
        var actorId = GetCreatedBy();

        _logger.LogInformation("Deleting internal note {NoteId} by {ActorId}", id, actorId);

        try
        {
            await _internalNoteService.DeleteAsync(id, request.xmin);
            _logger.LogInformation("Internal note {NoteId} deleted successfully", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Internal note {NoteId} not found", id);
            return NotFound(new { error = $"Internal note with ID {id} not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            return Conflict(new ErrorResponse { Code = "VERSION_CONFLICT", Message = ex.Message });
        }
    }

    /// <summary>
    /// Adds a comment to an internal note
    /// </summary>
    [RequirePermission(CustomerPermissions.NotesUpdate)]
    [HttpPost("{id}/comments")]
    [ProducesResponseType(typeof(InternalNoteCommentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateInternalNoteCommentRequest request)
    {
        try
        {
            var actorId = GetCreatedBy();
            var comment = await _internalNoteService.AddCommentAsync(id, request, actorId);
            return CreatedAtAction(nameof(GetComments), new { id }, comment);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Internal note not found" });
        }
    }

    /// <summary>
    /// Gets all comments for an internal note
    /// </summary>
    [RequirePermission(CustomerPermissions.NotesRead)]
    [HttpGet("{id}/comments")]
    [ProducesResponseType(typeof(List<InternalNoteCommentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComments(Guid id)
    {
        var comments = await _internalNoteService.GetCommentsAsync(id);
        return Ok(comments);
    }

    /// <summary>
    /// Gets combined activity for an internal note
    /// </summary>
    [RequirePermission(CustomerPermissions.NotesRead)]
    [HttpGet("{id}/activity")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivity(Guid id)
    {
        var activity = await _internalNoteService.GetNoteActivityAsync(id);
        return Ok(activity);
    }
}
