namespace Maliev.CustomerService.Application.DTOs;

/// <summary>
/// Response DTO for company documents
/// </summary>
public class CompanyDocumentResponse
{
    /// <summary>
    /// Unique identifier of the document.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Unique identifier of the company that owns this document.
    /// </summary>
    public Guid CompanyId { get; set; }
    /// <summary>
    /// Type of document (e.g., "TaxRegistration", "BusinessLicense").
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;
    /// <summary>
    /// Original file name of the uploaded document.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// URL to access the stored document file.
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;
    /// <summary>
    /// Expiry date of the document, if applicable.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
    /// <summary>
    /// Timestamp when the document was uploaded.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating company documents
/// </summary>
public class CreateCompanyDocumentRequest
{
    /// <summary>
    /// Type of document being uploaded.
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;
    /// <summary>
    /// Original file name of the document.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// URL where the document file is stored.
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;
    /// <summary>
    /// Optional expiry date for time-limited documents.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
}
