# Data Model: Principal-First Migration

## Entities

### Customer (Updated)
Represents the business data for a customer.

| Field | Type | Constraint | Description |
|-------|------|------------|-------------|
| Id | Guid | PK | Primary key |
| **PrincipalId** | **Guid** | **Unique, Indexed** | Link to the global IAM Principal |
| FirstName | string | Required | |
| LastName | string | Required | |
| Email | string | Required, Unique | |
| ... | ... | ... | Legacy fields remain |

### ApplicationUser (Deprecated)
The legacy ASP.NET Identity entity to be removed after successful migration.

## Relationships

- **Customer -> IAM Principal**: 1:1 relationship via `PrincipalId`. The `PrincipalId` is owned by the IAM service.
- **Customer -> ApplicationUser**: Legacy 1:1 relationship to be replaced by the Principal link.

## Validation Rules

- `PrincipalId` MUST NOT be null for active customers (post-cleanup).
- `PrincipalId` MUST be unique across all active customers.
- During registration, a `PrincipalId` MUST be obtained from the IAM service before the customer record is persisted.

## State Transitions

1. **New Registration**:
   - `Request` -> `IAM Call` -> `Get PrincipalId` -> `Create Customer`
2. **Migration**:
   - `Existing Customer` -> `Check Identity` -> `Create Principal in IAM` -> `Link PrincipalId to Customer`
3. **Cleanup**:
   - `Verify All Linked` -> `Drop Identity Tables` -> `Set PrincipalId NOT NULL`
