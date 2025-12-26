# Implementation Plan: Principal-First Model Migration

**Branch**: `002-principal-model-migration` | **Date**: 2025-12-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-principal-model-migration/spec.md`

## Summary

Migrate CustomerService from ASP.NET Core Identity-based authentication to a principal-first model where identity is owned by the IAM service. This involves adding a `PrincipalId` to the `Customer` entity, implementing an IAM client for principal creation, backfilling existing data via a migration script, and eventually removing all legacy Identity code and tables.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: Microsoft.EntityFrameworkCore (10.0.0), Microsoft.Extensions.Http.Resilience (10.0.0), Scalar.AspNetCore (2.11.0), MassTransit (8.5.7)  
**Storage**: PostgreSQL (via Npgsql), Redis (for caching)  
**Testing**: xUnit, Testcontainers (PostgreSQL, Redis, RabbitMQ)  
**Target Platform**: Linux (Docker)
**Project Type**: Web API + Data Library  
**Performance Goals**: < 50ms p95 for Get Customer by Central Identity lookup.  
**Constraints**: Zero data loss during migration; 90-day archive for legacy identity tables.  
**Scale/Scope**: All existing customers must be migrated; all new customers must use IAM principals.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Service Autonomy**: Compliant. CustomerService owns business data, IAM owns identity.
- **II. Explicit Contracts**: Compliant. New endpoints documented via OpenAPI/Scalar.
- **III. Test-First Development**: Compliant. Plan includes unit and integration tests with Testcontainers.
- **IV. Real Infrastructure Testing**: Compliant. Mandatory use of Testcontainers for PostgreSQL and Redis.
- **VII. Secrets Management**: Compliant. IAM ServiceAccountToken injected via configuration (Google Secret Manager in prod).
- **XIV. Code Quality**: Compliant. No AutoMapper/FluentValidation/FluentAssertions used. Explicit mapping only.
- **XV. Project Structure**: Compliant. Flat project structure at root.

## Project Structure

### Documentation (this feature)

```text
specs/002-principal-model-migration/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── checklists/          # Validation checklists
```

### Source Code (repository root)

```text
Maliev.CustomerService.Api/
├── Controllers/
│   └── CustomerController.cs (Modified)
├── Models/
│   └── IAM/ (New)
├── Services/
│   ├── IAMClient.cs (New)
│   └── CustomerService.cs (Modified)
└── Program.cs (Modified)

Maliev.CustomerService.Data/
├── Models/
│   └── Customer.cs (Modified)
└── Migrations/
    └── YYYYMMDDHHMMSS_AddPrincipalIdToCustomers.cs (New)

Maliev.CustomerService.Tests/
├── Integration/
│   └── CustomerControllerTests.cs (New)
└── Unit/
    ├── IAMClientTests.cs (New)
    └── CustomerServiceTests.cs (Modified)
```

**Structure Decision**: Standard flat structure following Constitution Rule XV.

## Complexity Tracking

*No violations detected.*

---

## Detailed Execution Plan (from customer-plan.md)

### Phase 1: Add principal_id Column
**Goal**: Add principal_id column to customers table without breaking existing functionality

1. Create EF Core migration: `dotnet ef migrations add AddPrincipalIdToCustomers` in `Maliev.CustomerService.Data`.
2. Review migration: Adds `principal_id` UUID NULL column and index `idx_customers_principal`.
3. Test migration: `dotnet ef database update`.

### Phase 2: Create IAM Client
**Goal**: Implement HTTP client for IAM principal creation

1. Add Models: `CreatePrincipalRequest.cs`, `CreatePrincipalResponse.cs`.
2. Create `IIAMClient` interface.
3. Implement `IAMClient` using `HttpClient`.
4. Register in `Program.cs` with resilience and bearer token.
5. Write unit tests with mocked responses.

### Phase 3: Backfill Migration Script
**Goal**: Create principals for all existing customers

1. Create `MigrateToPrincipalsScript.cs`.
2. Implement batch processing (100 customers per batch).
3. Add CLI command `--migrate-principals` to `Program.cs`.
4. Test in development and staging with backups.

### Phase 4: Update Customer Creation
**Goal**: New customers automatically get principals

1. Modify `CustomerService.CreateAsync` to call IAM.
2. Add feature flag `Features:PrincipalBasedAuthEnabled`.
3. Implement error handling and transactional rollback.
4. Write unit and integration tests.

### Phase 5: Add GetByPrincipalId Endpoint
**Goal**: Enable lookup by `principal_id`

1. Add `GetByPrincipalIdAsync` to service.
2. Add `GET /by-principal/{principalId}` endpoint.
3. Add database index `idx_customers_principal_lookup`.
4. Write unit and integration tests.

### Phase 6: Update Credential Validation
**Goal**: Return `principal_id` instead of `userId`

1. Update `CredentialValidationResponse` model.
2. Modify `POST /validate` endpoint to use `PrincipalId`.

### Phase 7: Production Migration
**Goal**: Execute backfill in production with 2-hour maintenance window and backup.

### Phase 8: Enable Principal-Based Auth
**Goal**: Enable feature flag in production and monitor.

### Phase 9: Cleanup
**Goal**: Remove ASP.NET Identity code and tables after 1 week soak time.
1. Make `principal_id` NOT NULL.
2. Add unique constraint.
3. Remove `ApplicationUser` and Identity configuration.
4. Drop Identity tables.