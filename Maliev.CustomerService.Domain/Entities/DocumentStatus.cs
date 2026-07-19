namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Document reference status constants
/// </summary>
public static class DocumentStatus
{
    /// <summary>Document is pending.</summary>
    public const string Pending = "Pending";
    /// <summary>Document is complete.</summary>
    public const string Complete = "Complete";
    /// <summary>Document is pending deletion.</summary>
    public const string PendingDeletion = "PendingDeletion";
    /// <summary>Document is orphaned.</summary>
    public const string Orphaned = "Orphaned";
    /// <summary>Document file is missing.</summary>
    public const string MissingFile = "MissingFile";

    /// <summary>All document statuses.</summary>
    public static readonly string[] All = { Pending, Complete, PendingDeletion, Orphaned, MissingFile };
}
