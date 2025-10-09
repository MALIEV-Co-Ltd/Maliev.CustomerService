# Implementation Plan: Customer Service Microservice

**Branch**: `001-customer-service-microservice` | **Date**: 2025-10-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-customer-service-microservice/spec.md`

## Summary

Customer Service microservice manages customer and company master data, user account management, addresses, documents, NDAs, and internal notes. Provides customer segmentation (Retail/Wholesale/Enterprise/Government) and tiering (Bronze/Silver/Gold/Platinum/VIP) for downstream services like Quoting and Marketing. Implements localization preferences (ISO 639-1 language codes, IANA timezones) and communication preferences (JSON opt-in/opt-out) for personalized customer experience. Uses ASP.NET Core Identity for authentication with last_login_at tracking for security auditing.

**Technical Approach**: .NET 9 WebAPI with Clean Architecture (Controllers → Services → Data), Entity Framework Core 9.0.9 with PostgreSQL 18, optimistic concurrency control via row versioning, FluentValidation for requests, Polly for resilient external service integration (Upload Service, Country Service), Serilog for structured logging, JWT Bearer authentication, and rate limiting.

## Technical Context

**Language/Version**: .NET 9.0 (ASP.NET Core 9.0)
**Primary Dependencies**: Entity Framework Core 9.0.9, Npgsql 9.0.2, ASP.NET Core Identity 9.0.8, FluentValidation 11.5.1, Polly 8.0.0, Serilog 8.0.2
**Storage**: PostgreSQL 18 with snake_case naming convention
**Testing**: xUnit 2.4.2, FluentAssertions 8.6.0, Moq 4.20.72, Microsoft.AspNetCore.Mvc.Testing 9.0.0
**Target Platform**: Linux containers (Docker) deployed to Google Kubernetes Engine (GKE)
**Project Type**: Microservice API (3-project solution: Api, Data, Tests)
**Performance Goals**: p95 latency < 150ms for GET, < 200ms for POST/PATCH, 1000 concurrent operations
**Constraints**: Stateless design for horizontal scaling, optimistic concurrency control, 99.9% uptime SLA
**Scale/Scope**: 8 core entities, 127 functional requirements, 22 non-functional requirements, 9 user stories, 141 implementation tasks

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Validation |
|-----------|--------|------------|
| **I. Service Autonomy** | ✅ **PASS** | Own PostgreSQL database (`customer_app_db`), no direct database access to other services. Communicates with Upload Service and Country Service via REST APIs only. |
| **II. Explicit Contracts** | ✅ **PASS** | OpenAPI specification defined in `contracts/openapi.yaml`, versioned API endpoints (`/customers/v1/`), backward-compatible schema evolution via nullable fields. |
| **III. Test-First Development** | ⚠️ **EXCEPTION** | Tests NOT included in initial implementation per feature spec. **Justification**: Rapid MVP delivery for prototype phase. Tests will be added in iteration 2 targeting 80% coverage per constitution requirement. See Complexity Tracking below. |
| **IV. Auditability & Observability** | ✅ **PASS** | AuditLog entity tracks all mutations with actorType (Customer/Employee/System). Structured JSON logging via Serilog to stdout. Health checks: `/customers/liveness` (process), `/customers/readiness` (dependencies). |
| **V. Security & Compliance** | ✅ **PASS** | JWT Bearer authentication with issuer/audience validation. Role-based authorization (Customer, Employee, Manager, Admin). Password hashing via ASP.NET Core Identity (PBKDF2). Communication preferences support GDPR/PDPA compliance. |
| **VI. Secrets Management** | ✅ **PASS** | All secrets via Google Secret Manager mounted at `/mnt/secrets`. Database connection strings, JWT keys, external service URLs loaded from environment variables. No secrets in appsettings.json (localhost placeholders only). |
| **VII. Zero Warnings Policy** | ✅ **PASS** | CI/CD pipeline (T129) enforces warnings-as-errors. Build validation task (T134) verifies zero warnings. |
| **VIII. Clean Project Artifacts** | ✅ **PASS** | .gitignore excludes `bin/`, `obj/`, `.vs/`, IDE files. No boilerplate scaffolding files committed. Regular cleanup enforced in PR reviews. |
| **IX. Simplicity & Maintainability** | ✅ **PASS** | Clean Architecture (Controllers → Services → Data) without repository pattern over-engineering. Services directly use DbContext (EF Core already implements repository/unit-of-work). YAGNI principle applied. |

**Overall Status**: 8 PASS, 1 EXCEPTION (Test-First Development)

**Deployment Standards**: ✅ Containerized (Dockerfile with multi-stage build, non-root user), environment-specific config via env vars, rate limiting (100 req/min general, 10 req/min `/validate`), monitoring via Prometheus/Grafana.

**Security Compliance**: ✅ Pre-commit checklist enforced: no production endpoints, no connection strings in source, no JWT keys, documentation uses placeholder domains.

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```
Maliev.CustomerService/
├── Maliev.CustomerService.sln
├── Maliev.CustomerService.Api/
│   ├── Program.cs
│   ├── Controllers/
│   │   ├── CustomerController.cs
│   │   ├── CompanyController.cs
│   │   ├── AddressController.cs
│   │   ├── DocumentController.cs
│   │   ├── NDAController.cs
│   │   ├── UserController.cs
│   │   ├── ValidationController.cs
│   │   └── InternalNoteController.cs
│   ├── Services/
│   │   ├── ICustomerService.cs / CustomerService.cs
│   │   ├── ICompanyService.cs / CompanyService.cs
│   │   ├── IAddressService.cs / AddressService.cs
│   │   ├── IDocumentService.cs / DocumentService.cs
│   │   ├── INDAService.cs / NDAService.cs
│   │   ├── IUserService.cs / UserService.cs
│   │   ├── IInternalNoteService.cs / InternalNoteService.cs
│   │   └── External/
│   │       ├── IUploadServiceClient.cs / UploadServiceClient.cs
│   │       └── ICountryServiceClient.cs / CountryServiceClient.cs
│   ├── Models/
│   │   ├── Customers/
│   │   ├── Companies/
│   │   ├── Addresses/
│   │   ├── Documents/
│   │   ├── NDAs/
│   │   ├── Users/
│   │   ├── InternalNotes/
│   │   └── ErrorResponse.cs
│   ├── Validators/
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Configuration/
│   │   └── ExternalServiceOptions.cs
│   ├── BackgroundServices/
│   │   ├── NDAExpirationBackgroundService.cs
│   │   └── DocumentDeletionRetryBackgroundService.cs
│   └── appsettings.Development.json
├── Maliev.CustomerService.Data/
│   ├── CustomerDbContext.cs
│   ├── Models/
│   │   ├── ApplicationUser.cs
│   │   ├── Customer.cs
│   │   ├── Company.cs
│   │   ├── Address.cs
│   │   ├── DocumentReference.cs
│   │   ├── NDARecord.cs
│   │   ├── InternalNote.cs
│   │   └── AuditLog.cs
│   └── Migrations/
├── Maliev.CustomerService.Tests/
│   ├── (Tests will be added in iteration 2)
├── Dockerfile
├── .dockerignore
├── .gitignore
└── .editorconfig
```

**Structure Decision**: 3-project solution following .NET microservice best practices:
- **Maliev.CustomerService.Api**: WebAPI project with controllers, services (business logic), DTOs, validators, middleware
- **Maliev.CustomerService.Data**: Data access layer with EF Core DbContext, entity models, migrations
- **Maliev.CustomerService.Tests**: xUnit test project (deferred to iteration 2 per Constitution exception)

Clean Architecture pattern: Controllers depend on Services, Services depend on Data (DbContext), no circular dependencies.

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because | Remediation Plan |
|-----------|------------|--------------------------------------|------------------|
| **Test-First Development (Constitution Principle III)** | Rapid MVP delivery required to validate customer segmentation and localization architecture with stakeholders before investing in comprehensive test suite. Feature spec did not explicitly request TDD approach. | Writing tests first would delay MVP feedback by estimated 2-3 weeks for 141 tasks covering contract tests, integration tests, and unit tests targeting 80% coverage. Risk of building wrong features increases with delayed validation. | **Iteration 2 (post-MVP)**: Add comprehensive test suite after stakeholder validation:<br>- Contract tests for all API endpoints (OpenAPI spec validation)<br>- Integration tests for critical user journeys (US1, US2, US6)<br>- Unit tests for services and validators<br>- Target: 80% coverage minimum per Constitution<br>- Estimated: 40-50 additional test tasks<br>**Timeline**: Iteration 2 starts immediately after MVP deployment (week 4) |
