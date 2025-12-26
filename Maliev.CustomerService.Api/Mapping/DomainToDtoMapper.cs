using System.Text.Json;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Api.Mapping;

/// <summary>
/// Extension methods for mapping domain entities to DTOs
/// </summary>
public static class DomainToDtoMapper
{
    /// <summary>
    /// Maps a Customer entity to CustomerResponse DTO
    /// </summary>
    public static CustomerResponse ToCustomerResponse(this Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            PrincipalId = customer.PrincipalId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Segment = customer.Segment,
            Tier = customer.Tier,
            PreferredLanguage = customer.PreferredLanguage,
            Timezone = customer.Timezone,
            CommunicationPreferences = !string.IsNullOrEmpty(customer.CommunicationPreferences)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(customer.CommunicationPreferences)
                : null,
            CompanyId = customer.CompanyId,
            IsDeleted = customer.IsDeleted,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            Version = customer.Version
        };
    }

    /// <summary>
    /// Maps a Company entity to CompanyResponse DTO
    /// </summary>
    public static CompanyResponse ToCompanyResponse(this Company company)
    {
        return new CompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            VatNumber = company.VatNumber,
            RegistrationNumber = company.RegistrationNumber,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone,
            Segment = company.Segment,
            Tier = company.Tier,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt,
            Version = company.Version
        };
    }

    /// <summary>
    /// Maps an Address entity to AddressResponse DTO
    /// </summary>
    public static AddressResponse ToAddressResponse(this Address address)
    {
        return new AddressResponse
        {
            Id = address.Id,
            OwnerType = address.OwnerType,
            OwnerId = address.OwnerId,
            Type = address.Type,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            Province = address.Province,
            PostalCode = address.PostalCode,
            CountryId = address.CountryId,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt,
            Version = address.Version
        };
    }

    /// <summary>
    /// Maps an NDARecord entity to NDAResponse DTO
    /// </summary>
    public static NDAResponse ToNDAResponse(this NDARecord nda)
    {
        return new NDAResponse
        {
            Id = nda.Id,
            CustomerId = nda.CustomerId,
            DocumentReferenceId = nda.DocumentReferenceId,
            Status = nda.Status,
            SignedBy = nda.SignedBy,
            SignedAt = nda.SignedAt,
            RevokedAt = nda.RevokedAt,
            ExpiresAt = nda.ExpiresAt,
            CreatedAt = nda.CreatedAt,
            UpdatedAt = nda.UpdatedAt,
            Version = nda.Version
        };
    }

    /// <summary>
    /// Maps an InternalNote entity to InternalNoteResponse DTO
    /// </summary>
    public static InternalNoteResponse ToInternalNoteResponse(this InternalNote note)
    {
        return new InternalNoteResponse
        {
            Id = note.Id,
            OwnerType = note.OwnerType,
            OwnerId = note.OwnerId,
            NoteText = note.NoteText,
            CreatedBy = note.CreatedBy,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            Version = note.Version
        };
    }

    /// <summary>
    /// Maps a DocumentReference entity to DocumentResponse DTO
    /// </summary>
    public static DocumentResponse ToDocumentResponse(this DocumentReference document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            OwnerType = document.OwnerType,
            OwnerId = document.OwnerId,
            DocumentType = document.DocumentType,
            FileReference = document.FileReference,
            Filename = document.Filename,
            Status = document.Status,
            Version = document.Version,
            SignedBy = document.SignedBy,
            SignedAt = document.SignedAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            RowVersion = document.RowVersion
        };
    }
}
