# Research & Technical Decisions: Customer Service Microservice

**Date**: 2025-10-08
**Feature**: Customer Service Microservice
**Phase**: 0 - Research & Architecture Decisions

## Overview

This document captures research findings, technical decisions, and rationale for implementing the Customer Service microservice. All decisions align with the Maliev microservices constitution and established patterns.

## Technology Stack Decisions

### Decision: .NET 9 with Entity Framework Core 9.0.9
**Rationale**:
- Latest LTS version with performance improvements and native AOT support
- Entity Framework Core 9.0.9 provides stable PostgreSQL integration via Npgsql 9.0.2
- Strong typing and compile-time safety reduce runtime errors
- Comprehensive ecosystem for microservices (health checks, logging, versioning)
- Team familiarity and existing Maliev microservices use .NET 9

**Alternatives Considered**:
- .NET 8 LTS: Rejected - .NET 9 provides better performance and newer features
- Dapper for data access: Rejected - EF Core provides better productivity with migrations, change tracking, and LINQ queries while meeting performance requirements

### Decision: PostgreSQL 18 with Snake_Case Naming
**Rationale**:
- PostgreSQL 18 provides robust ACID compliance, JSON support, and horizontal scalability
- Snake_case naming convention (`customer_app_db`, column names) aligns with PostgreSQL best practices
- Strong support for optimistic concurrency via bytea row versioning
- Excellent performance for read-heavy workloads (customer lookups, queries)
- Native support for connection pooling (Npgsql)

**Alternatives Considered**:
- SQL Server: Rejected - Higher licensing costs, PostgreSQL preferred standard
- MongoDB: Rejected - ACID transactions and relational integrity critical for customer master data

### Decision: Serilog 8.0.2 for Structured Logging
**Rationale**:
- Industry-standard structured logging for .NET applications
- JSON output to stdout aligns with container/Kubernetes logging best practices
- High performance with minimal overhead via LoggerMessage delegates
- Strong integration with ASP.NET Core pipeline
- Enables log aggregation and analysis in centralized logging systems

**Alternatives Considered**:
- Built-in ILogger: Rejected - Less flexible for structured logging and custom sinks
- NLog: Rejected - Serilog has better ecosystem and configuration flexibility

## Architecture & Design Pattern Decisions

### Decision: Clean Architecture (Controllers → Services → Data)
**Rationale**:
- Clear separation of concerns: API layer, business logic, data access
- Testable design with dependency injection
- Aligns with Maliev microservices constitution (Principle IX: Simplicity)
- Avoids over-engineering with repository/unit-of-work patterns (YAGNI principle)
- Services directly use DbContext for data access (simpler than repository abstraction)

**Alternatives Considered**:
- Repository pattern: Rejected - Adds unnecessary abstraction over EF Core which already implements repository/unit-of-work
- CQRS: Rejected - Not needed for CRUD operations, would violate simplicity principle
- Vertical slice architecture: Rejected - Clean architecture provides better structure for this domain

### Decision: Optimistic Concurrency with RowVersion Byte Array
**Rationale**:
- Prevents lost updates in concurrent modification scenarios
- RowVersion automatically managed by database (no manual versioning logic)
- PostgreSQL bytea type with `HasDefaultValueSql("'\\x0000000000000000'::bytea")` pattern
- Returns 409 Conflict on version mismatch with clear error messages
- Lightweight compared to pessimistic locking (no database locks held)

**Alternatives Considered**:
- Pessimistic locking: Rejected - Reduces concurrency and can cause deadlocks
- Timestamp column: Rejected - RowVersion is database-agnostic and explicitly designed for concurrency
- No concurrency control: Rejected - Risk of lost updates unacceptable for customer master data

### Decision: FluentValidation 11.5.1 for Request Validation
**Rationale**:
- Declarative validation rules separate from business logic
- Async validation support for external service calls (country validation, NDA checks)
- Rich error messages with field-level detail
- Better testability than data annotations
- Chainable rules for complex validation logic

**Alternatives Considered**:
- Data Annotations: Rejected - Limited expressiveness, cannot handle async validation
- Manual validation in controllers: Rejected - Violates separation of concerns, hard to test

## External Service Integration Decisions

### Decision: Typed HttpClient with Polly Retry Policies
**Rationale**:
- Typed HttpClient pattern provides clean interface abstraction
- Polly retry with exponential backoff (3 attempts, 2^n second delays) handles transient failures
- Timeout configuration via ExternalServiceOptions (180s standard, 300s for uploads)
- Circuit breaker pattern prevents cascading failures
- Dependency injection makes testing easier (mock HTTP clients)

**Alternatives Considered**:
- RestSharp: Rejected - Typed HttpClient provides better integration with .NET dependency injection
- gRPC: Rejected - REST preferred for inter-service communication (broader compatibility)
- Manual HttpClient: Rejected - Typed pattern enforces better practices and testability

### Decision: Health Checks for All External Dependencies
**Rationale**:
- `/customers/liveness` - Lightweight process health (Kubernetes liveness probe)
- `/customers/readiness` - Comprehensive dependency health (database + Upload/Country/Identity services)
- Enables Kubernetes to stop routing traffic to unhealthy instances
- AspNetCore.HealthChecks.UI.Client provides standard response format
- Early detection of dependency failures

**Alternatives Considered**:
- Single health endpoint: Rejected - Separate liveness/readiness provides better control
- No external service checks: Rejected - Blind to dependency failures until user impact

## Security & Authentication Decisions

### Decision: ASP.NET Core Identity Scaffolding with Custom Extensions
**Rationale**:
- Start with ASP.NET Core Identity scaffold for proven user management foundation
- Provides built-in password hashing (PBKDF2), user store, role management
- Standard tables: AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims
- Extend with custom User entity linked to AspNetUsers via one-to-one relationship
- Custom linkedCustomerId field added to track Customer relationship
- JWT Bearer authentication built on top of Identity framework
- Role-based authorization policies (Customer role) with claims-based access control

**Implementation Approach**:
1. Scaffold ASP.NET Core Identity using `dotnet aspnet-codegenerator identity`
2. Customize IdentityUser to add linkedCustomerId navigation property
3. Configure Identity with password requirements and lockout policies
4. Add Customer-specific claims during token generation
5. Implement `/customers/v1/validate` endpoint using UserManager and SignInManager

**Alternatives Considered**:
- Custom user table from scratch: Rejected - ASP.NET Core Identity provides battle-tested implementation
- OAuth2/OpenID Connect: Rejected - JWT with Identity sufficient for inter-service auth
- API Keys: Rejected - Less secure, no user context, harder to rotate

### Decision: Rate Limiting (Fixed + Sliding Window)
**Rationale**:
- Fixed window: 100 req/min per IP for general endpoints (prevents basic abuse)
- Sliding window: 10 req/min per IP for batch operations (prevents resource exhaustion)
- ASP.NET Core 9 built-in rate limiting (no external dependencies)
- OnRejected handler returns 429 with retry-after metadata
- Per-IP partitioning prevents one client from affecting others

**Alternatives Considered**:
- AspNetCoreRateLimit package: Rejected - Built-in rate limiting sufficient and better integrated
- Token bucket: Rejected - Fixed/sliding windows simpler to understand and configure

## Data Model & Validation Decisions

### Decision: Actor Type Classification in Audit Logs
**Rationale**:
- `actorType` enum: Customer, Employee, System
- Distinguishes self-service (customer) from administrative (employee) modifications
- Supports compliance and security audits (who changed what)
- Derived from JWT claims (userType) automatically
- No additional service calls required

**Alternatives Considered**:
- Single actor identifier: Rejected - Cannot distinguish customer vs employee actions
- Separate audit tables: Rejected - Single table with type is simpler and queryable

### Decision: Country Service Integration for CountryId
**Rationale**:
- External Country Service owns country master data
- Customer Service stores only countryId reference (foreign key pattern)
- Validates countryId exists before accepting address create/update
- Decouples country management from customer service
- Follows service autonomy principle (Principle I)

**Alternatives Considered**:
- Store country names: Rejected - Data duplication, synchronization issues
- No validation: Rejected - Risk of invalid references, data integrity violations

### Decision: Upload Service Integration for Document Management
**Rationale**:
- Upload Service handles file storage, Customer Service handles metadata
- DocumentReference entity stores fileReference (Upload Service ID) only
- Validates file exists before creating document reference
- Deferred deletion with retry on Upload Service unavailability
- Clear separation of concerns (files vs metadata)

**Alternatives Considered**:
- Store files in database: Rejected - Poor performance, bloated database size
- Direct file system storage: Rejected - Not suitable for containerized/distributed environment

## Testing Strategy Decisions

### Decision: Real PostgreSQL Database for Tests
**Rationale**:
- TestDatabaseFixture with actual PostgreSQL validates real database behavior
- Auto-applies migrations before tests (ensures schema correctness)
- docker-compose.test.yml for easy local setup
- Catches PostgreSQL-specific issues (row versioning, snake_case, constraints)
- Clear error messages when database unavailable (no in-memory fallback)

**Alternatives Considered**:
- In-memory database: Rejected - Doesn't match PostgreSQL behavior (row versioning, case sensitivity)
- Mocked DbContext: Rejected - Doesn't validate EF configuration or migrations

### Decision: Test Auth Handler with Admin Claims
**Rationale**:
- TestAuthHandler bypasses JWT validation in Testing environment
- Returns Admin role with all permissions (simplifies test setup)
- Includes userType=employee claim for actor type tests
- Standard ASP.NET Core authentication schemes
- Authorization policies still enforced (validates policy logic)

**Alternatives Considered**:
- Real JWT tokens in tests: Rejected - Complex setup, slow, fragile
- No authentication in tests: Rejected - Cannot validate authorization logic

## Performance Optimization Decisions

### Decision: Simple MemoryCache Without SizeLimit
**Rationale**:
- `AddMemoryCache()` without SizeLimit configuration
- CRITICAL: Avoids runtime exceptions from missing Size property on cache entries
- Cache external service data (24-hour TTL for relatively static data)
- TryGetValue pattern checks cache before external calls
- Cache invalidation on entity updates

**Alternatives Considered**:
- MemoryCache with SizeLimit: Rejected - Requires Size on all entries, adds complexity
- Distributed cache (Redis): Rejected - Not needed for microservice stateless design
- No caching: Rejected - Reduces performance for external service calls

### Decision: Async/Await for All I/O Operations
**Rationale**:
- Non-blocking I/O throughout application (database, HTTP, file operations)
- Improves scalability and resource utilization
- Required for meeting p95 latency < 150ms requirements
- ASP.NET Core async pipeline designed for async operations
- Connection pooling maximized with async patterns

**Alternatives Considered**:
- Synchronous operations: Rejected - Blocks threads, reduces scalability
- Mixed sync/async: Rejected - Inconsistent patterns, hard to maintain

## Deployment & DevOps Decisions

### Decision: Manual Database Migrations
**Rationale**:
- Migrations created via `dotnet ef migrations add` command
- Applied manually via `dotnet ef database update` (NOT auto-applied on startup)
- Design-time DbContext factory (CustomerDbContextFactory) enables migrations without running app
- Prevents accidental schema changes in production
- Allows review and testing of migrations before application

**Alternatives Considered**:
- Auto-apply migrations on startup: Rejected - Dangerous in production, race conditions in multi-instance deployments
- Database-first approach: Rejected - Code-first provides better version control and team workflow

### Decision: Docker Multi-Stage Build with Non-Root User
**Rationale**:
- Build stage: .NET SDK 9.0 for restore, build, publish
- Runtime stage: ASP.NET 9.0 for minimal image size
- Non-root user (appuser, UID 1000) for security
- PostgreSQL client installed for health checks
- HEALTHCHECK validates `/customers/liveness` endpoint
- Layer optimization (separate COPY for project files before source code)

**Alternatives Considered**:
- Single-stage build: Rejected - Larger image size (includes SDK)
- Root user: Rejected - Security risk, violates least privilege principle

## Configuration & Secrets Management Decisions

### Decision: Google Secret Manager with Double Underscore Convention
**Rationale**:
- All secrets mounted at `/mnt/secrets` in Kubernetes
- Double underscore converts to colon in IConfiguration: `ConnectionStrings__CustomerDbContext`
- Strongly-typed ExternalServiceOptions classes for external services
- Environment-specific JWT issuers: maliev-dev, maliev-staging, maliev-prod
- No secrets in source code (constitution Principle VI)

**Alternatives Considered**:
- Environment variables directly: Rejected - Less secure than Secret Manager
- Azure Key Vault: Rejected - Google Secret Manager is infrastructure standard
- appsettings files: Rejected - Risk of committing secrets to source control

## Business Intelligence & Localization Decisions

### Decision: Customer Segmentation Enums (Retail, Wholesale, Enterprise, Government)
**Rationale**:
- Enables downstream services (Quoting Service, Marketing Service) to apply targeted business rules
- Retail: Individual consumers and small-scale buyers
- Wholesale: Bulk purchasers and distributors
- Enterprise: Large corporate customers with complex purchasing workflows
- Government: Public sector entities with specialized compliance requirements
- Stored as VARCHAR(20) with CHECK constraint for data integrity
- Indexed for fast filtering in business intelligence queries
- Nullable to support gradual migration and optional classification

**Alternatives Considered**:
- Numeric codes: Rejected - Less readable in logs and reports, requires lookup table
- Free-form text: Rejected - Data inconsistency risk, no validation
- Separate lookup table: Rejected - Over-engineering for stable, small enum set

### Decision: Customer Tier Enums (Bronze, Silver, Gold, Platinum, VIP)
**Rationale**:
- Supports differentiated service levels and pricing strategies
- Aligns with common industry practice for customer loyalty programs
- Enables Quoting Service to apply tier-based discounts automatically
- Bronze: Entry-level customers, standard pricing
- Silver: Regular customers, basic discounts
- Gold: Premium customers, enhanced service
- Platinum: Top-tier customers, priority support
- VIP: Strategic accounts requiring white-glove service
- Indexed for marketing campaign targeting and reporting

**Alternatives Considered**:
- Numeric tiers (1-5): Rejected - Less intuitive, requires documentation lookup
- Percentage-based: Rejected - Doesn't capture non-pricing service differentiation
- Custom tier names: Rejected - Need standardized vocabulary across services

### Decision: Company Segmentation Enums (SMB, MidMarket, Enterprise, Government)
**Rationale**:
- Standard B2B market segmentation aligned with sales and account management practices
- SMB (Small/Medium Business): <100 employees, simpler sales cycles
- MidMarket: 100-1000 employees, growing complexity
- Enterprise: 1000+ employees, complex procurement and approval workflows
- Government: Public sector with specialized compliance and contract requirements
- Enables account-based pricing strategies via downstream services
- Supports sales territory planning and quota assignment

**Alternatives Considered**:
- Revenue-based segmentation: Rejected - Revenue not tracked in Customer Service
- Employee count ranges: Rejected - Too granular, harder to maintain consistency
- Industry-based only: Rejected - Doesn't capture business size for pricing rules

### Decision: Company Tier Enums (Standard, Premium, Strategic, Partner)
**Rationale**:
- Reflects relationship depth and business value beyond size
- Standard: Transactional relationship, standard terms
- Premium: Regular business, negotiated pricing
- Strategic: High-value accounts, dedicated account management
- Partner: Technology/channel partners, special commercial terms
- Distinct from customer tier (company relationship vs individual service level)
- Supports B2B contract management and partnership workflows

**Alternatives Considered**:
- Reuse customer tiers: Rejected - Company and individual relationships have different dynamics
- Value-based tiers: Rejected - Value metrics vary by business model

### Decision: ISO 639-1 Two-Letter Language Codes for Preferred Language
**Rationale**:
- International standard ensuring interoperability across services
- Two-letter codes (en, th, zh, ja, ko) are compact and universally recognized
- Supports localization for Marketing Service, Notification Service, and customer-facing applications
- Validated via regex pattern `^[a-z]{2}$` in OpenAPI contracts
- Indexed for fast filtering when sending localized communications
- Nullable to support customers without explicit preference (default to system language)

**Alternatives Considered**:
- ISO 639-2 (three-letter codes): Rejected - Unnecessary complexity for common languages
- Full locale codes (en-US, en-GB): Rejected - Over-specification for current requirements, can extend later
- Language names: Rejected - Ambiguity (Chinese vs 中文), no standard validation

### Decision: IANA Timezone Identifiers for Timezone
**Rationale**:
- Industry standard for timezone representation (e.g., Asia/Bangkok, America/New_York)
- Handles daylight saving time transitions automatically
- Required for scheduling localized communications at appropriate times
- Enables Marketing Service to send emails/SMS during business hours in customer's timezone
- Validated against IANA timezone database (e.g., NodaTime library in .NET)
- Nullable to support customers without explicit preference

**Alternatives Considered**:
- UTC offsets (+07:00): Rejected - Doesn't handle DST, loses semantic location information
- Custom timezone names: Rejected - No standard, validation complexity
- Windows timezone IDs: Rejected - IANA standard more portable across platforms

### Decision: JSONB for Communication Preferences
**Rationale**:
- Flexible schema allows downstream services to define their own preference keys
- No predefined structure enforced by Customer Service (schema-less)
- Common keys: email_opt_in, sms_opt_in, marketing_opt_in, newsletter_opt_in
- PostgreSQL JSONB provides efficient indexing and querying capabilities
- Supports compliance with GDPR, PDPA (data subject consent tracking)
- Avoids creating new columns for each communication channel
- Future-proof: new channels (WhatsApp, LINE, etc.) can be added without schema migration

**Example**:
```json
{
  "email_opt_in": true,
  "sms_opt_in": false,
  "marketing_opt_in": true,
  "newsletter_opt_in": false,
  "preferred_contact_method": "email"
}
```

**Alternatives Considered**:
- Separate boolean columns: Rejected - Schema changes required for new channels
- JSON (not JSONB): Rejected - JSONB provides better performance for queries
- External Preferences Service: Rejected - Over-engineering, preferences are customer attributes

### Decision: Last Login Timestamp (last_login_at) for Activity Tracking
**Rationale**:
- Tracks most recent successful authentication for security auditing and compliance
- Updated automatically on successful validation via `/customers/v1/validate` endpoint
- Enables inactive account detection (e.g., accounts not used in 90+ days)
- Supports compliance reporting for security audits
- Indexed for efficient queries to identify inactive users
- Nullable (null = never logged in)
- Stored as TIMESTAMPTZ (UTC) for consistency across timezones

**Use Cases**:
- Security: Identify compromised accounts with unusual login patterns
- Compliance: Demonstrate active user monitoring for audit requirements
- Account cleanup: Automatically flag or archive inactive accounts
- Customer engagement: Re-engagement campaigns for dormant users

**Alternatives Considered**:
- Separate LoginHistory table: Rejected - Only need latest login for current use cases
- Store in AuditLog only: Rejected - Inefficient queries, no dedicated index
- External Analytics Service: Rejected - Last login is core customer data attribute

## Summary

All technical decisions align with:
- Maliev microservices constitution (all 9 principles)
- Performance requirements (p95 < 150ms, 1000 concurrent operations)
- Security requirements (JWT, RBAC, secrets management, audit trails)
- Simplicity principles (Clean Architecture, no over-engineering)
- Testability requirements (real PostgreSQL, 80% coverage)

No unresolved questions or NEEDS CLARIFICATION markers remain. Design is ready for Phase 1 (data model and API contracts).
