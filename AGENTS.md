# Maliev.CustomerService Agent Guidelines

This document provides essential instructions, commands, and standards for AI agents and developers working on the Maliev.CustomerService repository.

## 1. Build and Test Commands

The project uses the .NET CLI and follows standard conventions.

### Build
To build the entire solution:
```bash
dotnet build Maliev.CustomerService.slnx
```

To build a specific project (e.g., Api):
```bash
dotnet build Maliev.CustomerService.Api/Maliev.CustomerService.Api.csproj
```

### Testing
The project uses **xUnit** for testing.

**Run all tests:**
```bash
dotnet test
```

**Run tests for a specific project:**
```bash
dotnet test Maliev.CustomerService.Tests/Maliev.CustomerService.Tests.csproj
```

**Run a single specific test:**
Use the `--filter` option with the Fully Qualified Name (FQN).
```bash
dotnet test --filter "FullyQualifiedName~Maliev.CustomerService.Tests.Services.CustomerServiceTests.CreateAsync_WithValidData_ReturnsCustomerResponse"
```

**Run all tests in a class:**
```bash
dotnet test --filter "FullyQualifiedName~Maliev.CustomerService.Tests.Services.CustomerServiceTests"
```

**Run tests matching a pattern:**
```bash
dotnet test --filter "DisplayName~CreateAsync"
```

### Linting & Code Quality
Ensure code adheres to standard .NET coding conventions.
```bash
dotnet format
```

## 2. Code Style & Standards

### General
- **Framework:** .NET 10.0 (C# 13/14 features available).
- **Nullable Reference Types:** Enabled. Explicitly handle null states.
- **Async/Await:** Use `async/await` for all I/O bound operations. Avoid `.Result` or `.Wait()`.

### Naming Conventions
- **Classes/Methods/Properties:** PascalCase (e.g., `CustomerService`, `CreateAsync`).
- **Interfaces:** PascalCase with 'I' prefix (e.g., `ICustomerService`).
- **Private Fields:** camelCase with underscore prefix (e.g., `_context`, `_logger`).
- **Parameters/Locals:** camelCase (e.g., `customerId`, `request`).
- **Async Methods:** Suffix with `Async` (e.g., `GetByIdAsync`).
- **DTOs:** Suffix with `Request`, `Response`, or `Dto` (e.g., `CreateCustomerRequest`).

### Imports (Using Directives)
- Place `using` directives at the top of the file.
- Remove unused `using` directives.
- Use file-scoped namespace declarations (e.g., `namespace Maliev.CustomerService.Api.Services;`).

### Formatting
- Use K&R style braces (standard C# default).
- Indentation: 4 spaces.
- Line length: Aim for < 120 characters, but readability takes precedence.

### Dependency Injection
- Use Constructor Injection for all dependencies.
- Assign injected dependencies to `readonly` private fields.
- Example:
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

### Error Handling
- Use specific exceptions where possible (e.g., `KeyNotFoundException`, `InvalidOperationException`).
- **Logging:** Log exceptions with context before throwing or handling.
- Use `ILogger` extensions (`LogInformation`, `LogError`, `LogWarning`).
- Structured logging is preferred:
  ```csharp
  _logger.LogInformation("Creating customer with email {Email} by actor {ActorId}", request.Email, actorId);
  ```

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

## 3. Architecture & Patterns

### Project Structure (Clean Architecture)
- **Maliev.CustomerService.Api:** Controllers, Middleware.
- **Maliev.CustomerService.Application:** Use cases, handlers, DTOs.
- **Maliev.CustomerService.Domain:** Entities, interfaces, value objects.
- **Maliev.CustomerService.Infrastructure:** EF Core, repositories, external services.
- **Maliev.CustomerService.Tests:** Integration tests (Testcontainers).

### Entity Framework Core Migrations

- **EF Core Design Package:** Only install `Microsoft.EntityFrameworkCore.Design` in the **Infrastructure** project. Do NOT add it to Api or other projects.
- **Creating Migrations:**
  ```bash
  dotnet ef migrations add <Name> --project Maliev.CustomerService.Infrastructure --startup-project Maliev.CustomerService.Api
  ```
- **Applying Migrations:**
  ```bash
  dotnet ef database update --project Maliev.CustomerService.Infrastructure --startup-project Maliev.CustomerService.Api
  ```

### Key Libraries
- **Entity Framework Core:** Data access. Use `Async` methods (`ToListAsync`, `FirstOrDefaultAsync`).
- **MassTransit:** Event bus/messaging (`IPublishEndpoint`).
- **xUnit:** Testing framework.

### Layering
- **Controllers:** Minimal logic. Delegate to Handlers/Services.
- **Application Layer:** Use cases, CQRS handlers.
- **Domain Layer:** Business logic, entities, interfaces.
- **Infrastructure:** Data access, external integrations.

## 4. Testing Guidelines

### Test Structure
- Mirror the Api project structure (e.g., `Api/Services/CustomerService.cs` -> `Tests/Services/CustomerServiceTests.cs`).
- Use `[Fact]` for single cases and `[Theory]` for parameterized tests.

### Naming Tests
- Format: `MethodName_Condition_ExpectedResult`
- Example: `CreateAsync_WithDuplicateEmail_ThrowsInvalidOperationException`

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

## 5. Environment & Configuration

- **Development:** `appsettings.Development.json`
- **Secrets:** Do not commit secrets. Use User Secrets or Environment Variables.
- **Database:** PostgreSQL is used in production. Integration tests use a real database instance (via containers or local setup).

## 6. Git Workflow

- **Commits:** meaningful commit messages (e.g., "feat: add customer search", "fix: handle null email").
- **Branches:** feature branches off `main` or `develop`.
- **Review:** All code changes must pass tests before merging.

---
*Generated for AI Agents interacting with the Maliev.CustomerService repository.*


## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure, not Api:
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project ../Maliev.<Domain>Service.Api
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
