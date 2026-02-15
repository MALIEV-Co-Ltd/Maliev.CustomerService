using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.NDAs;

/// <summary>
/// Request model for deleting an NDA record
/// </summary>
public class DeleteNDARequest
{
    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Required]
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
