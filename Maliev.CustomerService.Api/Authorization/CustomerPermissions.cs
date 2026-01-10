namespace Maliev.CustomerService.Api.Authorization;

/// <summary>
/// Defines all permissions for the Customer Service.
/// Permissions follow the format: customer.{resource}.{action}
/// </summary>
public static class CustomerPermissions
{
    // Customer Operations
    /// <summary>Permission to create new customers.</summary>
    public const string CustomersCreate = "customer.customers.create";
    /// <summary>Permission to read customer information.</summary>
    public const string CustomersRead = "customer.customers.read";
    /// <summary>Permission to update customer information.</summary>
    public const string CustomersUpdate = "customer.customers.update";
    /// <summary>Permission to delete customers.</summary>
    public const string CustomersDelete = "customer.customers.delete";
    /// <summary>Permission to list all customers.</summary>
    public const string CustomersList = "customer.customers.list";
    /// <summary>Permission to search customers.</summary>
    public const string CustomersSearch = "customer.customers.search";

    // Company Operations
    /// <summary>Permission to manage company information.</summary>
    public const string CompaniesManage = "customer.companies.manage";

    // Address Operations
    /// <summary>Permission to manage customer addresses.</summary>
    public const string AddressesManage = "customer.addresses.manage";

    // Document Operations
    /// <summary>Permission to create customer documents.</summary>
    public const string DocumentsCreate = "customer.documents.create";
    /// <summary>Permission to read customer documents.</summary>
    public const string DocumentsRead = "customer.documents.read";
    /// <summary>Permission to delete customer documents.</summary>
    public const string DocumentsDelete = "customer.documents.delete";

    // Internal Note Operations
    /// <summary>Permission to create internal notes.</summary>
    public const string NotesCreate = "customer.notes.create";
    /// <summary>Permission to read internal notes.</summary>
    public const string NotesRead = "customer.notes.read";
    /// <summary>Permission to update internal notes.</summary>
    public const string NotesUpdate = "customer.notes.update";
    /// <summary>Permission to delete internal notes.</summary>
    public const string NotesDelete = "customer.notes.delete";

    // NDA Operations
    /// <summary>Permission to create NDAs.</summary>
    public const string NdasCreate = "customer.ndas.create";
    /// <summary>Permission to read NDAs.</summary>
    public const string NdasRead = "customer.ndas.read";
    /// <summary>Permission to update NDAs.</summary>
    public const string NdasUpdate = "customer.ndas.update";
    /// <summary>Permission to delete NDAs.</summary>
    public const string NdasDelete = "customer.ndas.delete";

    /// <summary>
    /// Collection of all defined customer permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { CustomersCreate, "Create new customers" },
        { CustomersRead, "Read customer information" },
        { CustomersUpdate, "Update customer information" },
        { CustomersDelete, "Delete customers" },
        { CustomersList, "List all customers" },
        { CustomersSearch, "Search customers" },
        { CompaniesManage, "Manage company information" },
        { AddressesManage, "Manage customer addresses" },
        { DocumentsCreate, "Create customer documents" },
        { DocumentsRead, "Read customer documents" },
        { DocumentsDelete, "Delete customer documents" },
        { NotesCreate, "Create internal notes" },
        { NotesRead, "Read internal notes" },
        { NotesUpdate, "Update internal notes" },
        { NotesDelete, "Delete internal notes" },
        { NdasCreate, "Create NDAs" },
        { NdasRead, "Read NDAs" },
        { NdasUpdate, "Update NDAs" },
        { NdasDelete, "Delete NDAs" }
    };

    /// <summary>
    /// All permissions defined for the Customer Service.
    /// </summary>
    public static readonly string[] All = AllWithDescriptions.Keys.ToArray();
}
