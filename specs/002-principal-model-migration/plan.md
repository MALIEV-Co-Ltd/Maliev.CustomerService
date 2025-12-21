# Implementation Plan: Principal-First Model Migration

**Branch**: `002-principal-model-migration` | **Date**: 2025-12-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-principal-model-migration/spec.md`

## Summary

Migrate `CustomerService` from ASP.NET Core Identity-based authentication to a principal-first model where identity is owned by the IAM service. This involves adding a `PrincipalId` to the `Customer` entity, implementing an IAM client for principal creation, backfilling existing data via a migration script, and eventually removing all legacy Identity code and tables.

## Technical Context

**Language/Version**: .NET 10 (C#)  
**Primary Dependencies**: EF Core 10, Npgsql, MassTransit (RabbitMQ), StackExchange.Redis, Scalar (OpenAPI)  
**Storage**: PostgreSQL (Primary), Redis (Caching)  
**Testing**: xUnit, Testcontainers (PostgreSQL, RabbitMQ, Redis)  
**Target Platform**: Linux Container (Docker)  
**Project Type**: Microservice (API + Data)  
**Performance Goals**: Customer creation with IAM integration < 2s; High-performance lookup by PrincipalId (< 10ms p95)  
**Constraints**: Zero downtime migration; No data loss; Constitution compliance (no AutoMapper, no FluentValidation)  
**Scale/Scope**: Migration of existing customers; Support for concurrent registration; Centralized identity management

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Rule | Status | Notes |
|------|--------|-------|
| I. Service Autonomy | ✅ | CustomerService continues to own business data, delegates identity to IAM. |
| III. Test-First | ⚠️ | Plan includes tests, but must ensure they run BEFORE implementation. |
| IV. Real Infrastructure | ✅ | Using Testcontainers for PG/Redis is already standard in this project. |
| X. Docker Best Practices | ✅ | Dockerfile already exists in Api project; uses `app` user. |
| XIV. Code Quality | ✅ | No AutoMapper/FluentValidation found in current dependencies. |
| XV. Project Structure | ✅ | Flat structure: Api, Data, Tests projects at root. |

## Project Structure

### Documentation (this feature)

```text
specs/002-principal-model-migration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── checklists/
    └── requirements.md  # Spec validation
```

### Source Code (repository root)

```text
Maliev.CustomerService.Api/
├── Configuration/       # Options and DI setup
├── Controllers/         # CustomerController (new endpoint)
├── Models/              # IAM Request/Response models
├── Services/            # CustomerService, IAMClient
└── Program.cs           # DI Registration

Maliev.CustomerService.Data/
├── Migrations/          # principal_id migration
├── Models/              # Customer (updated), ApplicationUser (to be removed)
└── CustomerDbContext.cs

Maliev.CustomerService.Tests/
├── Integration/         # Creation and lookup tests
├── Unit/                # IAMClient and Service tests
└── Infrastructure/      # Testcontainers setup
```

**Structure Decision**: Standard flat .NET microservice structure. Projects are named with `Maliev.CustomerService` prefix.

## Complexity Tracking

*No constitution violations requiring justification.*