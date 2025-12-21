# Research: Principal-First Model Migration

## Decisions

### Decision 1: IAM Client Implementation
**Rationale**: Using `HttpClient` with a dedicated interface (`IIAMClient`) follows the project's dependency injection patterns and allows for resilient communication using `Microsoft.Extensions.Http.Resilience`.
**Alternatives considered**: Direct database access to IAM (Rejected: Violates Service Autonomy); Generic Message Bus for creation (Rejected: Need synchronous PrincipalId for Customer creation).

### Decision 2: Batch Migration Strategy
**Rationale**: Migrating customers in batches of 100 ensures the database transaction log remains manageable and the IAM service is not overwhelmed by a single massive request.
**Alternatives considered**: Single transaction migration (Rejected: High risk of timeout/lock escalation).

### Decision 3: Identity Cleanup Timing
**Rationale**: Identity tables and code will be removed only after a 1-week verification period in production to ensure rollback is possible if unforeseen issues arise.
**Alternatives considered**: Immediate removal (Rejected: Too high risk).

## Technical Unknowns & Findings

### Unknown 1: IAM API Endpoint & Authentication
- **Finding**: The IAM service is expected to expose `POST /api/v1/principals`. Authentication will be via Service Account Token (Bearer) injected from Google Secret Manager in production.
- **Reference**: Architecture guidelines for Maliev Cloud.

### Unknown 2: Backfill Failures
- **Finding**: If a principal creation fails during backfill, the script will log the error and continue with the next customer. A retry mechanism will be included in the script.

### Unknown 3: ASP.NET Core Identity Removal Impact
- **Finding**: Removing `Microsoft.AspNetCore.Identity.EntityFrameworkCore` will require updating `CustomerDbContext` to inherit from `DbContext` instead of `IdentityDbContext<ApplicationUser, ...>`.

## Best Practices

- **Resilience**: Use exponential backoff for IAM API calls.
- **Consistency**: Ensure `PrincipalId` is indexed and has a unique constraint (nullable during migration, NOT NULL after).
- **Audit**: Log all migration actions, including both successes and failures.
