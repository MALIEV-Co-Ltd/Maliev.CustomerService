using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.NDAs;

/// <summary>
/// Request model for deleting an NDA record
/// </summary>
public class DeleteNDARequest
{
    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
