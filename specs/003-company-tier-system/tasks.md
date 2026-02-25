# Implementation Tasks: Company Tier System

**Feature**: Company Tier System | **Branch**: 003-company-tier-system | **Generated**: 2026-02-25

---

## Phase 1: Setup

- [X] T001 Verify existing project structure matches Clean Architecture (Api/Application/Domain/Infrastructure/Tests)
- [X] T002 Migrate from old structure (Api/Data/Tests) to Clean Architecture if needed: Create Maliev.CustomerService.Application and Maliev.CustomerService.Infrastructure projects, reorganize code into proper layers
- [X] T003 Review existing Company entity in Maliev.CustomerService.Domain/Entities/Company.cs and update with tier fields (CurrentYearPurchaseValue, CurrentYearOrderCount, Tier, TierCalculatedAt)

---

## Phase 2: Foundational

- [X] T004 Create CompanyTierSettings entity in Maliev.CustomerService.Domain/Entities/CompanyTierSettings.cs
- [X] T005 Create CompanyDocument entity in Maliev.CustomerService.Domain/Entities/CompanyDocument.cs
- [X] T006 Add tier-related fields to Company entity: CurrentYearPurchaseValue, CurrentYearOrderCount, Tier, TierCalculatedAt in Maliev.CustomerService.Domain/Entities/Company.cs
- [X] T007 Create EF Core configuration for CompanyTierSettings in Maliev.CustomerService.Infrastructure/Data/Configurations/CompanyTierSettingsConfiguration.cs
- [X] T008 Create EF Core configuration for CompanyDocument in Maliev.CustomerService.Infrastructure/Data/Configurations/CompanyDocumentConfiguration.cs
- [X] T009 Update CustomerDbContext to include CompanyTierSettings and CompanyDocuments DbSets in Maliev.CustomerService.Infrastructure/Data/CustomerDbContext.cs
- [X] T010 Create EF Core migration for new entities and fields
- [X] T010b Seed default tier settings (Classic, Silver, Gold) after migration

---

## Phase 3: User Story 1 - Automatic Tier Promotion (P1)

**Goal**: Companies automatically promoted when YTD purchase value and order count meet configurable thresholds

**Independent Test**: Create test orders and verify tier changes occur correctly when thresholds are met

### Implementation

- [X] T011 Create TierCalculationService interface in Maliev.CustomerService.Application/Services/ITierCalculationService.cs
- [X] T011b Create ICompanyRepository interface in Maliev.CustomerService.Application/Interfaces/ICompanyRepository.cs
- [X] T011c Create ICompanyTierSettingsRepository interface in Maliev.CustomerService.Application/Interfaces/ICompanyTierSettingsRepository.cs
- [X] T012 Implement TierCalculationService in Maliev.CustomerService.Application/Services/TierCalculationService.cs
- [X] T013 Add GetTierSettingsAsync method to retrieve current tier configurations
- [X] T014 Add CalculateTierAsync method to determine tier based on YTD values (>= comparison)
- [X] T015 Add ApplyTierAsync method to save tier changes to company
- [X] T016 Add ResetYearlyValuesAsync method to reset YTD values for all companies
- [X] T017 Add ApplyYearEndDemotionsAsync method for demotion logic (Gold→Silver→Classic)

---

## Phase 4: User Story 2 - Tier Benefit Application (P1)

**Goal**: Tier benefits (discounts, free shipping, coin rewards) applied to orders

**Independent Test**: Place orders for companies at different tiers and verify benefits applied

### Implementation

- [X] T018 [P] Add GetCompanyWithTierAsync method to TierCalculationService returning tier info with benefits
- [X] T019 [P] Update CompanyDto to include CurrentYearPurchaseValue, CurrentYearOrderCount, Tier, TierCalculatedAt in Maliev.CustomerService.Application/DTOs/CompanyDto.cs
- [X] T020 Create TierSettings response DTOs in Maliev.CustomerService.Application/DTOs/TierSettingsDto.cs
- [X] T021 Add GetDiscountPercentageAsync to TierCalculationService
- [X] T022 Add GetFreeShippingThresholdAsync to TierCalculationService
- [X] T023 Add GetCoinRewardPercentageAsync to TierCalculationService

---

## Phase 5: User Story 3 - Year-End Tier Demotion (P1)

**Goal**: Companies demoted by one tier at year-end, YTD values reset

**Independent Test**: Simulate year-end conditions and verify demotion logic

### Implementation

- [X] T024 Create YearEndTierJob BackgroundService in Maliev.CustomerService.Application/BackgroundServices/YearEndTierJob.cs
- [X] T025 Configure job to run at UTC midnight on January 1st
- [X] T026 Add logging for all demotions for audit trail
- [X] T027 Implement automatic retry logic with escalation notification

---

## Phase 6: User Story 4 - Admin Manual Tier Recalculation (P2)

**Goal**: Administrators can manually trigger tier recalculation for any company

**Independent Test**: Invoke recalculation endpoint and verify correct tier applied

### Implementation

- [X] T028 Create CustomerPermissions class in Maliev.CustomerService.Domain/Authorization/CustomerPermissions.cs
- [X] T029 Create CustomerPredefinedRoles class in Maliev.CustomerService.Domain/Authorization/CustomerPredefinedRoles.cs
- [X] T030 Add POST /customer/v1/companies/{id}/calculate-tier endpoint in Maliev.CustomerService.Api/Controllers/CompaniesController.cs
- [X] T031 Apply customer.companies.manage permission to calculate-tier endpoint

---

## Phase 7: User Story 5 - Tier Settings Management (P2)

**Goal**: Administrators can configure tier thresholds and benefits via API

**Independent Test**: Update settings and verify new thresholds applied to future calculations

### Implementation

- [X] T032 Add CompanyTierSettingsRepository in Maliev.CustomerService.Infrastructure/Repositories/CompanyTierSettingsRepository.cs
- [X] T033 Add UpdateTierSettingsAsync method with optimistic concurrency (xmin)
- [X] T034 Create TierSettingsController in Maliev.CustomerService.Api/Controllers/TierSettingsController.cs
- [X] T035 Add GET /customer/v1/tier-settings endpoint (customer.tiers.read)
- [X] T036 Add POST /customer/v1/tier-settings endpoint (customer.tiers.manage)
- [X] T037 Add PUT /customer/v1/tier-settings/{id} endpoint with HTTP 409 on conflict (customer.tiers.manage)

---

## Phase 8: User Story 6 - Company Document Management (P3)

**Goal**: Companies can upload and manage supporting documents

**Independent Test**: Upload, list, and delete documents

**Note**: Document files are stored externally in GCS. This service manages document metadata and URLs only.

### Implementation

- [X] T038 Create DocumentService interface in Maliev.CustomerService.Application/Services/IDocumentService.cs
- [X] T039 Implement DocumentService in Maliev.CustomerService.Application/Services/DocumentService.cs
- [X] T040 Create DocumentDto classes in Maliev.CustomerService.Application/DTOs/CompanyDocumentDto.cs
- [X] T041 Create CompanyDocumentsController in Maliev.CustomerService.Api/Controllers/CompanyDocumentsController.cs
- [X] T042 Add GET /customer/v1/companies/{companyId}/documents endpoint (customer.companies.read)
- [X] T043 Add POST /customer/v1/companies/{companyId}/documents endpoint (customer.companies.write)
- [X] T044 Add DELETE /customer/v1/companies/{companyId}/documents/{id} endpoint (customer.companies.write)

---

## Phase 9: User Story 7 - Tier Progress Display (P3)

**Goal**: Intranet displays tier progress with YTD values and next tier requirements

**Independent Test**: View company details and verify progress information accurate

### Implementation

- [X] T045 Update GET /customer/v1/companies/{id} to include tier info in response (customer.companies.read)
- [X] T046 Add next tier requirements to company response (values needed for next tier)

---

## Phase 10: Event Integration

- [X] T047 Create OrderPaidEventConsumer in Maliev.CustomerService.Api/Consumers/OrderPaidEventConsumer.cs
- [X] T048 In consumer: increment CurrentYearOrderCount on OrderPaidEvent
- [X] T049 In consumer: add order total to CurrentYearPurchaseValue
- [X] T050 In consumer: call CalculateTierAsync after order processed
- [X] T051 In consumer: publish CompanyTierChangedEvent if tier changed

---

## Phase 11: Testing

- [ ] T052 Create unit tests for TierCalculationService in Maliev.CustomerService.Tests/Unit/TierCalculationServiceTests.cs
- [ ] T053 Test CalculateTier_ZeroOrders_ReturnsClassic
- [ ] T053b Test CalculateTier_ExactlyAtThreshold_ReturnsPromoted (>= boundary)
- [ ] T054 Test CalculateTier_MeetsSilverThreshold_ReturnsSilver
- [ ] T055 Test CalculateTier_MeetsGoldThreshold_ReturnsGold
- [ ] T056 Test ApplyDemotion_GoldBelowThreshold_ReturnsSilver
- [ ] T057 Test ApplyDemotion_SilverBelowThreshold_ReturnsClassic
- [ ] T058 Test ApplyDemotion_ClassicStaysClassic
- [ ] T059 Create integration tests with Testcontainers.PostgreSql in Maliev.CustomerService.Tests/Integration/TierIntegrationTests.cs
- [ ] T060 Test concurrent tier settings update returns HTTP 409
- [ ] T061 Test OrderPaidEvent consumer triggers tier promotion
- [ ] T061b [US6] Test document upload, list, and delete operations
- [ ] T061c Test IAM permission enforcement on tier endpoints

## Phase 12: Polish & Cross-Cutting

- [X] T062 Register new services in DI container (IServiceCollection)
- [X] T063 Update IAM registration to include new permissions
- [X] T064 Run dotnet format to ensure code style compliance
- [X] T065 Run dotnet build to verify zero warnings
- [X] T066 Run dotnet test to verify all tests pass

---

## Dependencies

```
T001 → (none - setup)
T002 → T001
T003 → T002

T004 → T003 (foundational)
T005 → T003
T006 → T003
T007 → T004
T008 → T005
T009 → T006, T007, T008
T010 → T009
T010b → T010

T011 → T004 (US1 depends on entities)
T011b → T004
T011c → T004
T012 → T011
T013 → T012
T014 → T013
T015 → T014
T016 → T012
T017 → T016

T018 → T15 (US2 depends on US1 core logic)
T019 → T18
T020 → T004
T021 → T020
T022 → T021
T023 → T022

T024 → T017 (US3 depends on demotion method)
T025 → T024
T026 → T025
T027 → T026

T028 → T003 (US4 foundational)
T029 → T028
T030 → T012, T029
T031 → T030

T032 → T004 (US5 depends on settings entity)
T033 → T032
T034 → T033
T035 → T034
T036 → T035
T037 → T036

T038 → T005 (US6 depends on document entity)
T039 → T038
T040 → T039
T041 → T040
T042 → T041
T043 → T042
T044 → T043

T045 → T019 (US7 depends on company DTO)
T046 → T045

T047 → T012 (event depends on service)
T048 → T047
T049 → T048
T050 → T049
T051 → T050

T052 → T012 (tests depend on service implementation)
T053 → T052
T053b → T052
T054 → T052
T055 → T052
T056 → T052
T057 → T052
T058 → T052
T059 → T010 (integration tests need migration)
T060 → T037, T059
T061 → T051, T059
T061b → T044, T059
T061c → T031, T037, T059

T062 → all services
T063 → T028, T029
T064 → T063
T065 → T064
T066 → T065
```

---

## Parallel Execution Examples

**User Story 1 (T011-T017)**: Can be implemented as a single unit since it's the core service

**User Story 2 (T018-T023)**: Can run parallel with US1 after T015 complete:
- T018 [P] can run after T015
- T019 [P] can run after T015
- T020 can run after T004
- T021-T023 depend on T020

**User Story 4 (T028-T031)**: Can run parallel with US1 after T012:
- T028-T029 are foundational (no dependencies)
- T030-T031 depend on T012

**User Story 5 (T032-T037)**: Can run parallel with US1 after T004:
- T032 depends on T004
- T033-T037 sequential within story

**Event Integration (T047-T051)**: Must run after US1 core service complete

---

## Independent Test Criteria

| User Story | Independent Test | Can Implement Alone? |
|------------|------------------|---------------------|
| US1 - Auto Tier Promotion | Create test orders, verify tier changes | YES (MVP) |
| US2 - Tier Benefits | Place orders, verify benefits applied | YES |
| US3 - Year-End Demotion | Simulate year-end, verify demotion | YES |
| US4 - Manual Recalc | Invoke endpoint, verify tier | YES |
| US5 - Settings Management | Update settings, verify persistence | YES |
| US6 - Document Mgmt | Upload/list/delete docs | YES |
| US7 - Progress Display | View company, verify progress | YES |

---

## Suggested MVP Scope

**MVP**: User Stories 1, 2, 3 (Core tier system)

These three P1 stories form a complete, independently testable system:
- US1: Core promotion logic
- US2: Benefit calculation
- US3: Year-end demotion

All three can be implemented and tested together as the MVP. US4-US7 can be delivered in subsequent increments.

---

## Implementation Strategy

1. **MVP First (US1-US3)**: Implement core tier promotion/demotion logic, service layer, and background job
2. **Increment 2 (US4-US5)**: Add admin APIs for manual recalculation and settings management
3. **Increment 3 (US6)**: Add document management functionality
4. **Increment 4 (US7)**: Add progress display to company endpoint
5. **Polish**: Integration tests, format, build verification
