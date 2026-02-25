using Asp.Versioning;
using Maliev.CustomerService.Api.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Application.DTOs;
using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for company document management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("customer/v{version:apiVersion}/companies/{companyId:guid}/documents")]
[Authorize]
public class CompanyDocumentsController : ControllerBase
{
    private readonly ICompanyDocumentRepository _repository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<CompanyDocumentsController> _logger;

    /// <summary>
    /// Initializes a new instance of CompanyDocumentsController
    /// </summary>
    /// <param name="repository">Document repository</param>
    /// <param name="companyRepository">Company repository</param>
    /// <param name="logger">Logger instance</param>
    public CompanyDocumentsController(
        ICompanyDocumentRepository repository,
        ICompanyRepository companyRepository,
        ILogger<CompanyDocumentsController> logger)
    {
        _repository = repository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all documents for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    /// <response code="200">Documents retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Company not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CompanyDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CompanyDocumentResponse>>> GetAll(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Company with ID '{companyId}' not found",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var documents = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);

        var response = documents.Select(d => new CompanyDocumentResponse
        {
            Id = d.Id,
            CompanyId = d.CompanyId,
            DocumentType = d.DocumentType,
            FileName = d.FileName,
            FileUrl = d.FileUrl,
            ExpiryDate = d.ExpiryDate,
            CreatedAt = d.CreatedAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Creates a new document for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="request">Document creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document</returns>
    /// <response code="201">Document created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Company not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(CompanyDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDocumentResponse>> Create(Guid companyId, [FromBody] CreateCompanyDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "Invalid request data",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Company with ID '{companyId}' not found",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var document = new CompanyDocument
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DocumentType = request.DocumentType,
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            ExpiryDate = request.ExpiryDate,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(document, cancellationToken);

        var response = new CompanyDocumentResponse
        {
            Id = created.Id,
            CompanyId = created.CompanyId,
            DocumentType = created.DocumentType,
            FileName = created.FileName,
            FileUrl = created.FileUrl,
            ExpiryDate = created.ExpiryDate,
            CreatedAt = created.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { companyId, id = created.Id }, response);
    }

    /// <summary>
    /// Gets a document by ID
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="id">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document</returns>
    /// <response code="200">Document found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Document not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDocumentResponse>> GetById(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);

        if (document == null || document.CompanyId != companyId)
        {
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Document with ID '{id}' not found for company '{companyId}'",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var response = new CompanyDocumentResponse
        {
            Id = document.Id,
            CompanyId = document.CompanyId,
            DocumentType = document.DocumentType,
            FileName = document.FileName,
            FileUrl = document.FileUrl,
            ExpiryDate = document.ExpiryDate,
            CreatedAt = document.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Deletes a document
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="id">Document ID</param>
    /// <param name="xmin">Version for optimistic concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Document deleted successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Document not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid companyId, Guid id, [FromQuery] uint xmin, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);

        if (document == null || document.CompanyId != companyId)
        {
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Document with ID '{id}' not found for company '{companyId}'",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var success = await _repository.DeleteAsync(id, xmin, cancellationToken);

        if (!success)
        {
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Document with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        return NoContent();
    }
}
