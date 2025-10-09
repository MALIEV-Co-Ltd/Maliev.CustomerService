using FluentValidation;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Controllers;

[ApiController]
[Route("v1/internal-notes")]
[Authorize(Policy = "EmployeeOrHigher")]
public class InternalNoteController : ControllerBase
{
    private readonly IInternalNoteService _internalNoteService;
    private readonly IValidator<CreateInternalNoteRequest> _createValidator;
    private readonly IValidator<UpdateInternalNoteRequest> _updateValidator;
    private readonly ILogger<InternalNoteController> _logger;

    public InternalNoteController(
        IInternalNoteService internalNoteService,
        IValidator<CreateInternalNoteRequest> createValidator,
        IValidator<UpdateInternalNoteRequest> updateValidator,
        ILogger<InternalNoteController> logger)
    {
        _internalNoteService = internalNoteService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    private string GetCreatedBy() => User.FindFirst("sub")?.Value ?? "Unknown";

    /// <summary>
    /// Create a new internal note
    /// </summary>
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

        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for create internal note request: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(validationResult.Errors);
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

        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for update internal note request: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(validationResult.Errors);
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
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInternalNote(Guid id)
    {
        var actorId = GetCreatedBy();

        _logger.LogInformation("Deleting internal note {NoteId} by {ActorId}", id, actorId);

        try
        {
            await _internalNoteService.DeleteAsync(id);
            _logger.LogInformation("Internal note {NoteId} deleted successfully", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Internal note {NoteId} not found", id);
            return NotFound(new { error = $"Internal note with ID {id} not found" });
        }
    }
}
