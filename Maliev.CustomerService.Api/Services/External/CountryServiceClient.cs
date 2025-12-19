using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Retry;
using System.Net;

namespace Maliev.CustomerService.Api.Services.External;

/// <summary>
/// Client for validating country IDs via the Country Service
/// Implements retry policies with Polly and 24-hour caching for valid country IDs
/// </summary>
public class CountryServiceClient : ICountryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CountryServiceClient> _logger;
    private readonly IMemoryCache _cache;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    /// <summary>
    /// Initializes a new instance of the CountryServiceClient class
    /// </summary>
    /// <param name="httpClient">HTTP client for Country Service communication</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="cache">Memory cache for caching validation results</param>
    public CountryServiceClient(
        HttpClient httpClient,
        ILogger<CountryServiceClient> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;

        // Configure Polly retry policy: 3 attempts with exponential backoff
        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response =>
                        response.StatusCode == HttpStatusCode.RequestTimeout ||
                        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == HttpStatusCode.GatewayTimeout ||
                        (int)response.StatusCode >= 500)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry attempt {AttemptNumber} after {Delay}ms due to {Outcome}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Validates if a country ID exists in the Country Service with retry logic and caching
    /// </summary>
    /// <param name="countryId">Country ID to validate</param>
    /// <returns>True if the country ID is valid, false otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown when Country Service is unavailable or request times out</exception>
    /// <remarks>
    /// Valid country IDs are cached for 24 hours. The method implements exponential backoff retry with up to 3 attempts.
    /// </remarks>
    public async Task<bool> ValidateCountryIdAsync(Guid countryId)
    {
        var cacheKey = $"country_valid_{countryId}";

        // Check cache first
        if (_cache.TryGetValue<bool>(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("Country ID {CountryId} validation result from cache: {IsValid}",
                countryId, cachedResult);
            return cachedResult;
        }

        try
        {
            _logger.LogDebug("Validating country ID {CountryId} via Country Service", countryId);

            // Execute request with retry policy
            var response = await _retryPipeline.ExecuteAsync(async cancellationToken =>
            {
                return await _httpClient.GetAsync($"/country/v1/countries/{countryId}", cancellationToken);
            });

            var isValid = response.StatusCode == HttpStatusCode.OK;

            // Cache valid results for 24 hours
            if (isValid)
            {
                _cache.Set(cacheKey, true, CacheExpiration);
                _logger.LogInformation("Country ID {CountryId} validated successfully and cached", countryId);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Country ID {CountryId} not found in Country Service", countryId);
            }
            else
            {
                _logger.LogWarning("Country Service returned {StatusCode} for country ID {CountryId}",
                    response.StatusCode, countryId);
            }

            return isValid;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error validating country ID {CountryId}", countryId);
            throw new InvalidOperationException("Country Service is unavailable", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout validating country ID {CountryId}", countryId);
            throw new InvalidOperationException("Country Service request timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating country ID {CountryId}", countryId);
            throw;
        }
    }
}
