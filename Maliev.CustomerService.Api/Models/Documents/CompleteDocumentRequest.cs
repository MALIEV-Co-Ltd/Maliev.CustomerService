using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Documents;

/// <summary>
/// Request model for marking a document as complete
/// </summary>
public class CompleteDocumentRequest
{
    /// <summary>
    /// User who signed the document
    /// </summary>
    [JsonPropertyName("signedBy")]
    public string? SignedBy { get; set; }

    /// <summary>
    /// Timestamp when document was signed
    /// </summary>
    [JsonPropertyName("signedAt")]
    public DateTime? SignedAt { get; set; }
}
