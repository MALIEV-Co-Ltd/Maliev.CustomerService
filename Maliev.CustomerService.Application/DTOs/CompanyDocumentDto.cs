namespace Maliev.CustomerService.Application.DTOs;

/// <summary>
/// Response DTO for company documents
/// </summary>
public class CompanyDocumentResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating company documents
/// </summary>
public class CreateCompanyDocumentRequest
{
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
}
