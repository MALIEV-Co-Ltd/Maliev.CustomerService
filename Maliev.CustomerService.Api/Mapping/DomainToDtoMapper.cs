using System.Text.Json;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Domain.Entities;

namespace Maliev.CustomerService.Api.Mapping;

/// <summary>
/// Extension methods for mapping domain entities to DTOs
/// </summary>
public static class DomainToDtoMapper
{
    /// <summary>
    /// Maps a Customer entity to CustomerResponse DTO with optional company and NDA data
    /// </summary>
    public static CustomerResponse ToCustomerResponse(this Customer customer, Company? company = null, NDARecord? nda = null, uint xmin = 0)
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
            ThaiNationalId = customer.ThaiNationalId, // Will be masked by CustomerResponse property
            Segment = customer.Segment,
            Tier = customer.Tier,
            PreferredLanguage = customer.PreferredLanguage,
            Timezone = customer.Timezone,
            CommunicationPreferences = !string.IsNullOrEmpty(customer.CommunicationPreferences)
                ? JsonSerializer.Deserialize<Dictionary<string, bool>>(customer.CommunicationPreferences)
                : null,
            PaymentTerms = customer.PaymentTerms,
            CompanyId = customer.CompanyId,
            AccountManagerEmployeeId = customer.AccountManagerEmployeeId,
            CompanyName = company?.Name,
            CompanyPhone = company?.ContactPhone,
            NDAStatus = nda?.Status,
            UsesCompanyBillingAddress = customer.UsesCompanyBillingAddress,
            IsDeleted = customer.IsDeleted,

            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            xmin = xmin
        };
    }

    /// <summary>
    /// Maps a Company entity to CompanyResponse DTO
    /// </summary>
    public static CompanyResponse ToCompanyResponse(this Company company, uint xmin = 0)
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
            // BDEX fields
            FullNameTh = company.FullNameTh,
            RegistrationDate = company.RegistrationDate,
            CompanyStatus = company.CompanyStatus,
            CompanyStatusNameTh = company.CompanyStatusNameTh,
            CompanyTypeCode = company.CompanyTypeCode,
            BusinessObjectives = company.BusinessObjectives,
            IsVerifiedFromBdex = company.IsVerifiedFromBdex,
            BdexVerificationDate = company.BdexVerificationDate,
            StockSymbol = company.StockSymbol,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt,
            xmin = xmin
        };
    }

    /// <summary>
    /// Maps an Address entity to AddressResponse DTO
    /// </summary>
    public static AddressResponse ToAddressResponse(this Address address, uint xmin = 0)
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
            xmin = xmin
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
    public static NDAResponse ToNDAResponse(this NDARecord nda, uint xmin = 0)
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
            xmin = xmin
        };
    }

    /// <summary>
    /// Maps an InternalNote entity to InternalNoteResponse DTO
    /// </summary>
    public static InternalNoteResponse ToInternalNoteResponse(this InternalNote note, uint xmin = 0)
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
            xmin = xmin
        };
    }

    /// <summary>
    /// Maps an InternalNoteComment entity to InternalNoteCommentResponse DTO
    /// </summary>
    public static InternalNoteCommentResponse ToInternalNoteCommentResponse(this InternalNoteComment comment, uint xmin = 0)
    {
        return new InternalNoteCommentResponse
        {
            Id = comment.Id,
            InternalNoteId = comment.InternalNoteId,
            CommentText = comment.CommentText,
            CreatedBy = comment.CreatedBy,
            CreatedByName = comment.CreatedByName,
            CreatedAt = comment.CreatedAt,
            xmin = xmin
        };
    }

    /// <summary>
    /// Maps a DocumentReference entity to DocumentResponse DTO
    /// </summary>
    public static DocumentResponse ToDocumentResponse(this DocumentReference document, uint xmin = 0)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            OwnerType = document.OwnerType,
            OwnerId = document.OwnerId,
            DocumentType = document.DocumentType,
            FileReference = document.FileReference,
            Filename = document.Filename,
            FileSize = document.FileSize,
            MimeType = document.MimeType,
            Status = document.Status,
            Version = document.Version,
            CreatedBy = document.CreatedBy,
            SignedBy = document.SignedBy,
            SignedAt = document.SignedAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            xmin = xmin
        };
    }
}
