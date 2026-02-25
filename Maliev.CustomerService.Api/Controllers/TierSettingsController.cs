using Asp.Versioning;
using Maliev.CustomerService.Api.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Application.DTOs;
using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for tier settings management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("customer/v{version:apiVersion}/tier-settings")]
[Authorize]
public class TierSettingsController : ControllerBase
{
    private readonly ICompanyTierSettingsRepository _repository;
    private readonly ILogger<TierSettingsController> _logger;

    /// <summary>
    /// Initializes a new instance of TierSettingsController
    /// </summary>
    /// <param name="repository">Tier settings repository</param>
    /// <param name="logger">Logger instance</param>
    public TierSettingsController(
        ICompanyTierSettingsRepository repository,
        ILogger<TierSettingsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active tier settings
    /// </summary>
    /// <returns>List of tier settings</returns>
    /// <response code="200">Tier settings retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<TierSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<TierSettingsResponse>>> GetAll(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetActiveSettingsAsync(cancellationToken);

        var response = settings.Select(s => new TierSettingsResponse
        {
            Id = s.Id,
            TierName = s.TierName,
            MinPurchaseValue = s.MinPurchaseValue,
            MinOrderCount = s.MinOrderCount,
            DiscountPercentage = s.DiscountPercentage,
            FreeShippingMinOrder = s.FreeShippingMinOrder,
            CoinRewardPercentage = s.CoinRewardPercentage,
            ValidFrom = s.ValidFrom,
            ValidTo = s.ValidTo,
            xmin = s.xmin
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Creates a new tier setting
    /// </summary>
    /// <param name="request">Tier settings request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created tier setting</returns>
    /// <response code="201">Tier setting created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [ProducesResponseType(typeof(TierSettingsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TierSettingsResponse>> Create([FromBody] TierSettingsRequest request, CancellationToken cancellationToken = default)
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

        var settings = new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = request.TierName,
            MinPurchaseValue = request.MinPurchaseValue,
            MinOrderCount = request.MinOrderCount,
            DiscountPercentage = request.DiscountPercentage,
            FreeShippingMinOrder = request.FreeShippingMinOrder,
            CoinRewardPercentage = request.CoinRewardPercentage,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(settings, cancellationToken);

        var response = new TierSettingsResponse
        {
            Id = created.Id,
            TierName = created.TierName,
            MinPurchaseValue = created.MinPurchaseValue,
            MinOrderCount = created.MinOrderCount,
            DiscountPercentage = created.DiscountPercentage,
            FreeShippingMinOrder = created.FreeShippingMinOrder,
            CoinRewardPercentage = created.CoinRewardPercentage,
            ValidFrom = created.ValidFrom,
            ValidTo = created.ValidTo,
            xmin = created.xmin
        };

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    /// <summary>
    /// Gets a tier setting by ID
    /// </summary>
    /// <param name="id">Tier setting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tier setting</returns>
    /// <response code="200">Tier setting found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Tier setting not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TierSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TierSettingsResponse>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetByIdAsync(id, cancellationToken);

        if (settings == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Tier setting with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var response = new TierSettingsResponse
        {
            Id = settings.Id,
            TierName = settings.TierName,
            MinPurchaseValue = settings.MinPurchaseValue,
            MinOrderCount = settings.MinOrderCount,
            DiscountPercentage = settings.DiscountPercentage,
            FreeShippingMinOrder = settings.FreeShippingMinOrder,
            CoinRewardPercentage = settings.CoinRewardPercentage,
            ValidFrom = settings.ValidFrom,
            ValidTo = settings.ValidTo,
            xmin = settings.xmin
        };

        return Ok(response);
    }

    /// <summary>
    /// Updates a tier setting
    /// </summary>
    /// <param name="id">Tier setting ID</param>
    /// <param name="request">Tier settings request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated tier setting</returns>
    /// <response code="200">Tier setting updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Tier setting not found</response>
    /// <response code="409">Version conflict</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TierSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TierSettingsResponse>> Update(Guid id, [FromBody] TierSettingsRequest request, CancellationToken cancellationToken = default)
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

        var existing = await _repository.GetByIdAsync(id, cancellationToken);

        if (existing == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Tier setting with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        existing.TierName = request.TierName;
        existing.MinPurchaseValue = request.MinPurchaseValue;
        existing.MinOrderCount = request.MinOrderCount;
        existing.DiscountPercentage = request.DiscountPercentage;
        existing.FreeShippingMinOrder = request.FreeShippingMinOrder;
        existing.CoinRewardPercentage = request.CoinRewardPercentage;
        existing.ValidFrom = request.ValidFrom;
        existing.ValidTo = request.ValidTo;

        var success = await _repository.UpdateAsync(existing, cancellationToken);

        if (!success)
        {
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = "The record was modified by another user. Please refresh and try again.",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        var response = new TierSettingsResponse
        {
            Id = existing.Id,
            TierName = existing.TierName,
            MinPurchaseValue = existing.MinPurchaseValue,
            MinOrderCount = existing.MinOrderCount,
            DiscountPercentage = existing.DiscountPercentage,
            FreeShippingMinOrder = existing.FreeShippingMinOrder,
            CoinRewardPercentage = existing.CoinRewardPercentage,
            ValidFrom = existing.ValidFrom,
            ValidTo = existing.ValidTo,
            xmin = existing.xmin
        };

        return Ok(response);
    }
}
