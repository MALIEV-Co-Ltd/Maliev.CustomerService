# Data Model: Customer Service Microservice

**Date**: 2025-10-08
**Feature**: Customer Service Microservice
**Phase**: 1 - Data Model Design

## Overview

This document defines the data model for the Customer Service microservice, including entity definitions, relationships, validation rules, and database schema design. All entities use PostgreSQL snake_case naming convention and include optimistic concurrency control via RowVersion.

## Entity Relationship Diagram (Conceptual)

```
                         ┌──────────────┐
                         │     User     │ (ASP.NET Core Identity)
                         │              │
                         └──────┬───────┘
                                │ 0..1
                                │ (linked)
                                ▼
┌──────────────┐         ┌──────────────┐
│   Company    │◄────┐   │   Customer   │
│              │     │   │              │
└──────┬───────┘     │   └──────┬───────┘
       │             │          │
       │ 1..n        │ 0..1     │ 1..n
       │             └──────────┘
       │                        │
       │ 1..n                   │ 1..n
       ▼                        ▼
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│   Address    │         │  Document    │         │ Internal     │
│              │         │  Reference   │         │    Note      │
└──────────────┘         └──────┬───────┘         └──────────────┘
                                │                        │
                                │ 0..1                   │ 1..n
                                ▼                        │
                         ┌──────────────┐                │
                         │  NDA Record  │                │
                         │              │                │
                         └──────────────┘                │
                                                         │
          ┌──────────────────────────────────────────────┘
          │  (Customer/Company can have many internal notes)
          │
          ▼
┌──────────────┐
│  AuditLog    │ (tracks all entities, references User)
│              │
└──────────────┘
```

## Core Entities

### 1. User (extends ASP.NET Core IdentityUser)

**Purpose**: Represents a customer user account for authentication and authorization. Built on ASP.NET Core Identity scaffolding with custom extensions. Customer Service owns only customer user accounts (employee accounts managed by separate Employee Service).

**Implementation Approach**:
- Scaffold ASP.NET Core Identity to generate standard tables (AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserTokens, AspNetUserLogins)
- Extend IdentityUser with custom properties for Customer Service requirements
- Use built-in UserManager<ApplicationUser> and SignInManager<ApplicationUser> for user operations
- Leverage Identity's password hashing, lockout policies, and security stamp management

**Custom Fields (extends IdentityUser)**:

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `linked_customer_id` | Guid | Yes | FK to Customer | Reference to Customer record |
| `is_active` | boolean | No | Default true | Account active/disabled flag (in addition to Identity's LockoutEnabled) |
| `last_login_at` | datetime | Yes | Auto-updated on successful validation | Most recent successful authentication timestamp (UTC) for activity tracking |

**Inherited from IdentityUser** (subset of key fields):
- `Id` (string) - Primary key
- `UserName` (string) - Unique username
- `Email` (string) - Unique email
- `PasswordHash` (string) - Hashed password via Identity
- `SecurityStamp` (string) - Security token for invalidation
- `EmailConfirmed` (bool) - Email verification status
- `PhoneNumber` (string) - Optional phone
- `LockoutEnabled` (bool) - Account lockout enabled
- `LockoutEnd` (DateTimeOffset?) - Lockout expiration
- `AccessFailedCount` (int) - Failed login attempts

**Relationships**:
- May link to one `Customer` (optional via `linked_customer_id`)
- Referenced by `AuditLog` entries (via `actor_id`)
- Standard Identity relationships: AspNetRoles (many-to-many), AspNetUserClaims (one-to-many)

**Validation Rules**:
- `UserName` must be unique (enforced by Identity)
- `Email` must be unique and comply with RFC 5322 format (enforced by Identity)
- `PasswordHash` generated using ASP.NET Core Identity's password hasher (PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations)
- Only "Customer" role supported in this service (employee roles managed by Employee Service)
- `linked_customer_id` optional reference to Customer record
- Cannot delete user account while linked Customer exists (integrity check)

**Identity Configuration**:
- Password requirements: minimum 8 characters, require digit, require uppercase, require lowercase, require non-alphanumeric
- Lockout policy: 5 failed attempts → 15 minute lockout
- Email confirmation required for activation

**Indexes** (in addition to Identity defaults):
- Index on `linked_customer_id` for reverse lookups
- Index on `is_active` for active user queries
- Index on `last_login_at` for activity monitoring and inactive account detection

**State Transitions**:
```
Active ──(disable/lockout)──> Inactive
Inactive ──(enable/unlock)──> Active
```

**Security**:
- Password hash generated using ASP.NET Core Identity's `IPasswordHasher<ApplicationUser>`
- Plain text passwords NEVER stored in database
- Password validation endpoint `/customers/v1/validate` uses SignInManager.CheckPasswordSignInAsync()
- Failed login attempts tracked by Identity's AccessFailedCount
- Security stamp invalidates tokens on password change
- Failed login attempts logged for security monitoring (see AuditLog)

---

### 2. Customer

**Purpose**: Represents an individual contact in the system.

**Fields**:

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `id` | Guid | No | Primary Key | Unique identifier (auto-generated) |
| `first_name` | string(100) | No | Required, trimmed | Customer's first name |
| `last_name` | string(100) | No | Required, trimmed | Customer's last name |
| `email` | string(255) | No | Unique, RFC 5322 format | Customer's email address |
| `phone` | string(20) | No | E.164 format | Customer's phone number |
| `segment` | enum(string) | Yes | "Retail", "Wholesale", "Enterprise", "Government" | Business segment classification for pricing and marketing rules |
| `tier` | enum(string) | Yes | "Bronze", "Silver", "Gold", "Platinum", "VIP" | Customer tier for differentiated service levels |
| `preferred_language` | string(2) | Yes | ISO 639-1 codes (e.g., "en", "th", "zh") | Preferred language for communications |
| `timezone` | string(50) | Yes | IANA timezone identifiers (e.g., "Asia/Bangkok") | Customer's timezone for localized delivery |
| `communication_preferences` | JSONB | Yes | Flexible schema | Communication opt-in/opt-out preferences (e.g., email_opt_in, sms_opt_in, marketing_opt_in) |
| `company_id` | Guid | Yes | Foreign Key to Company | Optional company affiliation |
| `is_deleted` | boolean | No | Default false | Soft deletion flag |
| `created_at` | datetime | No | Auto-generated | Record creation timestamp (UTC) |
| `updated_at` | datetime | No | Auto-updated | Last modification timestamp (UTC) |
| `version` | byte[] | No | RowVersion | Optimistic concurrency control |

**Relationships**:
- Belongs to zero or one `Company` (optional `company_id` FK)
- Has many `Address` records (as owner)
- Has many `DocumentReference` records (as owner)
- Has many `NDARecord` records
- Has many `InternalNote` records (as owner)

**Validation Rules**:
- `email` must be unique across all active (non-deleted) customers
- `email` format must comply with RFC 5322
- `phone` format must comply with E.164 international standard
- `first_name` and `last_name` must not be empty after trimming
- `segment` must be one of: "Retail", "Wholesale", "Enterprise", "Government" (nullable)
- `tier` must be one of: "Bronze", "Silver", "Gold", "Platinum", "VIP" (nullable)
- `preferred_language` must be valid ISO 639-1 two-letter code (nullable)
- `timezone` must be valid IANA timezone identifier (nullable)
- `communication_preferences` stored as JSONB without schema validation (nullable)
- Soft deletion: set `is_deleted = true`, do NOT physically delete

**Indexes**:
- Primary key on `id`
- Unique index on `email` where `is_deleted = false`
- Index on `company_id` for company lookups
- Index on `created_at` for sorting
- Index on `segment` for segment-based queries
- Index on `tier` for tier-based queries
- Index on `preferred_language` for localization queries

**State Transitions**: None (simple CRUD entity)

**Business Context**:
- `segment` and `tier` enable downstream services (Quoting Service for pricing, Marketing Service for targeting) to apply business rules
- `preferred_language` and `timezone` support personalized, localized communications
- `communication_preferences` provides flexible opt-in/opt-out tracking for compliance

---

### 3. Company

**Purpose**: Represents a business entity or organization.

**Fields**:

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `id` | Guid | No | Primary Key | Unique identifier (auto-generated) |
| `name` | string(200) | No | Required | Company legal name |
| `vat_number` | string(50) | No | Country-specific format | VAT/tax identification number |
| `registration_number` | string(50) | No | Required | Company registration number |
| `contact_email` | string(255) | No | RFC 5322 format | Company contact email |
| `contact_phone` | string(20) | No | E.164 format | Company contact phone |
| `segment` | enum(string) | Yes | "SMB", "MidMarket", "Enterprise", "Government" | Business segment classification for account management |
| `tier` | enum(string) | Yes | "Standard", "Premium", "Strategic", "Partner" | Company tier for partnership and service levels |
| `created_at` | datetime | No | Auto-generated | Record creation timestamp (UTC) |
| `updated_at` | datetime | No | Auto-updated | Last modification timestamp (UTC) |
| `version` | byte[] | No | RowVersion | Optimistic concurrency control |

**Relationships**:
- Has many `Customer` records (via `company_id` FK)
- Has many `Address` records (as owner)
- Has many `DocumentReference` records (as owner)
- Has many `InternalNote` records (as owner)

**Validation Rules**:
- `vat_number` format validated according to country code prefix (e.g., "TH-1234567890" for Thailand)
- `contact_email` format must comply with RFC 5322
- `contact_phone` format must comply with E.164 standard
- `name`, `registration_number` must not be empty
- `segment` must be one of: "SMB", "MidMarket", "Enterprise", "Government" (nullable)
- `tier` must be one of: "Standard", "Premium", "Strategic", "Partner" (nullable)

**Indexes**:
- Primary key on `id`
- Index on `vat_number` for lookups
- Index on `name` for search
- Index on `segment` for segment-based queries
- Index on `tier` for tier-based queries

**State Transitions**: None (simple CRUD entity)

**Business Context**:
- `segment` and `tier` support B2B relationship management and account-based pricing strategies
- Enable downstream services to apply company-level business rules and differentiated service levels

---

### 4. Address

**Purpose**: Represents a physical address for billing or shipping purposes.

**Fields**:

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `id` | Guid | No | Primary Key | Unique identifier (auto-generated) |
| `owner_type` | enum(string) | No | "Customer" or "Company" | Polymorphic owner type |
| `owner_id` | Guid | No | FK to Customer or Company | Polymorphic owner reference |
| `type` | enum(string) | No | "Billing" or "Shipping" | Address classification |
| `address_line1` | string(200) | No | Required | Primary address line |
| `address_line2` | string(200) | Yes | Optional | Secondary address line (suite, apt, etc.) |
| `city` | string(100) | No | Required | City name |
| `province` | string(100) | Yes | Optional | Province/state name |
| `postal_code` | string(20) | No | Required | Postal/ZIP code |
| `country_id` | Guid | No | FK to Country Service | Country reference (external service) |
| `created_at` | datetime | No | Auto-generated | Record creation timestamp (UTC) |
| `updated_at` | datetime | No | Auto-updated | Last modification timestamp (UTC) |
| `version` | byte[] | No | RowVersion | Optimistic concurrency control |

**Relationships**:
- Belongs to one `Customer` or one `Company` (polymorphic via `owner_type` and `owner_id`)
- References one `Country` (external Country Service via `country_id`)

**Validation Rules**:
- `owner_type` must be "Customer" or "Company"
- `owner_id` must reference existing Customer or Company based on `owner_type`
- `type` must be "Billing" or "Shipping"
- `country_id` must be validated with Country Service before create/update
- Multiple addresses of the same type allowed per owner
- `address_line1`, `city`, `postal_code` must not be empty

**Indexes**:
- Primary key on `id`
- Composite index on `(owner_type, owner_id)` for owner lookups
- Index on `country_id` for country lookups

**State Transitions**: None (simple CRUD entity)

**External Dependency**:
- Country Service validation endpoint for `country_id`
- On unavailability: return HTTP 503 Service Unavailable

---

### 5. DocumentReference

**Purpose**: Represents metadata for a document stored in the Upload Service.

**Fields**:

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `id` | Guid | No | Primary Key | Unique identifier (auto-generated) |
| `owner_type` | enum(string) | No | "Customer" or "Company" | Polymorphic owner type |
| `owner_id` | Guid | No | FK to Customer or Company | Polymorphic owner reference |
| `document_type` | string(100) | No | Free-form | Document classification (e.g., "NDA", "Company Registration") |
| `file_reference` | string(255) | No | Upload Service ID | External file identifier from Upload Service |
| `filename` | string(255) | No | Required | Original filename |
| `status` | enum(string) | No | "Pending", "Complete", "PendingDeletion", "Orphaned", "MissingFile" | Document lifecycle status |
| `version` | int | No | Default 1, auto-increment on update | Document version number |
| `signed_by` | string(100) | Yes | Optional | Name of signatory (if applicable) |
| `signed_at` | datetime | Yes | Optional | Signing timestamp (UTC) |
| `created_at` | datetime | No | Auto-generated | Record creation timestamp (UTC) |
| `updated_at` | datetime | No | Auto-updated | Last modification timestamp (UTC) |
| `row_version` | byte[] | No | RowVersion | Optimistic concurrency control (renamed to avoid conflict with version) |

**Relationships**:
- Belongs to one `Customer` or one `Company` (polymorphic via `owner_type` and `owner_id`)
- May be referenced by zero or one `NDARecord`
- References one file in Upload Service (external via `file_reference`)

**Validation Rules**:
- `owner_type` must be "Customer" or "Company"
- `owner_id` must reference existing Customer or Company based on `owner_type`
- `file_reference` must be validated with Upload Service before create
- `status` lifecycle: Pending → Complete, Complete → PendingDeletion, any → Orphaned/MissingFile
- `version` increments on file_reference update
- `signed_by` and `signed_at` must both be present or both null

**Indexes**:
- Primary key on `id`
- Composite index on `(owner_type, owner_id)` for owner lookups
- Index on `document_type` for filtering
- Index on `status` for status queries

**State Transitions**:
```
Pending ──(complete)──> Complete
Complete ──(delete)──> PendingDeletion
PendingDeletion ──(retry success)──> [DELETED]
PendingDeletion ──(retry fail)──> [remains, retries later]
any ──(validation fail)──> Orphaned or MissingFile
```

**External Dependency**:
- Upload Service validation endpoint for `file_reference`
- Upload Service deletion endpoint for physical file removal
- On delete failure: transition to PendingDeletion, retry asynchronously

---

### 6. NDARecord

**Purpose**: Represents a non-disclosure agreement with lifecycle tracking.

**Fields**:

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `id` | Guid | No | Primary Key | Unique identifier (auto-generated) |
| `customer_id` | Guid | No | Foreign Key to Customer | Customer associated with NDA |
| `document_reference_id` | Guid | Yes | Foreign Key to DocumentReference | Optional document reference (required before signing) |
| `status` | enum(string) | No | "Draft", "Signed", "Expired", "Revoked" | NDA lifecycle status |
| `signed_by` | string(100) | Yes | Required when status=Signed | Name of signatory |
| `signed_at` | datetime | Yes | Required when status=Signed | Signing timestamp (UTC) |
| `revoked_at` | datetime | Yes | Required when status=Revoked | Revocation timestamp (UTC) |
| `expires_at` | datetime | No | Required | NDA expiration date |
| `created_at` | datetime | No | Auto-generated | Record creation timestamp (UTC) |
| `updated_at` | datetime | No | Auto-updated | Last modification timestamp (UTC) |
| `version` | byte[] | No | RowVersion | Optimistic concurrency control |

**Relationships**:
- Belongs to one `Customer` (via `customer_id` FK)
- References zero or one `DocumentReference` (via `document_reference_id` FK, optional until signing)

**Validation Rules**:
- Cannot transition to "Signed" status without `document_reference_id`
- `signed_by` and `signed_at` required when status = "Signed"
- `revoked_at` required when status = "Revoked"
- `expires_at` must be future date on creation
- Terminal states: "Expired" and "Revoked" cannot be modified further

**Indexes**:
- Primary key on `id`
- Index on `customer_id` for customer lookups
- Index on `status` for status queries
- Index on `expires_at` for expiration checks

**State Transitions**:
```
Draft ──(sign with document)──> Signed
Draft ──(expire)──> Expired
Signed ──(revoke)──> Revoked
Signed ──(expire)──> Expired
[Terminal states: Expired, Revoked - no further transitions]
```

**Validation**:
- Signing requires valid `document_reference_id`
- Automatic expiration check: transition to "Expired" when `current_date > expires_at`
- Background job or query-time check for expiration

---

### 7. InternalNote

**Purpose**: Represents internal notes or comments for employee use only, storing confidential observations and information about customers or companies that should never be visible to customers.

**Fields**:

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `id` | Guid | No | Primary Key | Unique identifier (auto-generated) |
| `owner_type` | enum(string) | No | "Customer" or "Company" | Polymorphic owner type |
| `owner_id` | Guid | No | FK to Customer or Company | Polymorphic owner reference |
| `note_text` | text | No | Required, max 5000 chars | Internal note content |
| `created_by` | string(255) | No | Required | Employee identifier who created the note |
| `created_at` | datetime | No | Auto-generated | Record creation timestamp (UTC) |
| `updated_at` | datetime | No | Auto-updated | Last modification timestamp (UTC) |
| `version` | byte[] | No | RowVersion | Optimistic concurrency control |

**Relationships**:
- Belongs to one `Customer` or one `Company` (polymorphic via `owner_type` and `owner_id`)

**Validation Rules**:
- `owner_type` must be "Customer" or "Company"
- `owner_id` must reference existing Customer or Company based on `owner_type`
- `note_text` required, maximum 5000 characters
- `created_by` stores employee identifier from JWT claims
- Multiple notes allowed per owner (history of internal observations)

**Indexes**:
- Primary key on `id`
- Composite index on `(owner_type, owner_id)` for owner lookups
- Index on `created_at` for chronological ordering
- Index on `created_by` for employee activity tracking

**State Transitions**: None (simple CRUD entity)

**Authorization**:
- **Create/Read/Update/Delete**: Only accessible by Employee, Manager, or Admin roles
- **Forbidden**: Customer role users cannot access internal notes via any endpoint
- **Self-Service Exclusion**: Customer profile retrieval endpoints exclude internal notes from response

**Security**:
- All internal note endpoints protected by `[Authorize(Policy = "EmployeeOrHigher")]`
- Customer self-service endpoints (`GET /customers/{id}` when called by customer) filter out internal notes
- Audit log records all internal note creation, updates, and deletions with employee identifier

---

### 8. AuditLog

**Purpose**: Represents a historical record of system changes for compliance and debugging.

**Fields**:

| Field | Type | Nullable | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `id` | Guid | No | Primary Key | Unique identifier (auto-generated) |
| `actor_id` | Guid | No | Required | User identifier performing action (references User.id) |
| `actor_type` | enum(string) | No | "Customer", "Employee", "System" | Actor classification for audit purposes |
| `action` | enum(string) | No | "Create", "Update", "Delete", "Cancel", "Approve" | Action type |
| `entity_type` | string(100) | No | Required | Type of entity affected (e.g., "Customer", "NDARecord") |
| `entity_id` | string(100) | No | Required | Identifier of affected entity |
| `timestamp` | datetime | No | Auto-generated | Action timestamp (UTC) |
| `changed_fields` | JSON | Yes | Optional | JSON array of changed field names |
| `previous_values` | JSON | Yes | Optional | JSON object with previous values (for updates) |

**Relationships**:
- References one `User` (via `actor_id` FK, soft reference for performance)
- Conceptually linked to all entities via `entity_type` and `entity_id` but not enforced via foreign keys
- Denormalized for performance and tamper-proofing

**Validation Rules**:
- `actor_id` references User.id from authenticated user context (for customer users) or user ID from Employee Service (for employee users)
- `actor_type` derived from user context (Customer role → Customer, employee users → Employee, system operations → System)
- `action` must be valid enum value
- `changed_fields` and `previous_values` required for Update actions
- Audit logs are immutable (insert-only, no updates or deletes)

**Indexes**:
- Primary key on `id`
- Composite index on `(entity_type, entity_id)` for entity history
- Index on `actor_id` for actor queries
- Index on `timestamp` for time-range queries
- Index on `actor_type` for compliance reports (Customer vs Employee)

**State Transitions**: None (immutable log records)

**Security**:
- Sensitive data (passwords, tokens) never logged
- Full connection strings never logged (only sanitized versions)

---

## Common Patterns

### Optimistic Concurrency Control

All entities (except AuditLog) include a `version` byte array for PostgreSQL row versioning:

**EF Core FluentAPI Configuration**:
```csharp
builder.Property(e => e.Version)
    .HasColumnName("version")
    .IsRowVersion()
    .HasDefaultValueSql("'\\x0000000000000000'::bytea")
    .ValueGeneratedOnAddOrUpdate()
    .IsRequired();
```

**Concurrency Conflict Handling**:
- Version passed as Base64 string in update requests
- Validated before attempting update
- Returns HTTP 409 Conflict on version mismatch
- Client must retrieve latest version and retry

### Soft Deletion

Customer entity uses soft deletion pattern:
- `is_deleted` flag set to true instead of physical delete
- Excluded from standard queries by default (WHERE is_deleted = false)
- Preserves all relationships (addresses, documents, NDAs)
- Can be restored by setting `is_deleted = false`

### Polymorphic Relationships

Address and DocumentReference use polymorphic ownership:
- `owner_type` (enum: "Customer" or "Company")
- `owner_id` (Guid referencing Customer or Company)
- Validated via application logic (no database FK constraint)
- Indexed for efficient owner lookups

### Timestamps

All entities include:
- `created_at`: Auto-generated on insert (UTC)
- `updated_at`: Auto-updated on any modification (UTC)
- Stored as `datetime` in PostgreSQL

### Snake_Case Naming Convention

All database objects use snake_case:
- Tables: `customer`, `company`, `address`, `document_reference`, `nda_record`, `audit_log`
- Columns: `first_name`, `owner_type`, `signed_at`, etc.
- Configured via EF Core `HasColumnName()` or global naming convention

---

## Database Schema (PostgreSQL)

### ASP.NET Core Identity Tables (Generated by Scaffold)

The following tables are automatically generated when scaffolding ASP.NET Core Identity:

- **AspNetUsers** - Core user authentication data (Id, UserName, Email, PasswordHash, SecurityStamp, etc.)
- **AspNetRoles** - Role definitions
- **AspNetUserRoles** - Many-to-many user-role assignments
- **AspNetUserClaims** - Custom claims per user
- **AspNetUserLogins** - External login providers (OAuth, etc.)
- **AspNetUserTokens** - Authentication tokens
- **AspNetRoleClaims** - Claims associated with roles

**Custom Extension to AspNetUsers**:
```sql
-- Migration to add custom fields to AspNetUsers table
ALTER TABLE "AspNetUsers" ADD COLUMN linked_customer_id UUID REFERENCES customer(id);
ALTER TABLE "AspNetUsers" ADD COLUMN is_active BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE "AspNetUsers" ADD COLUMN last_login_at TIMESTAMPTZ;

CREATE INDEX idx_aspnetusers_linked_customer ON "AspNetUsers"(linked_customer_id);
CREATE INDEX idx_aspnetusers_active ON "AspNetUsers"(is_active);
CREATE INDEX idx_aspnetusers_last_login ON "AspNetUsers"(last_login_at);
```

**Note**: ASP.NET Core Identity uses string-based primary keys by default. The ApplicationUser class extends IdentityUser and EF Core migration adds custom columns to AspNetUsers table.

### Table: `customer`

```sql
CREATE TABLE customer (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    segment VARCHAR(20) CHECK (segment IN ('Retail', 'Wholesale', 'Enterprise', 'Government')),
    tier VARCHAR(20) CHECK (tier IN ('Bronze', 'Silver', 'Gold', 'Platinum', 'VIP')),
    preferred_language VARCHAR(2),
    timezone VARCHAR(50),
    communication_preferences JSONB,
    company_id UUID REFERENCES company(id),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version BYTEA NOT NULL DEFAULT '\\x0000000000000000'::bytea
);

CREATE UNIQUE INDEX idx_customer_email_active ON customer(email) WHERE is_deleted = false;
CREATE INDEX idx_customer_company ON customer(company_id);
CREATE INDEX idx_customer_created ON customer(created_at);
CREATE INDEX idx_customer_segment ON customer(segment);
CREATE INDEX idx_customer_tier ON customer(tier);
CREATE INDEX idx_customer_language ON customer(preferred_language);
```

### Table: `company`

```sql
CREATE TABLE company (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    vat_number VARCHAR(50) NOT NULL,
    registration_number VARCHAR(50) NOT NULL,
    contact_email VARCHAR(255) NOT NULL,
    contact_phone VARCHAR(20) NOT NULL,
    segment VARCHAR(20) CHECK (segment IN ('SMB', 'MidMarket', 'Enterprise', 'Government')),
    tier VARCHAR(20) CHECK (tier IN ('Standard', 'Premium', 'Strategic', 'Partner')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version BYTEA NOT NULL DEFAULT '\\x0000000000000000'::bytea
);

CREATE INDEX idx_company_vat ON company(vat_number);
CREATE INDEX idx_company_name ON company(name);
CREATE INDEX idx_company_segment ON company(segment);
CREATE INDEX idx_company_tier ON company(tier);
```

### Table: `address`

```sql
CREATE TABLE address (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_type VARCHAR(20) NOT NULL CHECK (owner_type IN ('Customer', 'Company')),
    owner_id UUID NOT NULL,
    type VARCHAR(20) NOT NULL CHECK (type IN ('Billing', 'Shipping')),
    address_line1 VARCHAR(200) NOT NULL,
    address_line2 VARCHAR(200),
    city VARCHAR(100) NOT NULL,
    province VARCHAR(100),
    postal_code VARCHAR(20) NOT NULL,
    country_id UUID NOT NULL, -- External reference to Country Service
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version BYTEA NOT NULL DEFAULT '\\x0000000000000000'::bytea
);

CREATE INDEX idx_address_owner ON address(owner_type, owner_id);
CREATE INDEX idx_address_country ON address(country_id);
```

### Table: `document_reference`

```sql
CREATE TABLE document_reference (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_type VARCHAR(20) NOT NULL CHECK (owner_type IN ('Customer', 'Company')),
    owner_id UUID NOT NULL,
    document_type VARCHAR(100) NOT NULL,
    file_reference VARCHAR(255) NOT NULL, -- External reference to Upload Service
    filename VARCHAR(255) NOT NULL,
    status VARCHAR(20) NOT NULL CHECK (status IN ('Pending', 'Complete', 'PendingDeletion', 'Orphaned', 'MissingFile')),
    version INT NOT NULL DEFAULT 1,
    signed_by VARCHAR(100),
    signed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    row_version BYTEA NOT NULL DEFAULT '\\x0000000000000000'::bytea
);

CREATE INDEX idx_document_owner ON document_reference(owner_type, owner_id);
CREATE INDEX idx_document_type ON document_reference(document_type);
CREATE INDEX idx_document_status ON document_reference(status);
```

### Table: `nda_record`

```sql
CREATE TABLE nda_record (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customer(id),
    document_reference_id UUID REFERENCES document_reference(id),
    status VARCHAR(20) NOT NULL CHECK (status IN ('Draft', 'Signed', 'Expired', 'Revoked')),
    signed_by VARCHAR(100),
    signed_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version BYTEA NOT NULL DEFAULT '\\x0000000000000000'::bytea
);

CREATE INDEX idx_nda_customer ON nda_record(customer_id);
CREATE INDEX idx_nda_status ON nda_record(status);
CREATE INDEX idx_nda_expires ON nda_record(expires_at);
```

### Table: `internal_note`

```sql
CREATE TABLE internal_note (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_type VARCHAR(20) NOT NULL CHECK (owner_type IN ('Customer', 'Company')),
    owner_id UUID NOT NULL,
    note_text TEXT NOT NULL,
    created_by VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version BYTEA NOT NULL DEFAULT '\\x0000000000000000'::bytea
);

CREATE INDEX idx_internal_note_owner ON internal_note(owner_type, owner_id);
CREATE INDEX idx_internal_note_created_at ON internal_note(created_at);
CREATE INDEX idx_internal_note_created_by ON internal_note(created_by);
```

### Table: `audit_log`

```sql
CREATE TABLE audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_id UUID NOT NULL,
    actor_type VARCHAR(20) NOT NULL CHECK (actor_type IN ('Customer', 'Employee', 'System')),
    action VARCHAR(20) NOT NULL CHECK (action IN ('Create', 'Update', 'Delete', 'Cancel', 'Approve')),
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    changed_fields JSONB,
    previous_values JSONB
);

CREATE INDEX idx_audit_entity ON audit_log(entity_type, entity_id);
CREATE INDEX idx_audit_actor ON audit_log(actor_id);
CREATE INDEX idx_audit_timestamp ON audit_log(timestamp);
CREATE INDEX idx_audit_actor_type ON audit_log(actor_type);
```

---

## Migration Strategy

1. **Scaffold Identity**: Run `dotnet aspnet-codegenerator identity` to generate ASP.NET Core Identity infrastructure
2. **Identity Tables Migration**: EF Core generates migration for AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims
3. **Custom Extensions Migration**: Create migration to add linked_customer_id and is_active to AspNetUsers table
4. **Business Entities Migration**: Create migration for Customer, Company, Address, DocumentReference, NDARecord, AuditLog tables
5. **Seed Data**: Create "Customer" role in AspNetRoles (required for RBAC)
6. **Future Migrations**: All schema changes versioned via EF Core migrations
7. **Manual Application**: Migrations applied manually via `dotnet ef database update` (NOT auto-applied on startup)

## External Service Dependencies

### Country Service
- **Purpose**: Validate `country_id` in Address entity
- **Endpoint**: `/countries/{id}` (GET request)
- **Validation**: Before Address create/update
- **Failure Handling**: Return HTTP 503 if Country Service unavailable

### Upload Service
- **Purpose**: Validate and manage file references in DocumentReference
- **Endpoints**:
  - `/uploads/v1/{id}` (GET) - Validate file exists
  - `/uploads/v1/{id}` (DELETE) - Remove physical file
- **Validation**: Before DocumentReference create
- **Failure Handling**: Deferred deletion with retry on delete failures

---

## Performance Considerations

- **Indexes**: All foreign keys, frequently queried fields, and polymorphic owner lookups indexed
- **Connection Pooling**: Enabled by default in Npgsql
- **DTO Projection**: Query only required fields to minimize data transfer
- **Async Operations**: All database operations use async/await for non-blocking I/O
- **Caching**: External service data cached (24-hour TTL) to reduce external calls

---

## Security & Compliance

- **Row-Level Security**: Not implemented (application-level authorization sufficient)
- **Audit Logs**: Immutable, tamper-proof records for compliance
- **Actor Type Tracking**: Customer vs Employee modifications clearly distinguished
- **Sensitive Data**: Passwords and tokens never stored in database
- **Connection Strings**: Loaded from environment variables (Google Secret Manager)

---

## Summary

Data model supports all functional requirements with:
- **ASP.NET Core Identity scaffolding** as foundation for user authentication (AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims tables)
- **7 custom business entities** (Customer, Company, Address, DocumentReference, NDARecord, InternalNote, AuditLog)
- **ApplicationUser extends IdentityUser** with linked_customer_id for Customer relationship
- Optimistic concurrency control via RowVersion on all mutable entities
- Polymorphic relationships for flexible Address and DocumentReference ownership
- Built-in password hashing, lockout policies, security stamp management from Identity framework
- External service integrations (Upload, Country) with validation and failure handling
- Comprehensive audit trail with actor type classification
- Performance optimizations (indexes, async, caching)
- PostgreSQL 18 with snake_case naming convention for custom tables
