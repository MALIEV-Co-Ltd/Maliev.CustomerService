using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Request model for soft deleting a customer
/// </summary>
public class DeleteCustomerRequest
{
    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
