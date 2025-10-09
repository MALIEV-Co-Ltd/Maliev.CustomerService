using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.NDAs;

/// <summary>
/// Request model for creating a new NDA record
/// </summary>
public class CreateNDARequest
{
    /// <summary>
    /// Customer ID associated with this NDA
    /// </summary>
    [Required(ErrorMessage = "Customer ID is required")]
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Reference to the document in the Document Management system (optional until signed)
    /// </summary>
    [JsonPropertyName("documentReferenceId")]
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>
    /// Expiration date for the NDA (must be a future date if provided)
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}
