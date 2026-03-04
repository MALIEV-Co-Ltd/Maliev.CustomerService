using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.CustomerService.Api.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Application.Services;
using Maliev.CustomerService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for company management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("customer/v{version:apiVersion}/companies")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ITierCalculationService _tierCalculationService;
    private readonly ILogger<CompanyController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompanyController"/> class
    /// </summary>
    /// <param name="companyService">Company service</param>
    /// <param name="tierCalculationService">Tier calculation service</param>
    /// <param name="logger">Logger instance</param>
    public CompanyController(
        ICompanyService companyService,
        ITierCalculationService tierCalculationService,
        ILogger<CompanyController> logger)
    {
        _companyService = companyService;
        _tierCalculationService = tierCalculationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new company
    /// </summary>
    /// <param name="request">Company creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created company response</returns>
    /// <response code="201">Company created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="422">Invalid VAT number format</response>
    [HttpPost]
    [RequirePermission(CustomerPermissions.CompaniesManage)]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CompanyResponse>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for company creation: {Errors}", errors);

                var errorResponse = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred: " + errors,
                    Details = ModelState
                        .Where(ms => ms.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return BadRequest(errorResponse);
            }

            var (actorId, actorType) = User.GetActorInfo();

            var company = await _companyService.CreateAsync(request, actorId, actorType);

            return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid VAT number format");
            return UnprocessableEntity(new ErrorResponse
            {
                Code = "INVALID_VAT_FORMAT",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating company");
            throw; // Let global exception handler deal with it
        }
    }

    /// <summary>
    /// Retrieves a company by ID
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <returns>Company response</returns>
    /// <response code="200">Company found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Company not found</response>
    [HttpGet("{id:guid}")]
    [RequirePermission(CustomerPermissions.CompaniesRead)]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id)
    {
        try
        {
            var company = await _companyService.GetByIdAsync(id);

            if (company == null)
            {
                _logger.LogDebug("Company {CompanyId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"Company with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(company);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Company {CompanyId} not found", id);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company {CompanyId}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all companies with pagination and filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="segment">Optional customer segment filter</param>
    /// <param name="tier">Optional customer tier filter</param>
    /// <returns>Paginated list of companies</returns>
    /// <response code="200">Companies retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [RequirePermission(CustomerPermissions.CompaniesRead)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? segment = null,
        [FromQuery] string? tier = null)
    {
        try
        {
            // Enforce maximum page size
            pageSize = Math.Min(pageSize, 100);

            var result = await _companyService.GetAllAsync(page, pageSize, segment, tier);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies");
            throw;
        }
    }

    /// <summary>
    /// Searches for companies by name or VAT number and includes their default billing address
    /// </summary>
    /// <param name="query">Search query (name or VAT)</param>
    /// <param name="limit">Maximum number of results to return (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of company search results with addresses</returns>
    /// <response code="200">Search results retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("search")]
    [RequirePermission(CustomerPermissions.CompaniesRead)]
    [ProducesResponseType(typeof(List<CompanySearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<CompanySearchResultDto>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new List<CompanySearchResultDto>());
            }

            var results = await _companyService.SearchWithAddressAsync(query, limit, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching companies for query {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing company
    /// </summary>

    /// <param name="id">Company ID</param>
    /// <param name="request">Company update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated company response</returns>
    /// <response code="200">Company updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Company not found</response>
    /// <response code="409">Version conflict</response>
    /// <response code="422">Invalid VAT number format</response>
    [HttpPatch("{id:guid}")]
    [RequirePermission(CustomerPermissions.CompaniesManage)]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CompanyResponse>> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for company update: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                var errorResponse = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred",
                    Details = ModelState
                        .Where(ms => ms.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return BadRequest(errorResponse);
            }

            var (actorId, actorType) = User.GetActorInfo();

            var company = await _companyService.UpdateAsync(id, request, actorId, actorType);

            return Ok(company);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Company {CompanyId} not found for update", id);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict for company {CompanyId}", id);
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = "The record was modified by another user. Please refresh and try again.",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid VAT number format");
            return UnprocessableEntity(new ErrorResponse
            {
                Code = "INVALID_VAT_FORMAT",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating company {CompanyId}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a company with its associated customers
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <returns>Company with customers response</returns>
    /// <response code="200">Company with customers found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Company not found</response>
    [HttpGet("{id:guid}/customers")]
    [RequirePermission(CustomerPermissions.CompaniesRead)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetCompanyWithCustomers(Guid id)
    {
        try
        {
            var result = await _companyService.GetWithCustomersAsync(id);

            if (result == null)
            {
                _logger.LogDebug("Company {CompanyId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"Company with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                company = result.Value.Company,
                customers = result.Value.Customers
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Company {CompanyId} not found", id);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company {CompanyId} with customers", id);
            throw;
        }
    }

    /// <summary>
    /// Manually triggers tier recalculation for a company
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company with updated tier</returns>
    /// <response code="200">Tier recalculated successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Company not found</response>
    [HttpPost("{id:guid}/calculate-tier")]
    [RequirePermission(CustomerPermissions.TiersManage)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> CalculateTier(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var tierChanged = await _tierCalculationService.ApplyTierAsync(id, cancellationToken);
            var company = await _tierCalculationService.GetCompanyWithTierAsync(id, cancellationToken);

            if (company == null)
            {
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"Company with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation(
                "Tier recalculation for company {CompanyId}: tierChanged={TierChanged}, currentTier={Tier}",
                id, tierChanged, company.Tier);

            return Ok(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating tier for company {CompanyId}", id);
            throw;
        }
    }
}
