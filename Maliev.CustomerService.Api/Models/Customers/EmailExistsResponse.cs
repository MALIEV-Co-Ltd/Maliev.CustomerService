namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Response model for email existence check
/// </summary>
public class EmailExistsResponse
{
    /// <summary>
    /// Indicates whether a customer with this email already exists
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// The normalized email that was checked
    /// </summary>
    public string? Email { get; set; }
}
