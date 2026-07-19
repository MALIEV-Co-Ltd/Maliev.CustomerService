using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Addresses;

/// <summary>
/// Request model for deleting an address
/// </summary>
public class DeleteAddressRequest
{
    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
