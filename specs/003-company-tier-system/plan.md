# Implementation Plan: Company Tier System

**Branch**: `003-company-tier-system` | **Date**: 2026-02-25 | **Spec**: specs/003-company-tier-system/spec.md
**Input**: Feature specification from `/specs/003-company-tier-system/spec.md`

## Summary

Automated tier system for company customers based on yearly purchase activity. Companies are promoted through Classic → Silver → Gold tiers based on YTD purchase value and order count. Year-end demotion reduces tier by one level. Tier benefits (discounts, free shipping, coin rewards) are configurable via API. Includes company document management and background job for year-end processing.

## Technical Context

**Language/Version**: C# 14 / .NET 10.0
**Primary Dependencies**: Entity Framework Core 10.0, MassTransit 8.x, xUnit, Moq, Testcontainers.PostgreSql 4.x
**Storage**: PostgreSQL (via EF Core)
**Testing**: xUnit with Testcontainers (real PostgreSQL, real RabbitMQ)
**Target Platform**: Linux container (Docker)
**Project Type**: Web service / REST API
**Performance Goals**:
- Tier promotion within 5 seconds of OrderPaidEvent
- API response time under 500ms for tier settings
- Year-end job completes within 1 hour
**Constraints**:
- Clean Architecture (Api/Application/Domain/Infrastructure/Tests)
- No AutoMapper, FluentValidation, FluentAssertions
- PostgreSQL xmin optimistic concurrency for concurrent updates
- BackgroundService for scheduled jobs (NOT HTTP endpoints)
**Scale/Scope**:
- All registered companies in the system
- YTD values reset annually at UTC midnight

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| Clean Architecture (Api/Application/Domain/Infrastructure/Tests) | ✅ PASS | 5-layer structure per implementation plan |
| No AutoMapper/FluentValidation/FluentAssertions | ✅ PASS | Using explicit mapping, DataAnnotations, xUnit Assert |
| Testcontainers for integration tests | ✅ PASS | Testcontainers.PostgreSql + MassTransit.TestFramework |
| Permissions use plural resource format | ✅ PASS | customer.companies.*, customer.tiers.* |
| Routes prefixed /customer/v1/ | ✅ PASS | Per implementation plan |
| Scalar at /customer/scalar | ✅ PASS | Per implementation plan |
| xmin optimistic concurrency | ✅ PASS | CompanyTierSettings, CompanyDocument |
| BackgroundService for scheduled jobs | ✅ PASS | YearEndTierJob as BackgroundService |
| >= 80% coverage mandate | ✅ PASS | Coverage target in implementation plan |

## Project Structure

### Documentation (this feature)

```
specs/003-company-tier-system/
├── plan.md              # This file
├── research.md          # (not needed - no unknowns)
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (if needed)
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```
Maliev.CustomerService.slnx
├── Maliev.CustomerService.Api/
│   ├── Controllers/
│   │   ├── CompaniesController.cs
│   │   ├── TierSettingsController.cs
│   │   └── CompanyDocumentsController.cs
│   ├── Consumers/
│   │   └── OrderPaidEventConsumer.cs
│   └── Program.cs
├── Maliev.CustomerService.Application/
│   ├── Services/
│   │   ├── TierCalculationService.cs
│   │   └── DocumentService.cs
│   ├── DTOs/
│   │   ├── CompanyDto.cs
│   │   ├── TierSettingsDto.cs
│   │   └── CompanyDocumentDto.cs
│   └── BackgroundServices/
│       └── YearEndTierJob.cs
├── Maliev.CustomerService.Domain/
│   ├── Entities/
│   │   ├── Company.cs (updated)
│   │   ├── CompanyTierSettings.cs (new)
│   │   └── CompanyDocument.cs (new)
│   └── Authorization/
│       ├── CustomerPermissions.cs (new)
│       └── CustomerPredefinedRoles.cs (new)
├── Maliev.CustomerService.Infrastructure/
│   ├── Data/
│   │   ├── CustomerDbContext.cs (updated)
│   │   └── Configurations/
│   │       ├── CompanyTierSettingsConfiguration.cs
│   │       └── CompanyDocumentConfiguration.cs
│   └── Repositories/
│       ├── CompanyRepository.cs
│       └── CompanyTierSettingsRepository.cs
└── Maliev.CustomerService.Tests/
    ├── Unit/
    │   └── TierCalculationServiceTests.cs
    └── Integration/
        └── TierIntegrationTests.cs
```

**Structure Decision**: Clean Architecture with 5 projects (Api, Application, Domain, Infrastructure, Tests) as per MALIEV standards.

## Complexity Tracking

No constitutional violations requiring justification.

---

## Phase 1: Design Artifacts

### Data Model

Per the feature spec and implementation plan, the following entities are required:

**Company Entity (updated)**
- `CurrentYearPurchaseValue` (decimal) - YTD purchase total in THB
- `CurrentYearOrderCount` (int) - YTD order count
- `Tier` (string: "Classic" | "Silver" | "Gold")
- `TierCalculatedAt` (DateTime?)

**CompanyTierSettings Entity (new)**
- `Id` (Guid, PK)
- `TierName` (string)
- `MinPurchaseValue` (decimal)
- `MinOrderCount` (int)
- `DiscountPercentage` (decimal)
- `FreeShippingMinOrder` (decimal?)
- `CoinRewardPercentage` (decimal?)
- `ValidFrom` (DateTime)
- `ValidTo` (DateTime?)
- `xmin` (uint) - PostgreSQL optimistic concurrency

**CompanyDocument Entity (new)**
- `Id` (Guid, PK)
- `CompanyId` (Guid, FK)
- `DocumentType` (string: "TaxCert" | "BusinessLicense" | "Contract" | "Other")
- `FileName` (string)
- `FileUrl` (string) - GCS URL
- `ExpiryDate` (DateTime?)
- `CreatedAt` (DateTime)
- `xmin` (uint) - PostgreSQL optimistic concurrency

### API Contracts

All endpoints under `/customer/v1/`:

| Method | Route | Permission | Description |
|--------|-------|-------------|-------------|
| GET | /customer/v1/companies/{id} | customer.companies.read | Get company with tier info |
| POST | /customer/v1/companies/{id}/calculate-tier | customer.companies.manage | Manual tier recalc |
| GET | /customer/v1/tier-settings | customer.tiers.read | List tier settings |
| POST | /customer/v1/tier-settings | customer.tiers.manage | Create tier setting |
| PUT | /customer/v1/tier-settings/{id} | customer.tiers.manage | Update tier setting |
| GET | /customer/v1/companies/{companyId}/documents | customer.companies.read | List documents |
| POST | /customer/v1/companies/{companyId}/documents | customer.companies.write | Upload document |
| DELETE | /customer/v1/companies/{companyId}/documents/{id} | customer.companies.write | Delete document |

### Background Jobs

**YearEndTierJob** (BackgroundService)
- Runs at UTC midnight on January 1st
- Calls `ResetYearlyValuesAsync()` then `ApplyYearEndDemotionsAsync()`
- Automatic retry with escalation on failure

### Event Integration

**OrderPaidEventConsumer**
- Consumes `OrderPaidEvent` from `Maliev.MessagingContracts`
- Increments `CurrentYearOrderCount`
- Adds order total to `CurrentYearPurchaseValue`
- Calls `CalculateTierAsync()`
- Publishes `CompanyTierChangedEvent` if tier changed

---

## Research/Clarifications

No additional research needed. All technical decisions are provided in the implementation plan input and align with the constitution.

---

## Implementation Phases

### Phase 1: Domain Entities (Tasks T004-T010)
1. Update Company entity with tier fields
2. Create CompanyTierSettings entity
3. Create CompanyDocument entity
4. Update CustomerDbContext
5. Create EF Core migration

### Phase 2: Application Layer (Tasks T011-T017)
1. Create TierCalculationService
2. Create DocumentService

### Phase 3: DTOs (Tasks T018-T023)
1. Update CompanyResponse DTO
2. Create TierSettings DTOs
3. Create Document DTOs

### Phase 4: Permissions (Tasks T028-T031)
1. Define CustomerPermissions
2. Define CustomerPredefinedRoles
3. Register IAM permissions

### Phase 5: API Endpoints (Tasks T034-T044)
1. CompaniesController
2. TierSettingsController
3. CompanyDocumentsController

### Phase 6: Background Jobs (Tasks T024-T027)
1. YearEndTierJob BackgroundService

### Phase 7: Event Integration (Tasks T047-T051)
1. OrderPaidEventConsumer

### Phase 8: Testing (Tasks T052-T061c)
1. Unit tests for TierCalculationService
2. Integration tests with Testcontainers
