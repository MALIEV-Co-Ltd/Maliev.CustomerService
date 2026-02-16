namespace Maliev.CustomerService.Data.Attributes;

/// <summary>
/// Attribute to mark string properties that should be encrypted at rest
/// Used for sensitive PII data like Thai National IDs
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EncryptedAttribute : Attribute
{
}
