# Research: Principal-First Model Migration

## Decisions

### Decision 1: IAM Client Implementation
**Rationale**: Using `HttpClient` with `Microsoft.Extensions.Http.Resilience` ensures the system can handle transient failures when communicating with the IAM service.
**Alternatives considered**: MassTransit Request/Response (Rejected: Need synchronous flow for registration UI; simpler HTTP client is sufficient).

### Decision 2: Batch Migration Strategy
**Rationale**: Processing 100 customers per batch prevents memory bloat and long-running database transactions while keeping the IAM service load manageable.
**Alternatives considered**: Sequential processing (Too slow); Full parallel processing (Risk of rate-limiting IAM).

### Decision 3: Identity Cleanup Archive
**Rationale**: Archiving legacy tables for 90 days before dropping them ensures we can recover from any late-detected migration issues.
**Alternatives considered**: Permanent retention (Rejected: PII cleanup policy); Immediate deletion (Rejected: High risk).

## Technical Context Research

### ASP.NET Identity Removal
- Removing `Microsoft.AspNetCore.Identity.EntityFrameworkCore` requires updating `CustomerDbContext` to inherit from `DbContext` instead of `IdentityDbContext`.
- All `AspNet*` tables must be dropped in the final cleanup phase.

### Resilience Patterns
- The `IAMClient` will use a standard retry policy (3 attempts) and a circuit breaker to prevent cascading failures if IAM is down.