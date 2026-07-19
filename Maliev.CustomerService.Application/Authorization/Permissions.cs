namespace Maliev.CustomerService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Customer Service.
/// </summary>
public static class CustomerPermissions
{
    public const string CompanyRead = "customer.companies.read";
    public const string CompanyManage = "customer.companies.manage";
    public const string CompanyWrite = "customer.companies.write";

    public const string CustomerCreate = "customer.customers.create";
    public const string CustomerRead = "customer.customers.read";
    public const string CustomerUpdate = "customer.customers.update";
    public const string CustomerDelete = "customer.customers.delete";
    public const string CustomerList = "customer.customers.list";
    public const string CustomerSearch = "customer.customers.search";

    public const string AddressManage = "customer.addresses.manage";

    public const string DocumentCreate = "customer.documents.create";
    public const string DocumentRead = "customer.documents.read";
    public const string DocumentDelete = "customer.documents.delete";

    public const string NoteCreate = "customer.notes.create";
    public const string NoteRead = "customer.notes.read";
    public const string NoteUpdate = "customer.notes.update";
    public const string NoteDelete = "customer.notes.delete";

    public const string NdaCreate = "customer.ndas.create";
    public const string NdaRead = "customer.ndas.read";
    public const string NdaUpdate = "customer.ndas.update";
    public const string NdaDelete = "customer.ndas.delete";

    public const string TierRead = "customer.tiers.read";
    public const string TierManage = "customer.tiers.manage";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { CompanyRead, "Read company data" },
        { CompanyManage, "Manage company data" },
        { CompanyWrite, "Write company data" },
        { CustomerCreate, "Create customers" },
        { CustomerRead, "Read customer data" },
        { CustomerUpdate, "Update customer data" },
        { CustomerDelete, "Delete customers" },
        { CustomerList, "List customers" },
        { CustomerSearch, "Search customers" },
        { AddressManage, "Manage customer addresses" },
        { DocumentCreate, "Create customer documents" },
        { DocumentRead, "Read customer documents" },
        { DocumentDelete, "Delete customer documents" },
        { NoteCreate, "Create customer notes" },
        { NoteRead, "Read customer notes" },
        { NoteUpdate, "Update customer notes" },
        { NoteDelete, "Delete customer notes" },
        { NdaCreate, "Create customer NDAs" },
        { NdaRead, "Read customer NDAs" },
        { NdaUpdate, "Update customer NDAs" },
        { NdaDelete, "Delete customer NDAs" },
        { TierRead, "Read customer tiers" },
        { TierManage, "Manage customer tiers" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
