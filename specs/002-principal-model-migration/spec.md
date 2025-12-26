# Feature Specification: Principal-First Model Migration

**Feature Branch**: `002-principal-model-migration`  
**Created**: 2025-12-21  
**Status**: Draft  
**Input**: User description: "Principal-First Model Migration: Migrate CustomerService from ASP.NET Core Identity to IAM-owned identity (principal_id)."

## Clarifications

### Session 2025-12-21
- Q: How should the system handle customer records that cannot be automatically linked to a central identity during the backfill? → A: Mark as "Migration Pending" and log for manual administrative review.
- Q: What is the preferred user experience when the identity service is slow (e.g., > 2 seconds) during a new registration? → A: Fail fast with a user-friendly "Service Busy" error after 2 seconds.
- Q: If a new customer registers with an email that already has a central identity in IAM, how should we proceed? → A: Automatically link the new customer record to the existing central identity.
- Q: What is the specific p95 latency target for the new "Get Customer by Central Identity" endpoint? → A: < 50ms p95.
- Q: For how long should legacy identity tables be archived after being dropped? → A: 90 days.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Existing Customer Access (Priority: P1)

As an existing customer, I want to maintain access to my account and data after the system migrates to a central identity model, so that my service is not interrupted.

**Why this priority**: Critical for business continuity and user retention. Interruption in access is unacceptable.

**Independent Test**: Can be tested by verifying that a customer record linked to a legacy local identity record can be successfully linked to a new central identity and remains accessible via that identity.

**Acceptance Scenarios**:

1. **Given** a customer exists with a legacy local identity account, **When** the migration process runs, **Then** a corresponding central identity is created and linked to the Customer record.
2. **Given** a migrated customer, **When** they authenticate via the central identity service, **Then** the Customer Service correctly identifies them using their new central identity.

---

### User Story 2 - New Customer Registration (Priority: P1)

As a new customer, I want my identity to be automatically created in the central identity system when I register, so that I can use my credentials across all services.

**Why this priority**: Essential for the new architecture. Ensures all new data is born in the correct format.

**Independent Test**: Register a new customer and verify a central identity is assigned to them and exists in the central identity system.

**Acceptance Scenarios**:

1. **Given** valid registration details, **When** a new customer is created, **Then** the system first requests a new identity from the central identity service before saving the customer record.
2. **Given** the central identity service is temporarily unavailable, **When** a customer attempts to register, **Then** the system provides a clear error message and does not create an orphaned customer record.

---

### User Story 3 - Service-to-Service Customer Lookup (Priority: P2)

As a developer of another service, I want to be able to find a customer's business data using only their central identity, so that I don't need to know their local database ID.

**Why this priority**: Facilitates integration across the microservices ecosystem.

**Independent Test**: Request customer details using a valid central identity and receive the correct customer profile.

**Acceptance Scenarios**:

1. **Given** a valid central identity, **When** requested via the public interface, **Then** the system returns the corresponding customer's business details.
2. **Given** a central identity that does not exist in the Customer Service, **When** requested, **Then** the system returns a "Not Found" response.

---

### Edge Cases

- **Identity Service Latency**: The system will fail fast with a user-friendly "Service Busy" error if the central identity service does not respond within 2 seconds during registration.
- **Duplicate Identity Check**: If an identity already exists in the central system for a user's email during new registration, the system will automatically link the new customer record to that existing identity.
- **Sync Failures**: Customers whose identity link fails to update during the migration window are marked as "Migration Pending" for manual administrative review.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST store a unique reference to a central identity for every customer record.
- **FR-002**: System MUST coordinate with the central identity service to create identities for new customers.
- **FR-003**: System MUST provide a mechanism to retrieve customer profiles using only their central identity.
- **FR-004**: System MUST successfully migrate all existing customer accounts to use the central identity model.
- **FR-005**: System MUST remove all legacy local identity data and logic after the transition is verified; legacy data MUST be archived for 90 days prior to permanent deletion.
- **FR-006**: System MUST return the central identity reference in authentication responses during the transition phase.

### Key Entities *(include if feature involves data)*

- **Customer**: Represents the business entity (profile, preferences, history). Now includes a link to the central identity.
- **Central Identity**: An external representation of the user's login identity, managed by a dedicated service.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of active customer records are successfully linked to a valid central identity.
- **SC-002**: New customer registration completes in under 2 seconds (including central identity creation).
- **SC-003**: Zero data loss or account access issues reported during the migration window.
- **SC-004**: Complete removal of legacy identity-management code and data structures from the service.
- **SC-005**: The "Get Customer by Central Identity" endpoint maintains a latency of < 50ms p95.
