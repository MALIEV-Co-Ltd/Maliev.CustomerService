# Maliev.CustomerService Agent Guidelines

This document provides essential instructions, commands, and standards for AI agents and developers working on the Maliev.CustomerService repository.

---

## 1. Build and Test Commands

All commands run from within the service directory (`B:\maliev\Maliev.CustomerService`).

### Build (treats warnings as errors — all must be fixed)
```powershell
dotnet build Maliev.CustomerService.slnx
```

### Run all tests
```powershell
dotnet test Maliev.CustomerService.slnx --verbosity normal
```

### Run a single test method
```powershell
dotnet test --filter "FullyQualifiedName~CustomerServiceTests.CreateAsync_WithValidData_ReturnsCustomerResponse"
```

### Run all tests in a class
```powershell
dotnet test --filter "FullyQualifiedName~CustomerServiceTests"
```

### Run with code coverage
```powershell
dotnet test Maliev.CustomerService.slnx --collect:"XPlat Code Coverage"
```

### Format check
```powershell
dotnet format Maliev.CustomerService.slnx
```

### EF Core migrations (Infrastructure project only)
```powershell
dotnet ef migrations add <Name> --project Maliev.CustomerService.Infrastructure --startup-project Maliev.CustomerService.Infrastructure
```

---

## 2. Code Style & Conventions

### Project Structure (Clean Architecture)
```
Maliev.CustomerService/
├── Maliev.CustomerService.Api/           # Controllers, Consumers, Middleware
├── Maliev.CustomerService.Application/   # Use cases, DTOs, Interfaces, Handlers
├── Maliev.CustomerService.Domain/        # Entities, value objects, domain interfaces
├── Maliev.CustomerService.Infrastructure/ # EF Core DbContext, repositories, HTTP clients
├── Maliev.CustomerService.Tests/         # Unit + Integration tests (xUnit)
├── Directory.Build.props                 # Central package versioning
└── Maliev.CustomerService.slnx          # Solution file (.slnx preferred over .sln)
```

### C# Naming & Formatting
- **Namespaces**: File-scoped (`namespace Maliev.CustomerService.Domain.Entities;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `GetByIdAsync`)
- **Interfaces**: Prefix with `I` (e.g., `ICustomerService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `customer.customers.create`, `customer.addresses.update`
  - Invalid: `customer.customer.create` (singular), `customer.create` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns
- **DI**: Constructor injection with `private readonly` fields
  ```csharp
  public class CustomerService : ICustomerService
  {
      private readonly CustomerDbContext _context;
      private readonly ILogger<CustomerService> _logger;

      public CustomerService(CustomerDbContext context, ILogger<CustomerService> logger)
      {
          _context = context;
          _logger = logger;
      }
  }
  ```
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("customer/v{version:apiVersion}")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {CustomerId}", customerId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **JSON**: Follow existing conventions in this service — check current controller serialization settings
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned

### Documentation
- Public methods and classes must have XML documentation (`/// <summary>`).
- Document parameters (`<param>`) and return values (`<returns>`).
- Document exceptions thrown (`<exception>`).

Example:
```csharp
/// <summary>
/// Creates a new customer with audit logging.
/// </summary>
/// <param name="request">The customer creation request data.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The created customer response.</returns>
/// <exception cref="InvalidOperationException">Thrown if email already exists.</exception>
public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
{
    // Implementation
}
```

---

## 3. Architecture & Patterns

### Layering
- **Controllers:** Minimal logic. Delegate to Handlers/Services.
- **Application Layer:** Use cases, CQRS handlers, DTOs.
- **Domain Layer:** Business logic, entities, interfaces, value objects.
- **Infrastructure:** Data access (EF Core), external integrations.

### Key Libraries
- **Entity Framework Core:** Data access. Use `Async` methods (`ToListAsync`, `FirstOrDefaultAsync`).
- **MassTransit:** Event bus/messaging (`IPublishEndpoint`).
- **xUnit:** Testing framework.

---

## 4. Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/customer/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

---

## 5. Testing Guidelines

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

### Test Structure
- Mirror the Api project structure (e.g., `Api/Services/CustomerService.cs` -> `Tests/Services/CustomerServiceTests.cs`).
- Use `[Fact]` for single cases and `[Theory]` for parameterized tests.

### Mocking
- Mock external dependencies (`IIAMClient`, `IPublishEndpoint`) using Moq.
- Use `MockBehavior.Strict` if precise interaction verification is needed, otherwise `MockBehavior.Default` (Loose) is acceptable as seen in codebase.

### Integration Tests
- Use `TestWebApplicationFactory` for end-to-end integration tests.
- Ensure database state is reset between tests (use `ClearDatabaseAsync` or similar fixtures).
- Verify side effects (Database persistence, Event publication).

Example Test:
```csharp
[Fact]
public async Task GetByIdAsync_WithExistingCustomer_ReturnsCustomer()
{
    // Arrange
    var service = CreateService();
    var id = Guid.NewGuid();

    // Act
    var result = await service.GetByIdAsync(id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(id, result.Id);
}
```

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

---

## 6. Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("customer.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with `/customer`
- **Scalar docs**: Configured at `/customer/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
  - Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
  - Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
  - Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

---

## 7. Environment & Configuration

- **Development:** `appsettings.Development.json`
- **Secrets:** Do not commit secrets. Use User Secrets, GCP Secret Manager, or Environment Variables.
- **Database:** PostgreSQL is used in production. Integration tests use a real database instance via Testcontainers.

---

## 8. Git Rules

- This is an independent git repo within the workspace. Run git commands from within `B:\maliev\Maliev.CustomerService`
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked
- **Commits:** meaningful commit messages (e.g., "feat: add customer search", "fix: handle null email")
- **Branches:** feature branches off `main` or `develop`
- **Review:** All code changes must pass tests before merging

---
*Generated for AI Agents interacting with the Maliev.CustomerService repository.*
