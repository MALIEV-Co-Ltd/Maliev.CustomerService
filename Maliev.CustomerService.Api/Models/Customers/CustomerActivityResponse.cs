using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Represents a single activity event for a customer
/// </summary>
public record CustomerActivityResponse
{
    /// <summary>The type of action performed (Create, Update, Delete, etc.)</summary>
    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    /// <summary>The description of the activity</summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>ID of the actor who performed the action</summary>
    [JsonPropertyName("actorId")]
    public string ActorId { get; init; } = string.Empty;

    /// <summary>Name of the actor</summary>
    [JsonPropertyName("actorName")]
    public string? ActorName { get; init; }

    /// <summary>Email of the actor</summary>
    [JsonPropertyName("actorEmail")]
    public string? ActorEmail { get; init; }

    /// <summary>When the activity occurred</summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    /// <summary>Details of what changed (JSON)</summary>
    [JsonPropertyName("details")]
    public string? Details { get; init; }
}
