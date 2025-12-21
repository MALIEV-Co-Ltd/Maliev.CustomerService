# Tasks: Principal-First Model Migration

**Input**: Design documents from `/specs/002-principal-model-migration/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Tests are MANDATORY per the Microservices Constitution Rule III.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Add `PrincipalId` property to `Customer` entity in `Maliev.CustomerService.Data/Models/Customer.cs`
- [ ] T002 Create EF Core migration `AddPrincipalIdToCustomers` in `Maliev.CustomerService.Data`
- [ ] T003 [P] Create `CreatePrincipalRequest` model in `Maliev.CustomerService.Api/Models/IAM/CreatePrincipalRequest.cs`
- [ ] T004 [P] Create `CreatePrincipalResponse` model in `Maliev.CustomerService.Api/Models/IAM/CreatePrincipalResponse.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Create `IIAMClient` interface in `Maliev.CustomerService.Api/Services/IIAMClient.cs`
- [ ] T006 Implement `IAMClient` using `HttpClient` in `Maliev.CustomerService.Api/Services/IAMClient.cs`
- [ ] T007 Register `IIAMClient` with resilience and bearer token in `Maliev.CustomerService.Api/Program.cs`
- [ ] T008 [P] Add IAM configuration and feature flags to `Maliev.CustomerService.Api/appsettings.json`
- [ ] T009 Add unit tests for `IAMClient` in `Maliev.CustomerService.Tests/Services/IAMClientTests.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Existing Customer Access (Priority: P1) 🎯 MVP

**Goal**: Migrate existing customers to use central identity (Principals)

**Independent Test**: Verify migration script creates principals in IAM and links them to customer records in the database.

### Tests for User Story 1
- [ ] T010 [US1] Integration test for `MigrateToPrincipalsScript` in `Maliev.CustomerService.Tests/Integration/MigrationScriptTests.cs`

### Implementation for User Story 1
- [ ] T011 [US1] Create `MigrateToPrincipalsScript` class in `Maliev.CustomerService.Api/Scripts/MigrateToPrincipalsScript.cs`
- [ ] T012 [US1] Implement batch processing logic (100 per batch) with error logging in `MigrateToPrincipalsScript.cs`
- [ ] T013 [US1] Add CLI command `--migrate-principals` to `Maliev.CustomerService.Api/Program.cs`
- [ ] T014 [US1] Create migration runbook and SQL verification queries in `Maliev.CustomerService.Api/Scripts/MIGRATION_RUNBOOK.md`

**Checkpoint**: Existing customers can be migrated and verified independently.

---

## Phase 4: User Story 2 - New Customer Registration (Priority: P1)

**Goal**: New customers automatically get central identities upon registration

**Independent Test**: Register a new customer and verify a principal is created in IAM and linked to the record.

### Tests for User Story 2
- [ ] T015 [US2] Unit test for registration with IAM integration in `Maliev.CustomerService.Tests/Services/CustomerServiceTests.cs`
- [ ] T016 [US2] Integration test for end-to-end registration flow in `Maliev.CustomerService.Tests/Integration/US1_CustomerRegistrationIntegrationTests.cs`

### Implementation for User Story 4
- [ ] T017 [US2] Update `CreateAsync` in `Maliev.CustomerService.Api/Services/CustomerService.cs` to call `IIAMClient`
- [ ] T018 [US2] Implement feature flag check and transactional rollback for IAM failure in `CustomerService.cs`
- [ ] T019 [US2] Add logging and metrics for new registration flow in `CustomerService.cs`

**Checkpoint**: New registrations now produce central identities.

---

## Phase 5: User Story 3 - Service-to-Service Customer Lookup (Priority: P2)

**Goal**: Retrieve customer data using their central identity ID

**Independent Test**: Call lookup endpoint with valid/invalid Principal IDs and verify results.

### Tests for User Story 3
- [ ] T020 [US3] Integration tests for `GET /by-principal/{id}` in `Maliev.CustomerService.Tests/Integration/CustomerControllerTests.cs`

### Implementation for User Story 3
- [ ] T021 [US3] Add `GetByPrincipalIdAsync` to `ICustomerService.cs` and `CustomerService.cs`
- [ ] T022 [US3] Implement `GET /by-principal/{principalId}` endpoint in `Maliev.CustomerService.Api/Controllers/CustomerController.cs`
- [ ] T023 [US3] Create database migration for lookup performance index `idx_customers_principal_lookup` in `Maliev.CustomerService.Data`

**Checkpoint**: Downstream services can now look up customers via central identity.

---

## Phase 6: Transition Support & Credential Validation

**Purpose**: Support authentication services during the transition period

- [ ] T024 [P] Update `CredentialValidationResponse` model in `Maliev.CustomerService.Api/Models/Customers/CredentialValidationResponse.cs`
- [ ] T025 Update `ValidateCredentials` endpoint in `Maliev.CustomerService.Api/Controllers/CustomerController.cs` to return `PrincipalId`
- [ ] T026 Add integration test for credential validation returning `PrincipalId` in `Maliev.CustomerService.Tests/Integration/US6_UserAccountManagementIntegrationTests.cs`

---

## Phase 7: Polish & Cleanup (Post-Verification)

**Purpose**: Removal of legacy identity systems and data structures

- [ ] T027 [P] Update `Customer.cs` to make `PrincipalId` NOT NULL and create migration in `Maliev.CustomerService.Data`
- [ ] T028 Add unique constraint index to `PrincipalId` in `Maliev.CustomerService.Data` migration
- [ ] T029 Remove `ApplicationUser.cs` model from `Maliev.CustomerService.Data/Models/`
- [ ] T030 Remove ASP.NET Identity configuration and middleware from `Maliev.CustomerService.Api/Program.cs`
- [ ] T031 Remove Identity-related NuGet packages from `Api` and `Data` project files
- [ ] T032 Create final cleanup migration to DROP all `AspNet*` and `application_users` tables in `Maliev.CustomerService.Data`
- [ ] T033 [P] Update `quickstart.md` and architecture documentation with final state

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - P1 stories (US1, US2) should be completed first.
- **Polish (Phase 7)**: Depends on successful production verification of all user stories.

### Parallel Opportunities

- T003, T004 (Models) can run in parallel.
- T008 (Config) can run in parallel with T006, T007.
- US1, US2, and US3 can proceed in parallel once Phase 2 is complete.
- Cleanup tasks (Phase 7) can be prepared in parallel once verification starts.

---

## Implementation Strategy

### MVP First (User Stories 1 & 2)

1. Complete Setup and Foundational phases.
2. Implement User Story 1 (Migration) to handle existing data.
3. Implement User Story 2 (Registration) to handle new data.
4. **STOP and VALIDATE**: Verify identity consistency between IAM and local DB.

### Incremental Delivery

1. Foundation → Identity infrastructure ready.
2. US1 + US2 → Full identity lifecycle support (Legacy + New).
3. US3 → External lookup support.
4. Cleanup → Legacy debt removal.

---

## Notes

- Use `Testcontainers` for all integration tests (PG, Redis).
- Ensure zero warnings in build before committing.
- No PII in logs during migration.
