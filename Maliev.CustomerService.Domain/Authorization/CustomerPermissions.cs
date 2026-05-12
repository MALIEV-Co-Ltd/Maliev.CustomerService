namespace Maliev.CustomerService.Domain.Authorization;

/// <summary>
/// Defines all permissions for the Customer Service.
/// Permissions follow the format: customer.{resource}.{action}
/// </summary>
public static class CustomerPermissions
{
    /// <summary>Permission to read company information.</summary>
    public const string CompaniesRead = "customer.companies.read";

    /// <summary>Permission to manage company information.</summary>
    public const string CompaniesManage = "customer.companies.manage";

    /// <summary>Permission to write company information.</summary>
    public const string CompaniesWrite = "customer.companies.write";

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
    /// <summary>Self-register as a customer (anonymous — no auth required).</summary>
    public const string CustomersRegister = "customer.customers.register";

    // Account Operations
    /// <summary>Permission to read customer portal account status.</summary>
    public const string AccountsRead = "customer.accounts.read";
    /// <summary>Permission to manage customer portal accounts and credential operations.</summary>
    public const string AccountsManage = "customer.accounts.manage";

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

    // Tier Operations
    /// <summary>Permission to read tier settings.</summary>
    public const string TiersRead = "customer.tiers.read";
    /// <summary>Permission to manage tier settings.</summary>
    public const string TiersManage = "customer.tiers.manage";

    /// <summary>
    /// Collection of all defined customer permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { CompaniesRead, "Read company information" },
        { CompaniesManage, "Manage company information" },
        { CompaniesWrite, "Write company information" },
        { CustomersCreate, "Create new customers" },
        { CustomersRead, "Read customer information" },
        { CustomersUpdate, "Update customer information" },
        { CustomersDelete, "Delete customers" },
        { CustomersList, "List all customers" },
        { CustomersSearch, "Search customers" },
        { CustomersRegister, "Self-register as a customer (anonymous)" },
        { AccountsRead, "Read customer portal account status" },
        { AccountsManage, "Manage customer portal accounts and credentials" },
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
        { NdasDelete, "Delete NDAs" },
        { TiersRead, "Read tier settings" },
        { TiersManage, "Manage tier settings" }
    };

    /// <summary>
    /// All permissions defined for the Customer Service.
    /// </summary>
    public static readonly string[] All = AllWithDescriptions.Keys.ToArray();
}
