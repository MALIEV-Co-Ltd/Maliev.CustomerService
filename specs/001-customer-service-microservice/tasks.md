# Tasks: Customer Service Microservice

**Input**: Design documents from `/specs/001-customer-service-microservice/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: Tests are NOT included (not requested in feature specification)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3...)
- Include exact file paths in descriptions

## Path Conventions
- **Solution**: `Maliev.CustomerService.sln` at repository root
- **API Project**: `Maliev.CustomerService.Api/`
- **Data Project**: `Maliev.CustomerService.Data/`
- **Tests Project**: `Maliev.CustomerService.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create solution structure with three projects: Maliev.CustomerService.Api (.NET 9 WebAPI), Maliev.CustomerService.Data (class library), Maliev.CustomerService.Tests (xUnit)
- [X] T002 [P] Add NuGet packages to Api project: Microsoft.AspNetCore.OpenApi 9.0.0, AspNetCore.HealthChecks.UI.Client 9.0.0, Microsoft.AspNetCore.Authentication.JwtBearer 9.0.8, Serilog.AspNetCore 8.0.2, FluentValidation 11.5.1, Polly 8.0.0
- [X] T003 [P] Add NuGet packages to Data project: Npgsql.EntityFrameworkCore.PostgreSQL 9.0.2, Microsoft.EntityFrameworkCore.Design 9.0.9, Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.8
- [X] T004 [P] Add NuGet packages to Tests project: xUnit 2.4.2, FluentAssertions 8.6.0, Moq 4.20.72, Microsoft.AspNetCore.Mvc.Testing 9.0.0
- [X] T005 [P] Configure Serilog console logging in Maliev.CustomerService.Api/Program.cs with JSON structured output
- [X] T006 [P] Configure linting and formatting (.editorconfig) for snake_case database naming and C# coding standards

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 Scaffold ASP.NET Core Identity in Maliev.CustomerService.Api using dotnet aspnet-codegenerator identity --dbContext CustomerDbContext
- [X] T008 Create ApplicationUser class extending IdentityUser in Maliev.CustomerService.Data/Models/ApplicationUser.cs with custom fields: linked_customer_id (Guid?), is_active (bool), last_login_at (DateTime?)
- [X] T009 Create CustomerDbContext in Maliev.CustomerService.Data/CustomerDbContext.cs inheriting from IdentityDbContext<ApplicationUser> with DbSets for all entities
- [X] T010 Configure EF Core snake_case naming convention in CustomerDbContext using ModelConfigurationBuilder
- [X] T011 Create initial Identity migration in Maliev.CustomerService.Data/Migrations/ using dotnet ef migrations add InitialIdentity
- [X] T012 Create ExternalServiceOptions classes in Maliev.CustomerService.Api/Configuration/ for UploadService, CountryService base URLs and timeouts
- [X] T013 [P] Configure JWT Bearer authentication in Maliev.CustomerService.Api/Program.cs with issuer validation, audience validation, and role claims
- [X] T014 [P] Configure rate limiting in Maliev.CustomerService.Api/Program.cs: fixed window 100 req/min general, sliding window 10 req/min for /validate
- [X] T015 [P] Create ErrorResponse DTO in Maliev.CustomerService.Api/Models/ErrorResponse.cs with code, message, details, traceId, timestamp
- [X] T016 [P] Implement ExceptionHandlingMiddleware in Maliev.CustomerService.Api/Middleware/ExceptionHandlingMiddleware.cs for global error handling with structured logging
- [X] T017 [P] Configure health checks in Maliev.CustomerService.Api/Program.cs: /customers/liveness (simple), /customers/readiness (database + external services)
- [X] T018 [P] Configure Swagger/OpenAPI in Maliev.CustomerService.Api/Program.cs with JWT bearer configuration, route prefix /customers/swagger
- [X] T019 Create appsettings.Development.json with placeholder connection string (`${ConnectionStrings__CustomerDbContext}`) and development JWT settings - actual secrets loaded from environment variables per Constitution VI
- [X] T020 Create Dockerfile with multi-stage build: SDK 9.0 for build, ASP.NET 9.0 runtime, non-root user (appuser UID 1000)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Customer Registration and Basic Information Management (Priority: P1) 🎯 MVP

**Goal**: Enable sales representatives to create, retrieve, update, and soft-delete customer records with personal details, segmentation (segment/tier), localization preferences (language/timezone), and communication preferences

**Independent Test**: Create a customer with segment="Retail", tier="Bronze", preferredLanguage="en", timezone="Asia/Bangkok" via POST /customers, retrieve via GET /customers/{id}, update fields via PATCH, soft-delete via DELETE

### Implementation for User Story 1

- [X] T021 [P] [US1] Create Customer entity in Maliev.CustomerService.Data/Models/Customer.cs with fields: id, first_name, last_name, email, phone, segment (enum), tier (enum), preferred_language, timezone, communication_preferences (JSONB), company_id, is_deleted, created_at, updated_at, version (RowVersion)
- [X] T022 [P] [US1] Create AuditLog entity in Maliev.CustomerService.Data/Models/AuditLog.cs with fields: id, actor_id, actor_type (Customer/Employee/System), action, entity_type, entity_id, timestamp, changed_fields (JSON), previous_values (JSON)
- [X] T023 [US1] Configure Customer entity in CustomerDbContext.OnModelCreating: snake_case columns, CHECK constraints for segment/tier enums, unique index on email WHERE is_deleted=false, indexes on company_id, created_at, segment, tier, preferred_language
- [X] T024 [US1] Configure AuditLog entity in CustomerDbContext.OnModelCreating: indexes on (entity_type, entity_id), actor_id, timestamp, actor_type
- [X] T025 [US1] Create migration for Customer and AuditLog tables: dotnet ef migrations add AddCustomerAndAuditLog
- [X] T026 [P] [US1] Create CreateCustomerRequest DTO in Maliev.CustomerService.Api/Models/Customers/CreateCustomerRequest.cs with validation attributes matching OpenAPI spec
- [X] T027 [P] [US1] Create UpdateCustomerRequest DTO in Maliev.CustomerService.Api/Models/Customers/UpdateCustomerRequest.cs with version field for optimistic concurrency
- [X] T028 [P] [US1] Create CustomerResponse DTO in Maliev.CustomerService.Api/Models/Customers/CustomerResponse.cs with all customer fields including segment, tier, localization preferences
- [X] T029 [US1] Create CreateCustomerRequestValidator in Maliev.CustomerService.Api/Validators/CreateCustomerRequestValidator.cs using FluentValidation: email RFC 5322, phone E.164, segment/tier enum validation, preferred_language ISO 639-1 regex, timezone IANA validation
- [X] T030 [US1] Create UpdateCustomerRequestValidator in Maliev.CustomerService.Api/Validators/UpdateCustomerRequestValidator.cs with same validation rules plus version requirement
- [X] T031 [US1] Create ICustomerService interface in Maliev.CustomerService.Api/Services/ICustomerService.cs with methods: CreateAsync, GetByIdAsync, UpdateAsync, SoftDeleteAsync
- [X] T032 [US1] Implement CustomerService in Maliev.CustomerService.Api/Services/CustomerService.cs with CustomerDbContext injection, audit logging for all operations (recording actorType from JWT claims), optimistic concurrency handling for updates
- [X] T033 [US1] Create CustomerController in Maliev.CustomerService.Api/Controllers/CustomerController.cs with POST /customers/v1/customers, GET /customers/v1/customers/{id}, PATCH /customers/v1/customers/{id}, DELETE /customers/v1/customers/{id} (soft delete)
- [X] T034 [US1] Add authorization policies to CustomerController: [Authorize] on all endpoints, distinguish Employee vs Customer actorType from JWT claims for audit logging
- [X] T035 [US1] Add error handling in CustomerController: 400 for validation errors, 404 for not found, 409 for duplicate email or version conflicts, 422 for domain validation failures

**Checkpoint**: At this point, User Story 1 should be fully functional - customers can be created, retrieved, updated (with segmentation and localization), and soft-deleted with complete audit trails

---

## Phase 4: User Story 2 - Multi-Address Management for Billing and Shipping (Priority: P1)

**Goal**: Enable customer service representatives to manage multiple billing and shipping addresses for customers and companies with Country Service validation

**Independent Test**: Create a customer, add billing address with valid countryId (validated via Country Service), add second shipping address, retrieve all addresses, update postal code, delete address

### Implementation for User Story 2

- [ ] T036 [P] [US2] Create Address entity in Maliev.CustomerService.Data/Models/Address.cs with fields: id, owner_type (Customer/Company enum), owner_id, type (Billing/Shipping enum), address_line1, address_line2, city, province, postal_code, country_id, created_at, updated_at, version
- [ ] T037 [US2] Configure Address entity in CustomerDbContext.OnModelCreating: CHECK constraints for owner_type and type enums, composite index on (owner_type, owner_id), index on country_id
- [ ] T038 [US2] Create migration for Address table: dotnet ef migrations add AddAddressEntity
- [ ] T039 [P] [US2] Create CreateAddressRequest DTO in Maliev.CustomerService.Api/Models/Addresses/CreateAddressRequest.cs with owner polymorphism
- [ ] T040 [P] [US2] Create UpdateAddressRequest DTO in Maliev.CustomerService.Api/Models/Addresses/UpdateAddressRequest.cs with version field
- [ ] T041 [P] [US2] Create AddressResponse DTO in Maliev.CustomerService.Api/Models/Addresses/AddressResponse.cs
- [ ] T042 [US2] Create AddressRequestValidator in Maliev.CustomerService.Api/Validators/AddressRequestValidator.cs: required fields, owner_type/type enum validation
- [ ] T043 [US2] Create ICountryServiceClient interface in Maliev.CustomerService.Api/Services/External/ICountryServiceClient.cs with ValidateCountryIdAsync method
- [ ] T044 [US2] Implement CountryServiceClient in Maliev.CustomerService.Api/Services/External/CountryServiceClient.cs with typed HttpClient, Polly retry policy (3 attempts, exponential backoff), 24-hour cache for valid country IDs
- [ ] T045 [US2] Register CountryServiceClient in Program.cs with HttpClient factory and Polly policies
- [ ] T046 [US2] Create IAddressService interface in Maliev.CustomerService.Api/Services/IAddressService.cs with methods: CreateAsync, GetByOwnerAsync, UpdateAsync, DeleteAsync
- [ ] T047 [US2] Implement AddressService in Maliev.CustomerService.Api/Services/AddressService.cs with Country Service validation before create/update, audit logging, optimistic concurrency
- [ ] T048 [US2] Create AddressController in Maliev.CustomerService.Api/Controllers/AddressController.cs with POST /customers/v1/addresses, GET /customers/v1/addresses?ownerType={type}&ownerId={id}, PATCH /customers/v1/addresses/{id}, DELETE /customers/v1/addresses/{id}
- [ ] T049 [US2] Add error handling in AddressController: 503 for Country Service unavailability, 400 for invalid countryId

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - addresses can be managed for customers with external service validation

---

## Phase 5: User Story 6 - User Account Management and Credential Validation (Priority: P1)

**Goal**: Enable user account creation for customers, password management, role updates, and provide /validate endpoint for Auth Service credential verification with last_login_at tracking

**Independent Test**: Create user account with username/password/email, call POST /customers/v1/validate with valid credentials (verify isValid=true, last_login_at updated), call with invalid credentials (verify isValid=false, generic error), verify rate limiting after 10 attempts

### Implementation for User Story 6

- [ ] T050 [US6] Create migration to add last_login_at index on AspNetUsers: dotnet ef migrations add AddLastLoginAtIndex
- [ ] T051 [P] [US6] Create CreateUserRequest DTO in Maliev.CustomerService.Api/Models/Users/CreateUserRequest.cs with username, email, password, roles, linkedCustomerId
- [ ] T052 [P] [US6] Create UpdatePasswordRequest DTO in Maliev.CustomerService.Api/Models/Users/UpdatePasswordRequest.cs
- [ ] T053 [P] [US6] Create UpdateRolesRequest DTO in Maliev.CustomerService.Api/Models/Users/UpdateRolesRequest.cs
- [ ] T054 [P] [US6] Create UserResponse DTO in Maliev.CustomerService.Api/Models/Users/UserResponse.cs with id, username, email, roles, linkedCustomerId, isActive, lastLoginAt, createdAt, updatedAt
- [ ] T055 [P] [US6] Create ValidateCredentialsRequest DTO in Maliev.CustomerService.Api/Models/Users/ValidateCredentialsRequest.cs
- [ ] T056 [P] [US6] Create ValidationResponse DTO in Maliev.CustomerService.Api/Models/Users/ValidationResponse.cs with isValid, userId, username, roles
- [ ] T057 [US6] Create UserRequestValidator in Maliev.CustomerService.Api/Validators/UserRequestValidator.cs: email format, password requirements (min 8 chars, digit, uppercase, lowercase, non-alphanumeric), username format
- [ ] T058 [US6] Create IUserService interface in Maliev.CustomerService.Api/Services/IUserService.cs with methods: CreateAsync, UpdatePasswordAsync, UpdateRolesAsync, ValidateCredentialsAsync
- [ ] T059 [US6] Implement UserService in Maliev.CustomerService.Api/Services/UserService.cs with UserManager<ApplicationUser> and SignInManager<ApplicationUser> injection, ValidateCredentialsAsync updates last_login_at on successful validation, audit logging for all operations
- [ ] T060 [US6] Create UserController in Maliev.CustomerService.Api/Controllers/UserController.cs with POST /customers/v1/users, PUT /customers/v1/users/{id}/password, PUT /customers/v1/users/{id}/roles
- [ ] T061 [US6] Create ValidationController in Maliev.CustomerService.Api/Controllers/ValidationController.cs with POST /customers/v1/validate [AllowAnonymous] endpoint, rate limited to 10 req/min per IP
- [ ] T062 [US6] Add security logging in ValidationController for all validation attempts (success/failure) without logging passwords, include source IP and timestamp
- [ ] T063 [US6] Configure Identity password policy in Program.cs: RequireDigit=true, RequireUppercase=true, RequireLowercase=true, RequireNonAlphanumeric=true, RequiredLength=8, lockout after 5 failures for 15 minutes
- [ ] T064 [US6] Implement GET /customers/v1/users list endpoint in UserController with pagination (page, pageSize) and filtering by last_login_at date range for inactive account detection (FR-126, FR-127)

**Checkpoint**: At this point, User Stories 1, 2, AND 6 should all work independently - user accounts can be managed, queried for inactivity, and Auth Service can validate credentials with activity tracking

---

## Phase 6: User Story 3 - Company Master Data Management (Priority: P2)

**Goal**: Enable business development managers to register and manage company information with VAT validation, segmentation (segment/tier), and link customers to companies

**Independent Test**: Create company with name, VAT number "TH-1234567890", segment="Enterprise", tier="Premium", retrieve company with associated customers list, update contact info, verify VAT format validation

### Implementation for User Story 3

- [ ] T065 [P] [US3] Create Company entity in Maliev.CustomerService.Data/Models/Company.cs with fields: id, name, vat_number, registration_number, contact_email, contact_phone, segment (enum), tier (enum), created_at, updated_at, version
- [ ] T066 [US3] Configure Company entity in CustomerDbContext.OnModelCreating: CHECK constraints for segment/tier enums, indexes on vat_number, name, segment, tier
- [ ] T067 [US3] Create migration for Company table: dotnet ef migrations add AddCompanyEntity
- [ ] T068 [P] [US3] Create CreateCompanyRequest DTO in Maliev.CustomerService.Api/Models/Companies/CreateCompanyRequest.cs
- [ ] T069 [P] [US3] Create UpdateCompanyRequest DTO in Maliev.CustomerService.Api/Models/Companies/UpdateCompanyRequest.cs with version field
- [ ] T070 [P] [US3] Create CompanyResponse DTO in Maliev.CustomerService.Api/Models/Companies/CompanyResponse.cs with segment and tier fields
- [ ] T071 [US3] Create CompanyRequestValidator in Maliev.CustomerService.Api/Validators/CompanyRequestValidator.cs: VAT number format validation (country prefix pattern ^[A-Z]{2}-\d{10,15}$), email RFC 5322, phone E.164, segment/tier enum validation
- [ ] T072 [US3] Create ICompanyService interface in Maliev.CustomerService.Api/Services/ICompanyService.cs with methods: CreateAsync, GetByIdAsync, UpdateAsync, GetWithCustomersAsync, GetAllAsync (for list endpoint)
- [ ] T073 [US3] Implement CompanyService in Maliev.CustomerService.Api/Services/CompanyService.cs with VAT format validation, audit logging, optimistic concurrency, GetWithCustomersAsync loads related customers, GetAllAsync supports pagination and filtering
- [ ] T074 [US3] Create CompanyController in Maliev.CustomerService.Api/Controllers/CompanyController.cs with POST /customers/v1/companies, GET /customers/v1/companies/{id}, GET /customers/v1/companies (list), PATCH /customers/v1/companies/{id}, GET /customers/v1/companies/{id}/customers
- [ ] T075 [US3] Implement pagination and filtering in GET /customers/v1/companies list endpoint: support segment, tier filters and page/pageSize parameters (FR-116, FR-117)
- [ ] T076 [US3] Add error handling in CompanyController: 422 for invalid VAT format

**Checkpoint**: At this point, User Stories 1, 2, 3, AND 6 should all work independently - companies can be created with segmentation, queried by segment/tier, and customers can reference them

---

## Phase 7: User Story 4 - NDA Lifecycle Management (Priority: P2)

**Goal**: Enable legal compliance officers to track NDA lifecycle (Draft → Signed → Expired/Revoked) with document references and automatic expiration via background job

**Independent Test**: Create customer, create NDA in Draft status, attempt to mark Signed without document (verify validation error), link document reference, mark Signed with signedBy/signedAt, revoke NDA, verify automatic expiration via background job

### Implementation for User Story 4

- [ ] T077 [P] [US4] Create NDARecord entity in Maliev.CustomerService.Data/Models/NDARecord.cs with fields: id, customer_id, document_reference_id, status (Draft/Signed/Expired/Revoked enum), signed_by, signed_at, revoked_at, expires_at, created_at, updated_at, version
- [ ] T078 [US4] Configure NDARecord entity in CustomerDbContext.OnModelCreating: CHECK constraint for status enum, indexes on customer_id, status, expires_at
- [ ] T079 [US4] Create migration for NDARecord table: dotnet ef migrations add AddNDARecordEntity
- [ ] T080 [P] [US4] Create CreateNDARequest DTO in Maliev.CustomerService.Api/Models/NDAs/CreateNDARequest.cs
- [ ] T081 [P] [US4] Create UpdateNDAStatusRequest DTO in Maliev.CustomerService.Api/Models/NDAs/UpdateNDAStatusRequest.cs with version, status, signedBy, signedAt, revokedAt
- [ ] T082 [P] [US4] Create NDAResponse DTO in Maliev.CustomerService.Api/Models/NDAs/NDAResponse.cs
- [ ] T083 [US4] Create NDARequestValidator in Maliev.CustomerService.Api/Validators/NDARequestValidator.cs: lifecycle transition validation (Draft→Signed requires document_reference_id, Signed→Revoke requires revokedAt, terminal states Expired/Revoked cannot transition), expires_at must be future date
- [ ] T084 [US4] Create INDAService interface in Maliev.CustomerService.Api/Services/INDAService.cs with methods: CreateAsync, GetByIdAsync, UpdateStatusAsync, CheckExpiredNDAsAsync (for background job)
- [ ] T085 [US4] Implement NDAService in Maliev.CustomerService.Api/Services/NDAService.cs with lifecycle validation, audit logging, CheckExpiredNDAsAsync transitions NDAs with expires_at < NOW() to Expired status
- [ ] T086 [US4] Create NDAController in Maliev.CustomerService.Api/Controllers/NDAController.cs with POST /customers/v1/ndas, GET /customers/v1/ndas/{id}, PATCH /customers/v1/ndas/{id}/status
- [ ] T087 [US4] Create NDAExpirationBackgroundService in Maliev.CustomerService.Api/BackgroundServices/NDAExpirationBackgroundService.cs inheriting from BackgroundService, runs daily to call CheckExpiredNDAsAsync
- [ ] T088 [US4] Register NDAExpirationBackgroundService as hosted service in Program.cs
- [ ] T089 [US4] Add error handling in NDAController: 422 for invalid lifecycle transitions

**Checkpoint**: At this point, User Stories 1, 2, 3, 4, AND 6 should all work independently - NDAs can track full lifecycle with automatic expiration

---

## Phase 8: User Story 5 - Document Metadata Management with Upload Service Integration (Priority: P2)

**Goal**: Enable document controllers to register document metadata with Upload Service file references, manage versions, and handle deferred deletion when Upload Service is unavailable

**Independent Test**: Create customer, register document with valid Upload Service file reference (verify validation call), mark document complete, update with new file reference (verify version increment), delete document (verify Upload Service deletion call), test deferred deletion when Upload Service unavailable

### Implementation for User Story 5

- [ ] T090 [P] [US5] Create DocumentReference entity in Maliev.CustomerService.Data/Models/DocumentReference.cs with fields: id, owner_type, owner_id, document_type, file_reference, filename, status (Pending/Complete/PendingDeletion/Orphaned/MissingFile enum), version, signed_by, signed_at, created_at, updated_at, row_version (RowVersion)
- [ ] T091 [US5] Configure DocumentReference entity in CustomerDbContext.OnModelCreating: CHECK constraints for owner_type and status enums, composite index on (owner_type, owner_id), indexes on document_type, status
- [ ] T092 [US5] Create migration for DocumentReference table: dotnet ef migrations add AddDocumentReferenceEntity
- [ ] T093 [P] [US5] Create CreateDocumentRequest DTO in Maliev.CustomerService.Api/Models/Documents/CreateDocumentRequest.cs
- [ ] T094 [P] [US5] Create UpdateDocumentRequest DTO in Maliev.CustomerService.Api/Models/Documents/UpdateDocumentRequest.cs with new fileReference for versioning
- [ ] T095 [P] [US5] Create DocumentResponse DTO in Maliev.CustomerService.Api/Models/Documents/DocumentResponse.cs
- [ ] T096 [US5] Create DocumentRequestValidator in Maliev.CustomerService.Api/Validators/DocumentRequestValidator.cs: required fields, owner_type/status enum validation
- [ ] T097 [US5] Create IUploadServiceClient interface in Maliev.CustomerService.Api/Services/External/IUploadServiceClient.cs with methods: ValidateFileReferenceAsync, DeleteFileAsync
- [ ] T098 [US5] Implement UploadServiceClient in Maliev.CustomerService.Api/Services/External/UploadServiceClient.cs with typed HttpClient, Polly retry policy, timeout 300s for uploads
- [ ] T099 [US5] Register UploadServiceClient in Program.cs with HttpClient factory and Polly policies
- [ ] T100 [US5] Create IDocumentService interface in Maliev.CustomerService.Api/Services/IDocumentService.cs with methods: CreateAsync, GetByOwnerAsync, UpdateAsync, MarkCompleteAsync, DeleteAsync, RetryPendingDeletionsAsync (for background job)
- [ ] T101 [US5] Implement DocumentService in Maliev.CustomerService.Api/Services/DocumentService.cs with Upload Service validation before create, version increment on update (preserving previous version in audit log), DeleteAsync calls Upload Service (on failure marks PendingDeletion), RetryPendingDeletionsAsync for background job
- [ ] T102 [US5] Create DocumentController in Maliev.CustomerService.Api/Controllers/DocumentController.cs with POST /customers/v1/documents, GET /customers/v1/documents?ownerType={type}&ownerId={id}, PATCH /customers/v1/documents/{id}, PATCH /customers/v1/documents/{id}/complete, DELETE /customers/v1/documents/{id}
- [ ] T103 [US5] Create DocumentDeletionRetryBackgroundService in Maliev.CustomerService.Api/BackgroundServices/DocumentDeletionRetryBackgroundService.cs inheriting from BackgroundService, runs daily to call RetryPendingDeletionsAsync
- [ ] T104 [US5] Register DocumentDeletionRetryBackgroundService as hosted service in Program.cs
- [ ] T105 [US5] Add error handling in DocumentController: 503 for Upload Service unavailability, 400 for invalid file reference

**Checkpoint**: At this point, User Stories 1, 2, 3, 4, 5, AND 6 should all work independently - documents can be managed with external file storage integration and resilient deletion

---

## Phase 9: User Story 7 - Internal Notes Management for Employee Use (Priority: P2)

**Goal**: Enable sales representatives and account managers to create internal notes for customers and companies that are visible only to employees, never to customers via self-service endpoints

**Independent Test**: Create customer, employee creates internal note "Customer prefers morning calls", employee retrieves customer details (verify note included), customer retrieves own profile (verify note NOT included), test HTTP 403 when customer attempts to access notes endpoint

### Implementation for User Story 7

- [ ] T106 [P] [US7] Create InternalNote entity in Maliev.CustomerService.Data/Models/InternalNote.cs with fields: id, owner_type (Customer/Company enum), owner_id, note_text, created_by, created_at, updated_at, version
- [ ] T107 [US7] Configure InternalNote entity in CustomerDbContext.OnModelCreating: CHECK constraint for owner_type, composite index on (owner_type, owner_id), indexes on created_at, created_by
- [ ] T108 [US7] Create migration for InternalNote table: dotnet ef migrations add AddInternalNoteEntity
- [ ] T109 [P] [US7] Create CreateInternalNoteRequest DTO in Maliev.CustomerService.Api/Models/InternalNotes/CreateInternalNoteRequest.cs
- [ ] T110 [P] [US7] Create UpdateInternalNoteRequest DTO in Maliev.CustomerService.Api/Models/InternalNotes/UpdateInternalNoteRequest.cs with version field
- [ ] T111 [P] [US7] Create InternalNoteResponse DTO in Maliev.CustomerService.Api/Models/InternalNotes/InternalNoteResponse.cs
- [ ] T112 [US7] Create InternalNoteRequestValidator in Maliev.CustomerService.Api/Validators/InternalNoteRequestValidator.cs: note_text required, max 5000 chars, owner_type enum validation
- [ ] T113 [US7] Create authorization policy EmployeeOrHigher in Program.cs requiring role Employee, Manager, or Admin
- [ ] T114 [US7] Create IInternalNoteService interface in Maliev.CustomerService.Api/Services/IInternalNoteService.cs with methods: CreateAsync, GetByOwnerAsync, UpdateAsync, DeleteAsync
- [ ] T115 [US7] Implement InternalNoteService in Maliev.CustomerService.Api/Services/InternalNoteService.cs with created_by from JWT claims, audit logging
- [ ] T116 [US7] Create InternalNoteController in Maliev.CustomerService.Api/Controllers/InternalNoteController.cs with POST /customers/v1/internal-notes, GET /customers/v1/internal-notes?ownerType={type}&ownerId={id}, PATCH /customers/v1/internal-notes/{id}, DELETE /customers/v1/internal-notes/{id}, all endpoints protected with [Authorize(Policy = "EmployeeOrHigher")]
- [ ] T117 [US7] Update CustomerService.GetByIdAsync to exclude internal notes when actorType from JWT claims is "Customer" (customer self-service), include internal notes when actorType is "Employee"
- [ ] T118 [US7] Add error handling in InternalNoteController: 403 for customer users attempting access

**Checkpoint**: At this point, User Stories 1, 2, 3, 4, 5, 6, AND 7 should all work independently - internal notes provide employee-only customer context

---

## Phase 10: User Story 9 - Customer Segmentation and Communication Preferences Management (Priority: P2)

**Goal**: Enable filtering and querying customers by segment/tier for downstream services (Marketing, Quoting), support localization preferences updates, and enable communication consent tracking for compliance

**Independent Test**: Create customers with different segments (Retail, Enterprise) and tiers (Bronze, VIP), query customers filtered by tier="VIP" for marketing campaign, update customer's communication_preferences with email_opt_in=true/sms_opt_in=false, query all customers' communication preferences for compliance report

### Implementation for User Story 9

- [ ] T119 [US9] Update CustomerService.GetAllAsync (list endpoint) in Maliev.CustomerService.Api/Services/CustomerService.cs to accept optional query parameters: segment, tier, preferredLanguage, lastLoginAtFrom, lastLoginAtTo
- [ ] T120 [US9] Update CustomerController.GetAll in Maliev.CustomerService.Api/Controllers/CustomerController.cs to add GET /customers/v1/customers?segment={segment}&tier={tier}&preferredLanguage={lang}&page={page}&pageSize={size} with pagination metadata (totalCount, page, pageSize, totalPages)
- [ ] T121 [US9] Create query indexes in CustomerDbContext if not already present: composite indexes on (segment, tier), (preferred_language), (last_login_at) for efficient filtering
- [ ] T122 [US9] Create GetCustomerPreferencesResponse DTO in Maliev.CustomerService.Api/Models/Customers/GetCustomerPreferencesResponse.cs with id, email, segment, tier, preferredLanguage, timezone, communicationPreferences for compliance reporting
- [ ] T123 [US9] Add GET /customers/v1/customers/preferences endpoint in CustomerController for bulk export of customer preferences (paginated) for compliance/audit purposes
- [ ] T124 [US9] Update CustomerService to validate preferred_language against ISO 639-1 standard (regex ^[a-z]{2}$) and timezone against IANA timezone database using TimeZoneInfo or NodaTime library
- [ ] T125 [US9] Add integration logging in CustomerService for segment/tier queries to track downstream service usage (Marketing Service querying tier="VIP", Quoting Service querying segment="Enterprise")

**Checkpoint**: At this point, User Stories 1, 2, 3, 4, 5, 6, 7, AND 9 should all work independently - segmentation enables targeted business rules and compliance reporting

---

## Phase 11: User Story 8 - Query and Filtering Capabilities (Priority: P3)

**Goal**: Enable sales managers to search customers by email, company affiliation, creation date range with pagination, supporting reporting and data analysis workflows

**Independent Test**: Create 30 customers with different emails, companies, creation dates; query with email filter "@example.com" (verify partial match), filter by companyId (verify correct subset), filter by creation date range (verify date boundaries), test pagination with pageSize=10 across 3 pages

### Implementation for User Story 8

- [ ] T126 [US8] Enhance CustomerService.GetAllAsync in Maliev.CustomerService.Api/Services/CustomerService.cs to accept additional query parameters: email (partial match), companyId, createdFrom, createdTo, isDeleted (default false)
- [ ] T127 [US8] Update CustomerController.GetAll in Maliev.CustomerService.Api/Controllers/CustomerController.cs to support GET /customers/v1/customers?email={email}&companyId={id}&createdFrom={date}&createdTo={date}&includeDeleted={bool}&page={page}&pageSize={size}
- [ ] T128 [US8] Implement efficient LINQ queries in CustomerService with proper indexing: WHERE email LIKE '%{email}%' uses index, WHERE created_at BETWEEN uses index, default ORDER BY created_at DESC
- [ ] T129 [US8] Add pagination helper method in CustomerService to calculate totalCount, totalPages, return PaginatedResponse<CustomerResponse> with metadata
- [ ] T130 [US8] Create query indexes if not already present: index on email (for LIKE queries), composite index on (is_deleted, created_at) for filtered sorting

**Checkpoint**: All user stories (1-9) should now be independently functional - comprehensive query capabilities support business operations and reporting

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T131 [P] Create GitHub Actions CI/CD workflows in .github/workflows/: ci-develop.yml, ci-staging.yml, ci-main.yml with .NET build, test, Docker build/push to GCP Artifact Registry, Kustomize image update in maliev-gitops
- [ ] T132 [P] Create Kubernetes manifests in maliev-gitops/3-apps/customer-service/: base/deployment.yaml (with envFrom secretRef for DB connection and JWT secrets), base/service.yaml, base/kustomization.yaml, overlays for development/staging/production
- [ ] T133 [P] Create ServiceMonitor in maliev-gitops for Prometheus metrics collection from /metrics endpoint
- [ ] T134 [P] Update README.md with service overview, architecture diagram, API endpoints summary, local development quickstart, deployment instructions
- [ ] T135 [P] Validate all endpoints match contracts/openapi.yaml specification exactly (request/response schemas, HTTP status codes, error formats)
- [ ] T136 Code review: verify all services use async/await consistently, check optimistic concurrency on all update operations, validate audit logging on all mutations
- [ ] T137 Performance validation: verify p95 latency < 150ms for GET endpoints, < 200ms for POST/PATCH endpoints under normal load (100 concurrent users)
- [ ] T138 Security hardening: verify no secrets in appsettings.json (all from Secret Manager), verify rate limiting configured correctly, verify JWT validation includes issuer/audience checks
- [ ] T139 [P] Run full quickstart.md walkthrough: setup local PostgreSQL, apply all migrations, verify all curl examples work, verify Swagger UI functional
- [ ] T140 Create database migration rollback plan documentation for production deployments
- [ ] T141 [P] Setup Grafana dashboards for customer service metrics: request rate, error rate, latency percentiles, database connection pool, external service health

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-11)**: All depend on Foundational phase completion
  - P1 stories (US1, US2, US6): Can proceed in parallel after Foundational
  - P2 stories (US3, US4, US5, US7, US9): Can start in parallel after Foundational
  - P3 story (US8): Can start after Foundational
  - Stories can work in parallel if team has capacity
- **Polish (Phase 12)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies on other stories - completely independent (requires Customer entity, audit logging)
- **User Story 2 (P1)**: No dependencies on other stories - completely independent (requires Address entity, Country Service integration)
- **User Story 6 (P1)**: No dependencies on other stories - completely independent (requires ApplicationUser/Identity, validation endpoint)
- **User Story 3 (P2)**: No dependencies on other stories - completely independent (requires Company entity, customers can optionally reference companies from US1)
- **User Story 4 (P2)**: Loosely depends on US5 for document references, but can test with mock document references - nearly independent
- **User Story 5 (P2)**: No dependencies on other stories - completely independent (requires DocumentReference entity, Upload Service integration)
- **User Story 7 (P2)**: Depends on US1 for Customer entity existing, but can test independently - minimal coupling
- **User Story 9 (P2)**: Extends US1 with filtering/querying - requires US1 implementation to be complete
- **User Story 8 (P3)**: Extends US1/US3 with advanced queries - requires US1 implementation to be complete

### Within Each User Story

- Models before services (entities must exist before business logic)
- Services before controllers (business logic before API endpoints)
- Core implementation before integration tests
- Validators alongside DTOs
- Background services after core service implementation

### Parallel Opportunities

- **Phase 1 (Setup)**: T002, T003, T004, T005, T006 can all run in parallel (different files, independent)
- **Phase 2 (Foundational)**: T013, T014, T015, T016, T017, T018 can run in parallel after T007-T011 complete
- **User Stories**: After Foundational completes, US1, US2, US6 (all P1) can be developed in parallel by different team members
- **Within User Story 1**: T021, T022, T026, T027, T028 all marked [P] - different files, can run together
- **Within User Story 2**: T036 (Address entity) can run in parallel with T039, T040, T041 (DTOs)
- **Polish (Phase 12)**: T131, T132, T133, T134, T135, T139, T141 all marked [P] - independent documentation, CI/CD, infrastructure tasks

---

## Parallel Example: User Story 1 (Customer Management)

```bash
# After Foundational phase completes, launch User Story 1 tasks in parallel:

# Launch models together (different entity files):
Task T021: "Create Customer entity in Maliev.CustomerService.Data/Models/Customer.cs"
Task T022: "Create AuditLog entity in Maliev.CustomerService.Data/Models/AuditLog.cs"

# Launch DTOs together (different request/response files):
Task T026: "Create CreateCustomerRequest DTO"
Task T027: "Create UpdateCustomerRequest DTO"
Task T028: "Create CustomerResponse DTO"

# After models configured and DTOs ready, proceed sequentially:
Task T032: "Implement CustomerService" (needs models T021-T024 complete)
Task T033: "Create CustomerController" (needs service T032 complete)
```

---

## Implementation Strategy

### MVP First (User Stories 1, 2, 6 Only - All P1)

1. Complete Phase 1: Setup (T001-T006)
2. Complete Phase 2: Foundational (T007-T020) - CRITICAL BLOCKING PHASE
3. Complete Phase 3: User Story 1 - Customer Management (T021-T035)
4. Complete Phase 4: User Story 2 - Address Management (T036-T049)
5. Complete Phase 5: User Story 6 - User Account & Validation (T050-T064)
6. **STOP and VALIDATE**: Test all three P1 user stories independently
7. Deploy to development environment, run quickstart validation
8. **MVP COMPLETE**: Service can manage customers, addresses, and user authentication

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready (T001-T020)
2. Add User Story 1 → Test independently → Deploy/Demo (MVP Core: Customer CRUD with segmentation/localization)
3. Add User Story 2 → Test independently → Deploy/Demo (Address management with Country Service)
4. Add User Story 6 → Test independently → Deploy/Demo (User accounts and Auth Service validation)
5. Add User Story 3 → Test independently → Deploy/Demo (Company management with VAT validation)
6. Add User Story 4 → Test independently → Deploy/Demo (NDA lifecycle management)
7. Add User Story 5 → Test independently → Deploy/Demo (Document management with Upload Service)
8. Add User Story 7 → Test independently → Deploy/Demo (Internal notes for employees)
9. Add User Story 9 → Test independently → Deploy/Demo (Segmentation querying and compliance)
10. Add User Story 8 → Test independently → Deploy/Demo (Advanced query/filtering)
11. Each story adds value without breaking previous stories

### Parallel Team Strategy

With 3 developers after Foundational phase completes:

1. Team completes Setup + Foundational together (T001-T020)
2. Once Foundational is done:
   - **Developer A**: User Story 1 - Customer Management (T021-T035)
   - **Developer B**: User Story 2 - Address Management (T036-T049)
   - **Developer C**: User Story 6 - User Account Management (T050-T064)
3. All three P1 stories complete in parallel, test independently
4. Next iteration:
   - **Developer A**: User Story 3 - Company Management (T065-T076)
   - **Developer B**: User Story 4 - NDA Management (T077-T089)
   - **Developer C**: User Story 5 - Document Management (T090-T105)
5. Final iteration:
   - **Developer A**: User Story 7 - Internal Notes (T106-T118)
   - **Developer B**: User Story 9 - Segmentation Querying (T119-T125)
   - **Developer C**: User Story 8 - Advanced Filtering (T126-T130)
6. Team collaborates on Polish & Cross-Cutting (T131-T141)

---

## Task Summary

**Total Tasks**: 141 tasks across 12 phases

**Tasks by Phase**:
- Phase 1 (Setup): 6 tasks
- Phase 2 (Foundational): 14 tasks ⚠️ CRITICAL BLOCKER
- Phase 3 (US1 - Customer Management): 15 tasks 🎯 MVP Core
- Phase 4 (US2 - Address Management): 14 tasks 🎯 MVP
- Phase 5 (US6 - User Account Management): 15 tasks 🎯 MVP
- Phase 6 (US3 - Company Management): 12 tasks
- Phase 7 (US4 - NDA Management): 13 tasks
- Phase 8 (US5 - Document Management): 16 tasks
- Phase 9 (US7 - Internal Notes): 13 tasks
- Phase 10 (US9 - Segmentation Querying): 7 tasks
- Phase 11 (US8 - Advanced Filtering): 5 tasks
- Phase 12 (Polish): 11 tasks

**Parallel Opportunities**: 58 tasks marked [P] for parallel execution

**Independent Test Criteria**:
- US1: Create/retrieve/update/delete customer with segmentation and localization
- US2: Manage billing/shipping addresses with Country Service validation
- US6: Create user accounts, validate credentials, track last login
- US3: Create/manage companies with VAT validation and segmentation
- US4: Track NDA lifecycle with automatic expiration
- US5: Manage document metadata with Upload Service integration
- US7: Employee-only internal notes with customer access prevention
- US9: Filter customers by segment/tier, export communication preferences
- US8: Search customers by email/company/date with pagination

**Suggested MVP Scope**: Phase 1 + Phase 2 + Phase 3 (US1) + Phase 4 (US2) + Phase 5 (US6) = 50 tasks for fully functional customer management, address management, and authentication

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label (US1-US9) maps task to specific user story for traceability
- Each user story is independently completable and testable
- Foundational phase (Phase 2) is CRITICAL - blocks all user story work
- Commit after each task or logical group of parallel tasks
- Stop at any checkpoint to validate story independently
- Tests are NOT included per feature specification (not explicitly requested)
- All paths assume repository root is `Maliev.CustomerService/`
- Database migrations use dotnet ef tools from Data project
- External service clients (Country, Upload) use Polly for resilience
- Audit logging on all mutations tracks actorType (Customer/Employee/System)
- Optimistic concurrency via RowVersion on all mutable entities
