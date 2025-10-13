namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Document reference status constants
/// </summary>
public static class DocumentStatus
{
    public const string Pending = "Pending";
    public const string Complete = "Complete";
    public const string PendingDeletion = "PendingDeletion";
    public const string Orphaned = "Orphaned";
    public const string MissingFile = "MissingFile";

    public static readonly string[] All = { Pending, Complete, PendingDeletion, Orphaned, MissingFile };
}
