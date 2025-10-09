# Feature Specification: Customer Service Microservice

**Feature Branch**: `001-customer-service-microservice`
**Created**: 2025-10-08
**Status**: Draft
**Input**: User description: "Write a **comprehensive functional requirements specification** for a microservice named **Customer Service**. Focus strictly on **what** the service must do — its behaviors, data models, validations, workflows, and external interfaces — without mentioning any technologies, frameworks, or infrastructure choices."

## Clarifications

### Session 2025-10-08

- Q: Should the Customer Service microservice manage user accounts (username/password/roles) for customers and employees? → A: Customer Service owns all user accounts using ASP.NET Core Identity. Auth Service validates credentials via `/customers/v1/validate` endpoint.
- Q: How frequently should background tasks execute (NDA expiration checks, deferred document deletions)? → A: Background tasks can be executed daily.
- Q: What is the implementation approach for user account management? → A: Start from ASP.NET Core Identity scaffold and then expand the required entities from there.
- Q: Do customers and companies need internal notes for employee use? → A: Yes, customer data stores require internal notes (not visible to customer, only visible to employees) to store additional information about specific customer or organization.
- Q: Should Customer Service track business intelligence data for downstream services? → A: Yes, add segment and tier fields to Customer and Company entities to enable Quoting Service pricing rules, Marketing Service targeting, and sales reporting.
- Q: How should the system support localization and communication preferences? → A: Add preferred_language (ISO 639-1), timezone (IANA), and communication_preferences (JSONB) to Customer entity for personalized delivery and compliance.
- Q: Should the system track user activity for security auditing? → A: Yes, add last_login_at timestamp to User entity, updated on successful authentication via `/validate` endpoint for inactive account detection and compliance reporting.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Customer Registration and Basic Information Management (Priority: P1)

A sales representative needs to register a new customer contact into the system, including their personal details, company affiliation, and primary billing address. This forms the foundation of all customer interactions.

**Why this priority**: Without the ability to create and manage customer records, no other functionality can exist. This is the core value proposition of the service.

**Independent Test**: Can be fully tested by creating a customer record with valid data via the API and retrieving it successfully. Delivers immediate value by allowing customer tracking.

**Acceptance Scenarios**:

1. **Given** no existing customer with email "john.doe@example.com", **When** a sales rep creates a customer with first name "John", last name "Doe", email "john.doe@example.com", phone "+66-2-123-4567", segment "Retail", tier "Bronze", preferred language "en", and timezone "Asia/Bangkok", **Then** the system creates a new customer record with a unique identifier and returns the complete customer details including creation timestamp and all specified attributes
2. **Given** an existing customer with email "jane.smith@example.com", **When** a sales rep attempts to create another customer with the same email, **Then** the system rejects the request with a validation error indicating the email is already in use
3. **Given** an existing customer record, **When** a sales rep retrieves the customer by their unique identifier, **Then** the system returns all customer information including segment, tier, preferred_language, timezone, communication_preferences, associated company, addresses, and metadata
4. **Given** an existing customer record, **When** a sales rep updates the customer's phone number and last name, **Then** the system updates only those fields, preserves other data unchanged, and records the modification timestamp, actorId, and actorType as "Employee" in the audit log
5. **Given** an existing customer record, **When** the customer themselves updates their own phone number through self-service, **Then** the system updates the phone number and records the modification with actorType as "Customer" in the audit log, clearly distinguishing it from employee-initiated changes
6. **Given** an existing customer record with preferred_language "en" and timezone "Asia/Bangkok", **When** a sales rep updates the customer's preferred_language to "th" and timezone to "Asia/Singapore", **Then** the system updates those fields and records the modification timestamp in the audit log
7. **Given** an existing customer record, **When** a sales rep updates the communication_preferences to specify email opt-in as true and SMS opt-in as false, **Then** the system stores the preferences in JSONB format and returns the updated customer record with the new preferences
8. **Given** an existing customer record, **When** a sales rep soft-deletes the customer, **Then** the system marks the record as deleted without removing it from storage, preserves all historical data including segment and localization preferences, and excludes it from standard queries

---

### User Story 2 - Multi-Address Management for Billing and Shipping (Priority: P1)

A customer service representative needs to manage multiple shipping addresses for a customer who operates across several locations, while maintaining a separate billing address for invoicing purposes.

**Why this priority**: Essential for order fulfillment and invoicing workflows. Without proper address management, the service cannot support the business's core operations.

**Independent Test**: Can be tested by creating a customer and adding multiple addresses of different types (billing, shipping), then retrieving and updating them independently.

**Acceptance Scenarios**:

1. **Given** an existing customer, **When** a rep adds a billing address with complete details (address lines, city, province, postal code, countryId), **Then** the system creates the address linked to the customer, marks it as "Billing" type, validates the countryId with the Country Service, and returns the address with a unique identifier
2. **Given** an existing customer with one shipping address, **When** a rep adds a second shipping address for a different location, **Then** the system creates the new shipping address without affecting the existing one, allowing multiple shipping addresses per customer
3. **Given** an existing address, **When** a rep updates the postal code and province, **Then** the system updates those fields and records the modification timestamp
4. **Given** an existing customer with multiple addresses, **When** a rep retrieves all addresses for that customer, **Then** the system returns all addresses grouped by type (billing, shipping) with complete details
5. **Given** an existing address, **When** a rep deletes the address, **Then** the system removes the address record and it no longer appears in customer address listings

---

### User Story 3 - Company Master Data Management (Priority: P2)

A business development manager needs to register and manage company information including legal identifiers (VAT number, registration number) and contact details, which can be associated with multiple customer contacts from that organization.

**Why this priority**: Important for B2B operations and regulatory compliance, but customers can exist without company affiliation. Supports organizational structure and tax documentation.

**Independent Test**: Can be tested by creating a company record with VAT details, then associating customers with the company and verifying the relationship.

**Acceptance Scenarios**:

1. **Given** no existing company, **When** a manager creates a company with name "ACME Corp", VAT number "TH-1234567890", registration number "REG123456", contact email, and phone, **Then** the system creates the company record with a unique identifier and validates the VAT number format for the country
2. **Given** an existing company, **When** a manager creates a customer and specifies the company identifier, **Then** the system links the customer to the company and the customer record includes the company association
3. **Given** an existing company, **When** a manager retrieves the company details, **Then** the system returns the company information along with all associated customers
4. **Given** an existing company, **When** a manager updates the contact email and phone number, **Then** the system updates those fields and records the modification timestamp
5. **Given** a company with an invalid VAT number format, **When** a manager attempts to create or update the company, **Then** the system rejects the request with a validation error specifying the expected VAT format for the country

---

### User Story 4 - NDA Lifecycle Management (Priority: P2)

A legal compliance officer needs to track non-disclosure agreements with customers, including their current status (draft, signed, expired, revoked), signing details, and expiration dates to ensure compliance with legal requirements.

**Why this priority**: Critical for legal compliance and business risk management, but not required for basic customer operations. Supports contractual relationship tracking.

**Independent Test**: Can be tested by creating an NDA record for a customer, transitioning it through lifecycle states (draft → signed), and verifying status transitions and validations.

**Acceptance Scenarios**:

1. **Given** an existing customer, **When** a compliance officer creates an NDA record in "Draft" status with an expiration date 12 months in the future, **Then** the system creates the NDA linked to the customer with status "Draft" and no signed date
2. **Given** an existing NDA in "Draft" status with an associated document reference, **When** an officer marks the NDA as "Signed" and provides signing details (signedBy, signedAt), **Then** the system transitions the NDA to "Signed" status, records the signing metadata, and validates that a document reference exists
3. **Given** an NDA in "Draft" status without a document reference, **When** an officer attempts to mark it as "Signed", **Then** the system rejects the request with a validation error indicating a document must be attached before signing
4. **Given** an NDA in "Signed" status, **When** an officer revokes the NDA with a revocation timestamp, **Then** the system transitions the NDA to "Revoked" status, records the revocation timestamp, and preserves all previous signing metadata
5. **Given** an NDA with an expiration date in the past, **When** the system evaluates NDA status, **Then** the NDA is automatically marked as "Expired" status
6. **Given** an existing NDA, **When** an officer retrieves the NDA details, **Then** the system returns the complete NDA information including current status, document reference, signing details, and all lifecycle timestamps

---

### User Story 5 - Document Metadata Management with Upload Service Integration (Priority: P2)

A document controller needs to register and track customer-related documents (company registration certificates, signed NDAs, contracts) by storing metadata and references to files managed by an external Upload Service, without handling file storage directly.

**Why this priority**: Supports document tracking and audit trails, but is secondary to core customer data. Enables integration with external file storage systems.

**Independent Test**: Can be tested by registering a document reference with a valid Upload Service file ID, marking it as complete, and verifying metadata retrieval.

**Acceptance Scenarios**:

1. **Given** an existing customer and a valid file reference from the Upload Service, **When** a controller registers a document with type "Company Registration", filename, and file reference, **Then** the system creates a document reference record linked to the customer with status "Pending" and version 1
2. **Given** an existing document reference in "Pending" status, **When** a controller marks the document as complete, **Then** the system transitions the document status to "Complete" and records the completion timestamp
3. **Given** an existing document reference, **When** a controller updates the document with a new file reference (new version), **Then** the system increments the version number, updates the file reference, and preserves the previous version metadata in audit logs
4. **Given** an existing customer with multiple document references, **When** a controller retrieves all documents for the customer, **Then** the system returns all document metadata including type, status, version, and file references, grouped by document type
5. **Given** an existing document reference, **When** a controller deletes the document, **Then** the system calls the Upload Service to remove the physical file, then removes the document reference metadata, and logs the deletion action
6. **Given** an invalid or non-existent Upload Service file reference, **When** a controller attempts to register a document, **Then** the system validates the file reference with the Upload Service and rejects the request if the reference is invalid

---

### User Story 6 - User Account Management and Credential Validation (Priority: P1)

The Customer Service must manage user accounts (username, password, roles) for both customers and employees. An external Auth Service validates user credentials by calling the Customer Service's `/customers/v1/validate` endpoint, which verifies username/password combinations and returns user identity and role information without exposing whether the user exists.

**Why this priority**: Core authentication capability required for all secure operations. Customer Service owns user identity data and provides credential validation as a service to the Auth Service.

**Independent Test**: Can be tested by creating user accounts, calling the validate endpoint with valid and invalid credentials, and verifying response format and security properties.

**Acceptance Scenarios**:

1. **Given** a new user (customer or employee) needs system access, **When** a user account is created with username, password, email, and role, **Then** the system stores the user account with securely hashed password and returns the user identifier
2. **Given** a valid username and password stored in Customer Service, **When** the Auth Service sends a validation request to `/customers/v1/validate`, **Then** the system verifies the credentials, returns isValid=true with userId, username, and roles array
3. **Given** an invalid username or incorrect password, **When** the Auth Service sends a validation request, **Then** the system returns isValid=false with a generic error message without indicating whether the username exists
4. **Given** repeated validation attempts from the same source, **When** the rate limit threshold is exceeded, **Then** the system rejects subsequent requests with a rate limit error and logs the activity for security monitoring
5. **Given** any validation request (successful or failed), **When** the request is processed, **Then** the system logs the validation attempt with timestamp, source identifier, and outcome (without logging password) for audit purposes
6. **Given** an existing user account, **When** a password reset is requested, **Then** the system updates the password hash and invalidates any existing sessions
7. **Given** user roles determine access levels, **When** a user's role is updated (e.g., Customer → Employee), **Then** the system updates the role and the change is reflected in subsequent authentication attempts

---

### User Story 7 - Internal Notes Management for Employee Use (Priority: P2)

A sales representative or account manager needs to record internal observations, account history, special instructions, or other confidential information about customers and companies that should never be visible to the customer themselves.

**Why this priority**: Critical for effective account management and sales operations, but secondary to core customer data. Supports internal knowledge sharing and account continuity.

**Independent Test**: Can be tested by creating internal notes for customers and companies, verifying employees can view/edit them, and confirming customers cannot access them.

**Acceptance Scenarios**:

1. **Given** an existing customer, **When** an employee adds an internal note with text "Customer prefers morning calls", **Then** the system creates the note linked to the customer, records the employee's identifier as creator, and returns the note with timestamp
2. **Given** an existing customer with internal notes, **When** an employee retrieves the customer details, **Then** the system returns all internal notes with note text, creator identifier, and timestamps
3. **Given** an existing internal note, **When** an employee updates the note text, **Then** the system updates the note content and records the modification timestamp
4. **Given** an existing customer with internal notes, **When** the customer themselves retrieves their own profile via self-service, **Then** the system returns customer details WITHOUT any internal notes (notes remain hidden)
5. **Given** an existing internal note, **When** an employee deletes the note, **Then** the system removes the note and it no longer appears in the customer's internal notes list
6. **Given** an existing company, **When** an employee adds an internal note with text "Negotiating volume discount", **Then** the system creates the note linked to the company with the employee's identifier and timestamp
7. **Given** an employee attempts to access internal notes, **When** the request is authenticated as a customer user, **Then** the system rejects the request with HTTP 403 Forbidden indicating insufficient permissions

---

### User Story 8 - Query and Filtering Capabilities (Priority: P3)

A sales manager needs to search and filter customer records by various criteria (email, company affiliation, creation date range) with pagination to efficiently locate specific customers from a large dataset.

**Why this priority**: Enhances usability and operational efficiency but is not required for basic CRUD operations. Supports reporting and data analysis workflows.

**Independent Test**: Can be tested by creating multiple customer records with different attributes, then querying with various filters and verifying correct result sets and pagination.

**Acceptance Scenarios**:

1. **Given** multiple customers in the system, **When** a manager queries customers with pagination (page 1, 20 items per page), **Then** the system returns the first 20 customers sorted by creation date (newest first) with total count and pagination metadata
2. **Given** multiple customers from different companies, **When** a manager filters customers by company ID, **Then** the system returns only customers associated with that company
3. **Given** customers with various email domains, **When** a manager searches customers by email containing "@example.com", **Then** the system returns all customers whose email matches the search criteria
4. **Given** customers created over several months, **When** a manager filters customers by creation date range (start and end dates), **Then** the system returns only customers created within that date range
5. **Given** both active and soft-deleted customers, **When** a manager queries customers without specifying deleted status, **Then** the system returns only active (non-deleted) customers by default

---

### User Story 9 - Customer Segmentation and Communication Preferences Management (Priority: P2)

A marketing manager and sales operations team need to classify customers by business segment (Retail, Wholesale, Enterprise, Government) and tier (Bronze, Silver, Gold, Platinum, VIP) to enable downstream services like Quoting Service to apply targeted pricing rules and Marketing Service to execute personalized campaigns. Additionally, customer service representatives need to manage localization preferences (preferred language, timezone) and communication preferences (email, SMS opt-in/out) to ensure personalized, compliant customer communications.

**Why this priority**: Important for business intelligence, targeted marketing, and personalized customer experience, but not required for basic customer operations. Supports integration with downstream services for pricing rules and campaign execution.

**Independent Test**: Can be tested by creating customers with different segments and tiers, updating localization and communication preferences, and verifying that downstream services can query and filter by these attributes.

**Acceptance Scenarios**:

1. **Given** a new customer is being created, **When** a sales rep specifies segment as "Retail" and tier as "Bronze", **Then** the system stores these values and makes them available for downstream services to query for pricing and marketing rules
2. **Given** an existing Retail/Bronze customer with growing purchase volume, **When** a sales operations manager updates the tier to "Silver", **Then** the system updates the tier and records the change in audit logs for business intelligence tracking
3. **Given** an existing customer with segment "Wholesale", **When** a manager updates the segment to "Enterprise" to reflect the customer's business growth, **Then** the system updates the segment and downstream services receive the updated classification
4. **Given** a customer prefers Thai language communications, **When** a customer service rep sets preferred_language to "th" and timezone to "Asia/Bangkok", **Then** the system stores these preferences and makes them available to downstream services for localized delivery
5. **Given** an existing customer with preferred_language "en", **When** the customer updates their own language preference to "th" through self-service, **Then** the system updates the preference and records actorType as "Customer" in the audit log
6. **Given** a customer wants to control communication channels, **When** a customer service rep updates communication_preferences to set email_opt_in=true, sms_opt_in=false, marketing_opt_in=true, **Then** the system stores the preferences in JSONB format and makes them available to downstream services for compliance
7. **Given** marketing needs to target premium customers, **When** the Marketing Service queries customers with tier "Platinum" or "VIP", **Then** the system returns all customers matching those tier criteria for campaign targeting
8. **Given** quoting service needs segment-based pricing, **When** the Quoting Service queries a customer's segment and tier, **Then** the system provides these attributes to enable dynamic pricing rules (e.g., Enterprise customers get volume discounts)
9. **Given** a notification service needs to send localized messages, **When** the service queries a customer's preferred_language and timezone, **Then** the system provides these values to enable properly formatted, timezone-appropriate communications
10. **Given** a compliance report requires communication consent tracking, **When** an auditor queries communication_preferences for all customers, **Then** the system provides the opt-in/opt-out status for each communication channel for regulatory compliance

---

### Edge Cases

- What happens when a customer is soft-deleted but has active NDA records?
  - System preserves all NDA records and document references linked to the deleted customer
  - NDA status remains unchanged and continues to track lifecycle
  - Soft-deleted customers can be restored with all relationships intact

- What happens when the Upload Service is unavailable during document deletion?
  - System marks the document reference for deferred deletion with a "PendingDeletion" status
  - Background process retries deletion when Upload Service becomes available
  - Document remains in the system until physical file deletion is confirmed

- What happens when the Country Service is unavailable during address creation or update?
  - System returns HTTP 503 Service Unavailable error to the caller
  - Operation is not completed until Country Service validates the countryId
  - Error message indicates the dependency failure and suggests retry

- What happens when multiple addresses have the same type (e.g., two billing addresses)?
  - System allows multiple addresses of the same type
  - Most recent address of each type can be marked as "primary" or "default" (assumed behavior)

- What happens when an NDA expires while in "Draft" status?
  - System transitions NDA directly from "Draft" to "Expired"
  - No signing is possible after expiration

- What happens when a customer is linked to a company that is later deleted or modified?
  - Company records are not deleted, only modified (assumed behavior)
  - If company data changes (e.g., VAT number update), customer link remains valid
  - System maintains referential integrity between customer and company

- What happens when validation requests are sent for non-existent users?
  - System returns the same generic "invalid credentials" response as for incorrect passwords
  - No indication is given about whether the username exists
  - All attempts are logged with the same security event type

- What happens when a document reference points to an Upload Service file that no longer exists?
  - System marks document reference as "Orphaned" or "MissingFile" status
  - Document metadata is preserved for audit purposes
  - System logs the inconsistency for manual review

- What happens when concurrent updates occur on the same customer record?
  - System uses optimistic concurrency control (version stamps or ETags - assumed mechanism)
  - Second update receives a conflict error (409) indicating the record was modified
  - Caller must retrieve latest version and retry the update

- What happens when external services are unavailable during health checks?
  - `/customers/liveness` endpoint always returns healthy if the service process is running, regardless of external service status
  - `/customers/readiness` endpoint returns unhealthy status and lists which external services (Upload Service, Country Service, Identity System) are unavailable
  - Orchestration platform can use readiness status to stop routing traffic to unhealthy instances

## Requirements *(mandatory)*

### Functional Requirements

#### Customer Management

- **FR-001**: System MUST create customer records with unique identifiers, storing firstName, lastName, email, phone, optional companyId, and automatic timestamps (createdAt, updatedAt)
- **FR-002**: System MUST enforce unique email addresses across all active customers
- **FR-003**: System MUST validate email format according to RFC 5322 standard
- **FR-004**: System MUST validate phone number format according to E.164 international standard
- **FR-005**: System MUST require firstName and lastName fields (non-empty, trimmed)
- **FR-006**: System MUST support partial updates (PATCH operations) allowing modification of individual fields without affecting others
- **FR-007**: System MUST implement soft deletion by setting isDeleted flag without removing records from storage
- **FR-008**: System MUST exclude soft-deleted customers from standard queries by default
- **FR-009**: System MUST allow retrieval of individual customers by unique identifier
- **FR-010**: System MUST support customer list queries with pagination (page number, page size)
- **FR-011**: System MUST support filtering customers by companyId, email (partial match), and creation date range
- **FR-012**: System MUST return customer list results sorted by creation date descending by default
- **FR-013**: System MUST update updatedAt timestamp on every modification to customer records

#### Company Management

- **FR-014**: System MUST create company records with unique identifiers, storing name, vatNumber, registrationNumber, contactEmail, contactPhone, and automatic timestamps
- **FR-015**: System MUST validate VAT number format using regex pattern `^[A-Z]{2}-\d{10,15}$` for country-prefixed format (e.g., "TH-1234567890")
- **FR-016**: System MUST allow customers to reference company records via companyId foreign key relationship
- **FR-017**: System MUST validate contactEmail format using the same rules as customer email (RFC 5322)
- **FR-018**: System MUST validate contactPhone format using E.164 standard
- **FR-019**: System MUST support partial updates to company records
- **FR-020**: System MUST retrieve company details including list of associated customers

#### Address Management

- **FR-021**: System MUST create address records linked to either customers or companies via ownerType ("Customer" or "Company") and ownerId
- **FR-022**: System MUST classify addresses as either "Billing" or "Shipping" type
- **FR-023**: System MUST require addressLine1, city, postalCode, and countryId fields for all addresses
- **FR-024**: System MUST validate countryId by querying the Country Service before accepting address creation or updates
- **FR-025**: System MUST allow optional addressLine2 and province fields
- **FR-026**: System MUST support multiple addresses per customer or company
- **FR-027**: System MUST allow multiple addresses of the same type (e.g., multiple shipping addresses)
- **FR-028**: System MUST support partial updates to address records
- **FR-029**: System MUST allow deletion of addresses without affecting the owner (customer or company)
- **FR-030**: System MUST retrieve all addresses for a given customer or company, filterable by type

#### Document Reference Management

- **FR-031**: System MUST create document reference records linked to owners (customers or companies) via ownerType and ownerId
- **FR-032**: System MUST store document metadata including documentType, fileReference (from Upload Service), filename, status, and version
- **FR-033**: System MUST validate that fileReference corresponds to a valid file in the Upload Service before creating document reference
- **FR-034**: System MUST initialize document references with status "Pending" and version 1
- **FR-035**: System MUST allow marking document references as "Complete" via dedicated completion endpoint
- **FR-036**: System MUST support document versioning by incrementing version number when file reference is updated
- **FR-037**: System MUST store signing metadata (signedBy, signedAt) when documents are signed
- **FR-038**: System MUST call Upload Service to delete physical files when document references are deleted
- **FR-039**: System MUST handle Upload Service deletion failures by marking documents for deferred deletion and retrying asynchronously
- **FR-040**: System MUST retrieve all document references for a given owner, filterable by documentType and status
- **FR-041**: System MUST prevent creation of document references with invalid or non-existent Upload Service file references

#### NDA Lifecycle Management

- **FR-042**: System MUST create NDA records linked to customers with initial status "Draft"
- **FR-043**: System MUST link NDA records to document references via documentReferenceId
- **FR-044**: System MUST support NDA status values: "Draft", "Signed", "Expired", "Revoked"
- **FR-045**: System MUST enforce lifecycle transition rules: Draft → Signed, Signed → Revoked, Signed → Expired, Draft → Expired
- **FR-046**: System MUST prevent transition to "Signed" status without an associated document reference
- **FR-047**: System MUST require signedBy and signedAt metadata when transitioning to "Signed" status
- **FR-048**: System MUST store revokedAt timestamp when transitioning to "Revoked" status
- **FR-049**: System MUST automatically transition NDAs to "Expired" status when current date exceeds expiresAt date via daily background job
- **FR-050**: System MUST prevent modifications to NDA status after "Revoked" or "Expired" (terminal states)
- **FR-051**: System MUST retrieve individual NDA details including all lifecycle metadata and associated document reference
- **FR-052**: System MUST execute background jobs daily to check for expired NDAs and retry deferred document deletions

#### User Account Management

- **FR-053**: System MUST manage user accounts for customers, storing username, email, password hash, and roles
- **FR-054**: System MUST enforce unique usernames across all user accounts
- **FR-055**: System MUST hash passwords using secure one-way hashing algorithm (never store plain text passwords)
- **FR-056**: System MUST support user roles including Customer, Employee, Manager, and Admin
- **FR-057**: System MUST provide user account creation endpoint accepting username, password, email, and role
- **FR-058**: System MUST provide password reset functionality that updates password hash and invalidates existing sessions
- **FR-059**: System MUST allow role updates for existing user accounts
- **FR-060**: System MUST link user accounts to Customer records for customer users

#### Credential Validation

- **FR-061**: System MUST validate username and password combinations against internally stored user accounts at `/customers/v1/validate` endpoint
- **FR-062**: System MUST return validation response indicating isValid (boolean), and if valid: userId, username, and roles array
- **FR-063**: System MUST return generic error message for invalid credentials without indicating whether username exists
- **FR-064**: System MUST implement rate limiting on validation endpoint (10 requests per minute per source IP)
- **FR-065**: System MUST log all validation attempts with timestamp, source identifier, and outcome (without logging password)
- **FR-066**: System MUST audit both successful and failed validation attempts for security monitoring

#### Internal Notes Management

- **FR-067**: System MUST create internal note records linked to either customers or companies via ownerType ("Customer" or "Company") and ownerId
- **FR-068**: System MUST store note text, creator identifier (employee), creation timestamp, and last updated timestamp for each internal note
- **FR-069**: System MUST restrict internal note creation, viewing, updating, and deletion to employee users only (roles: Employee, Manager, Admin)
- **FR-070**: System MUST prevent customer users from accessing internal notes via API authorization policies
- **FR-071**: System MUST exclude internal notes from customer self-service endpoints (customer profile retrieval returns no notes)
- **FR-072**: System MUST return all internal notes for a given customer or company when requested by an employee
- **FR-073**: System MUST allow employees to update internal note text with automatic timestamp update
- **FR-074**: System MUST allow employees to delete internal notes permanently
- **FR-075**: System MUST record internal note creation and deletion in audit logs with employee identifier

#### Error Handling and Validation

- **FR-076**: System MUST return structured error responses with code, message, details object, traceId, and HTTP status
- **FR-077**: System MUST return HTTP 400 for request validation errors (malformed data, missing required fields)
- **FR-078**: System MUST return HTTP 401 for unauthorized access attempts
- **FR-079**: System MUST return HTTP 403 for forbidden operations (insufficient permissions)
- **FR-080**: System MUST return HTTP 404 for requests targeting non-existent resources
- **FR-081**: System MUST return HTTP 409 for conflict errors (e.g., duplicate email, optimistic concurrency failures)
- **FR-082**: System MUST return HTTP 422 for domain validation failures (e.g., invalid VAT format, invalid lifecycle transition)
- **FR-083**: System MUST provide detailed validation error messages specifying which fields failed validation and why
- **FR-084**: System MUST include unique traceId in all error responses for debugging and support

#### Audit and Logging

- **FR-085**: System MUST record audit log entries for all create, update, and delete operations
- **FR-086**: System MUST capture actorId (user/service performing action), actorType (distinguishing "Customer", "Employee", or "System"), action type, timestamp, and entity identifier in audit logs
- **FR-087**: System MUST record changed fields and previous values for update operations in audit logs
- **FR-088**: System MUST derive actorType from JWT claims (userType field) to identify whether modifications were made by the customer themselves or by an employee
- **FR-089**: System MUST log all access attempts including successful and failed authentication/authorization checks
- **FR-090**: System MUST exclude sensitive data (passwords, full credit card numbers if any) from logs and error messages
- **FR-091**: System MUST log integration calls to Upload Service and Country Service including request/response status and any failures

#### Security and Access Control

- **FR-092**: System MUST enforce authentication on all endpoints except `/customers/v1/validate`, `/customers/liveness`, and `/customers/readiness`
- **FR-093**: System MUST authorize operations based on user roles: Customer, Employee, Manager, and Admin
- **FR-094**: System MUST distinguish between customer self-service operations and employee-initiated operations for authorization purposes
- **FR-095**: System MUST log security events including unauthorized access attempts, rate limit violations, and suspicious activity patterns

#### System Integration

- **FR-096**: System MUST integrate with Upload Service at `/uploads/v1` endpoints for file reference validation and deletion
- **FR-097**: System MUST integrate with Country Service to validate countryId references in address records
- **FR-098**: System MUST handle Upload Service and Country Service timeouts gracefully by returning appropriate error responses and logging failures
- **FR-099**: System MUST validate file references by calling Upload Service before accepting document reference creation
- **FR-100**: System MUST validate countryId by calling Country Service before accepting address creation or updates
- **FR-101**: System MUST call Upload Service deletion endpoint when document references are deleted, handling failures with retry logic

#### System Health and Documentation

- **FR-102**: System MUST expose liveness probe endpoint at `/customers/liveness` returning simple status without authentication, indicating the service process is running
- **FR-103**: System MUST expose readiness probe endpoint at `/customers/readiness` indicating when system is ready to accept traffic, including health status of external service dependencies (Database, Upload Service, Country Service)
- **FR-104**: System MUST return unhealthy status on `/customers/readiness` if any external service dependency is unavailable or unhealthy
- **FR-105**: System MUST serve interactive API documentation at `/customers/swagger`
- **FR-106**: System MUST generate OpenAPI specification covering all endpoints, request/response schemas, and error codes

#### Customer Segmentation and Business Intelligence

- **FR-107**: System MUST support customer segment classification with enumerated values: "Retail", "Wholesale", "Enterprise", "Government"
- **FR-108**: System MUST support customer tier classification with enumerated values: "Bronze", "Silver", "Gold", "Platinum", "VIP"
- **FR-109**: System MUST validate customer segment values against the enumerated list and reject invalid values with HTTP 400
- **FR-110**: System MUST validate customer tier values against the enumerated list and reject invalid values with HTTP 400
- **FR-111**: System MUST allow filtering customer queries by segment and tier for downstream service integration
- **FR-112**: System MUST record segment and tier changes in audit logs with previous and new values for business intelligence tracking

#### Company Segmentation

- **FR-113**: System MUST support company segment classification with enumerated values: "SMB", "MidMarket", "Enterprise", "Government"
- **FR-114**: System MUST support company tier classification with enumerated values: "Standard", "Premium", "Strategic", "Partner"
- **FR-115**: System MUST validate company segment and tier values against their respective enumerated lists and reject invalid values with HTTP 400
- **FR-116**: System MUST allow filtering company queries by segment and tier
- **FR-117**: System MUST provide company list query endpoint with pagination supporting segment and tier filters

#### Localization and Communication Preferences

- **FR-118**: System MUST support customer preferred_language field using ISO 639-1 two-letter language codes (e.g., "en", "th", "zh")
- **FR-119**: System MUST validate preferred_language values against ISO 639-1 standard and reject invalid codes with HTTP 400
- **FR-120**: System MUST support customer timezone field using IANA timezone identifiers (e.g., "Asia/Bangkok", "America/New_York")
- **FR-121**: System MUST validate timezone values against IANA timezone database and reject invalid identifiers with HTTP 400
- **FR-122**: System MUST support customer communication_preferences field stored as flexible JSON structure for opt-in/opt-out settings (e.g., email_opt_in, sms_opt_in, marketing_opt_in)
- **FR-123**: System MUST preserve communication_preferences structure when storing and retrieving JSON data without schema validation (allowing downstream services to define their own preference keys)

#### User Activity Tracking

- **FR-124**: System MUST support last_login_at timestamp field on User entity to track most recent successful authentication
- **FR-125**: System MUST update last_login_at timestamp when validation endpoint `/customers/v1/validate` returns successful authentication (isValid=true)
- **FR-126**: System MUST provide user list query endpoint with pagination supporting last_login_at date range filters for inactive account detection
- **FR-127**: System MUST support filtering users by last_login_at date range for security auditing and compliance reporting

### Non-Functional Requirements

#### Performance

- **NFR-001**: System MUST respond to GET requests with p95 latency under 150 milliseconds under normal load conditions
- **NFR-002**: System MUST respond to POST/PATCH/DELETE requests with p95 latency under 200 milliseconds under normal load conditions
- **NFR-003**: System MUST handle 1000 concurrent operations without performance degradation
- **NFR-004**: System MUST process credential validation requests with p95 latency under 200 milliseconds

#### Availability and Reliability

- **NFR-005**: System MUST maintain 99.9% uptime (maximum 8.76 hours downtime per year)
- **NFR-006**: System MUST implement optimistic concurrency control using row versioning to prevent lost updates
- **NFR-007**: System MUST retry failed external service calls with exponential backoff (3 attempts, 2^n second delays)
- **NFR-008**: System MUST gracefully degrade when external services are unavailable by returning appropriate HTTP 503 responses

#### Security

- **NFR-009**: System MUST use TLS 1.3 for all external communication
- **NFR-010**: System MUST hash passwords using PBKDF2 or Argon2 algorithms via ASP.NET Core Identity
- **NFR-011**: System MUST enforce JWT authentication with issuer and audience validation on all protected endpoints
- **NFR-012**: System MUST implement rate limiting: 100 requests/minute general, 10 requests/minute for validation endpoint
- **NFR-013**: System MUST log all security events without exposing sensitive data (passwords, tokens, full credit card numbers)

#### Testability and Quality

- **NFR-014**: System MUST achieve minimum 80% code coverage for business logic (per Constitution Principle III)
- **NFR-015**: System MUST build with zero warnings and zero errors in CI/CD pipeline
- **NFR-016**: System MUST validate all request inputs using FluentValidation with detailed error messages

#### Scalability

- **NFR-017**: System MUST support horizontal scaling via stateless API design
- **NFR-018**: System MUST use connection pooling for database connections
- **NFR-019**: System MUST cache Country Service validation results for 24 hours to reduce external calls

#### Maintainability

- **NFR-020**: System MUST follow Clean Architecture pattern (Controllers → Services → Data)
- **NFR-021**: System MUST use structured JSON logging to stdout for container environments
- **NFR-022**: System MUST provide OpenAPI specification matching actual implementation with 100% accuracy

### Key Entities

- **Customer**: Represents an individual contact in the system. Core attributes include unique identifier, personal information (first name, last name, email, phone), business classification (segment, tier), localization preferences (preferred_language using ISO 639-1 codes, timezone using IANA identifiers), communication preferences (JSONB for flexible opt-in/opt-out settings), optional company affiliation, lifecycle flags (soft deletion), and audit timestamps. Relationships: belongs to zero or one Company, has many Addresses, has many DocumentReferences, has many NDARecords, has many InternalNotes. Business context: segment and tier enable downstream services (Quoting, Marketing) to apply targeted rules and campaigns.

- **Company**: Represents a business entity or organization. Core attributes include unique identifier, legal name, tax identifiers (VAT number, registration number), business classification (segment, tier), contact information, and audit timestamps. Relationships: has many Customers, has many Addresses, has many DocumentReferences, has many InternalNotes. Business context: segment and tier support B2B relationship management and account-based pricing strategies.

- **Address**: Represents a physical address for billing or shipping purposes. Core attributes include unique identifier, owner link (polymorphic: Customer or Company), address type (Billing or Shipping), complete address components (lines, city, province, postal code, countryId referencing Country Service). Relationships: belongs to one Customer or one Company, references one Country (external service).

- **DocumentReference**: Represents metadata for a document stored in the Upload Service. Core attributes include unique identifier, owner link (polymorphic: Customer or Company), document classification (document type), file reference (external Upload Service ID), filename, lifecycle status, version number, signing metadata (signedBy, signedAt), and audit timestamps. Relationships: belongs to one Customer or one Company, may be referenced by one NDARecord.

- **NDARecord**: Represents a non-disclosure agreement with lifecycle tracking. Core attributes include unique identifier, customer link, document reference link, lifecycle status (Draft, Signed, Expired, Revoked), signing metadata (signedAt, signedBy), revocation timestamp, expiration date. Relationships: belongs to one Customer, references one DocumentReference.

- **InternalNote**: Represents an internal note or comment for employee use only. Core attributes include unique identifier, owner link (polymorphic: Customer or Company), note text, creator identifier (employee who created the note), creation timestamp, last updated timestamp. Relationships: belongs to one Customer or one Company. Security: accessible only by employee users, never visible to customers via self-service endpoints.

- **User**: Represents a user account for authentication and authorization (extends ASP.NET Core Identity's IdentityUser). Core attributes include unique identifier, username, email, password hash, roles (Customer, Employee, Manager, Admin), activity tracking (last_login_at timestamp), and audit timestamps. Relationships: linked to Customer records for customer users via linked_customer_id field. Security context: last_login_at enables inactive account detection, security auditing, and compliance reporting by tracking most recent successful authentication.

- **AuditLog**: Represents a historical record of system changes. Core attributes include unique identifier, actor identifier (user/service), actorType (Customer, Employee, or System), action type (create, update, delete), timestamp, entity type and identifier, changed field names, previous values. Relationships: conceptually linked to all entities but not enforced via foreign keys. The actorType field enables distinguishing between customer self-service modifications and employee-initiated changes for compliance and security tracking.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a complete customer record (including billing address) in under 30 seconds through the API
- **SC-002**: System processes customer retrieval requests in under 100 milliseconds at 95th percentile under normal load
- **SC-003**: System handles 1000 concurrent customer management operations without performance degradation
- **SC-004**: 100% of validation errors return actionable error messages specifying which field failed and why
- **SC-005**: Zero customer data is lost or corrupted during soft deletion and restoration operations
- **SC-006**: 100% of document deletion operations successfully remove files from Upload Service or queue for retry within 5 minutes
- **SC-007**: NDA lifecycle transitions conform to business rules with zero invalid state transitions accepted
- **SC-008**: System maintains 99.9% uptime measured via `/customers/liveness` endpoint over a 30-day period
- **SC-009**: Readiness probe at `/customers/readiness` accurately reflects external service health with 100% correlation (reports unhealthy when any dependency is unavailable)
- **SC-010**: All API operations complete with p95 latency under 150 milliseconds under normal load conditions
- **SC-011**: 100% of create, update, and delete operations are recorded in audit logs with complete actor and change tracking
- **SC-012**: Credential validation requests complete in under 200 milliseconds at 95th percentile
- **SC-013**: Rate limiting on validation endpoint prevents abuse with zero false positives (legitimate users are never blocked incorrectly)
- **SC-014**: API documentation at `/customers/swagger` covers 100% of endpoints with request/response examples
- **SC-015**: System scales horizontally to support 10,000 customer records with consistent query performance (list queries remain under 200ms)
- **SC-016**: Zero sensitive data (passwords, internal identifiers) appears in error messages or logs
- **SC-017**: Integration with Upload Service handles failures gracefully with 100% eventual consistency for document deletions

## Assumptions

1. **VAT Number Validation**: VAT numbers include country code prefix (e.g., "TH-1234567890" for Thailand) allowing the system to determine which country-specific validation rules to apply
2. **Identity System Integration**: A central identity system exists and provides an API for credential validation, role/permission verification, and actor type identification (Customer vs Employee)
3. **Country Service Integration**: An external Country Service provides country information and validation. This service exposes an endpoint to validate countryId references. Customer Service stores only the countryId reference, not country names or other country data.
4. **Upload Service Contract**: The Upload Service provides validation and deletion endpoints at `/uploads/v1` and returns standard HTTP status codes
5. **Rate Limiting**: Validation endpoint allows 10 requests per minute per source IP address before applying rate limiting
6. **Default Pagination**: List endpoints return 20 items per page by default unless specified otherwise
7. **Optimistic Concurrency**: System uses version stamps or ETags for concurrency control, though specific mechanism is implementation-specific
8. **Authentication Mechanism**: Bearer token authentication is used (implementation details excluded per requirements). The authentication token includes claims indicating whether the user is a customer or an employee.
9. **Actor Type Determination**: The identity system provides actor type (Customer, Employee, System) as part of authentication claims, enabling the Customer Service to record this in audit logs without additional lookups.
10. **Time Zones**: All timestamps stored and returned in UTC
11. **Soft Deletion Scope**: Soft-deleted customers are excluded from standard queries but can be restored by administrative operations
12. **Address Primary/Default**: While multiple addresses of the same type are allowed, applications consuming the API may implement their own logic for marking "primary" addresses
13. **Document Type Values**: Document types are free-form strings (e.g., "Company Registration", "NDA", "Contract") without predefined enumeration
14. **Audit Log Retention**: Audit logs are retained indefinitely (retention policy is outside scope of this specification)
15. **Background Processing**: System includes background job processing for deferred document deletion retries and automatic NDA expiration checks, executed daily
16. **Error Message Language**: All error messages and validation feedback are in English
17. **Company Deletion**: Companies are not soft-deleted; they are only modified. Customer-company relationships remain valid when company data changes.
18. **Health Check Behavior**: The `/customers/liveness` endpoint checks only if the service process is running (lightweight check). The `/customers/readiness` endpoint performs comprehensive health checks including database connectivity and external service availability (Upload Service, Country Service, Identity System). Health checks do not require authentication to support orchestration platform polling.

## Open Questions

[No critical clarifications needed - all ambiguities resolved through reasonable assumptions documented above]
