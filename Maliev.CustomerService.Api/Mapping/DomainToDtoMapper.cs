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
    /// Maps a Customer entity to CustomerResponse DTO with optional company and NDA data
    /// </summary>
    public static CustomerResponse ToCustomerResponse(this Customer customer, Company? company = null, NDARecord? nda = null)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            PrincipalId = customer.PrincipalId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Mobile = customer.Mobile,
            Extension = customer.Extension,
            Landline = customer.Landline,
            Segment = customer.Segment,
            Tier = customer.Tier,
            PreferredLanguage = customer.PreferredLanguage,
            Timezone = customer.Timezone,
            CommunicationPreferences = !string.IsNullOrEmpty(customer.CommunicationPreferences)
                ? JsonSerializer.Deserialize<Dictionary<string, bool>>(customer.CommunicationPreferences)
                : null,
            CompanyId = customer.CompanyId,
            CompanyName = company?.Name,
            CompanyPhone = company?.ContactPhone,
            NDAStatus = nda?.Status,
            UsesCompanyBillingAddress = customer.UsesCompanyBillingAddress,
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
            IsDefault = address.IsDefault,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            AddressLine3 = address.AddressLine3,
            District = address.District,
            City = address.City,
            StateProvince = address.StateProvince,
            PostalCode = address.PostalCode,
            CountryId = address.CountryId,
            RecipientName = address.RecipientName,
            RecipientPhone = address.RecipientPhone,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt,
            Version = address.Version
        };
    }

    /// <summary>
    /// Maps an Address entity to AddressSummaryDto
    /// </summary>
    public static AddressSummaryDto ToAddressSummaryDto(this Address address)
    {
        return new AddressSummaryDto
        {
            IsDefault = address.IsDefault,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            AddressLine3 = address.AddressLine3,
            District = address.District,
            City = address.City,
            StateProvince = address.StateProvince,
            PostalCode = address.PostalCode
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
