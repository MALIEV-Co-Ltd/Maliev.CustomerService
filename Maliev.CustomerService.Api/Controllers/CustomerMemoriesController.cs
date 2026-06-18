using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for customer-scoped memory operations.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("customer/v{version:apiVersion}/customers/{customerId:guid}/memories")]
public class CustomerMemoriesController : ControllerBase
{
    private readonly ICustomerMemoryService _customerMemoryService;
    private readonly ILogger<CustomerMemoriesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerMemoriesController"/> class.
    /// </summary>
    /// <param name="customerMemoryService">Customer memory service.</param>
    /// <param name="logger">Logger instance.</param>
    public CustomerMemoriesController(
        ICustomerMemoryService customerMemoryService,
        ILogger<CustomerMemoriesController> logger)
    {
        _customerMemoryService = customerMemoryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets customer-scoped memories for Make Studio and customer-facing assistants.
    /// </summary>
    /// <param name="customerId">Owning customer ID.</param>
    /// <param name="query">Optional search query.</param>
    /// <param name="limit">Maximum result count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching customer memories.</returns>
    [HttpGet]
    [RequirePermission(CustomerPermissions.MemoriesRead)]
    [ProducesResponseType(typeof(CustomerMemoryQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerMemoryQueryResponse>> Get(
        Guid customerId,
        [FromQuery] string? query = null,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _customerMemoryService.GetAsync(customerId, query, limit, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer memory query failed because customer {CustomerId} was not found.", customerId);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Observes or reinforces one customer-scoped memory.
    /// </summary>
    /// <param name="customerId">Owning customer ID.</param>
    /// <param name="request">Memory observation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inserted or updated memory.</returns>
    [HttpPost("observe")]
    [RequirePermission(CustomerPermissions.MemoriesWrite)]
    [ProducesResponseType(typeof(CustomerMemoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerMemoryResponse>> Observe(
        Guid customerId,
        [FromBody] CustomerMemoryObserveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            return Ok(await _customerMemoryService.ObserveAsync(customerId, request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer memory observe failed because customer {CustomerId} was not found.", customerId);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
