# Data Model: Principal-First Migration

## Entities

### Customer (Modified)
The primary entity for customer business data.

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Primary Key (Local) |
| **PrincipalId** | **Guid?** | **Foreign Key to IAM (Nullable during migration, NOT NULL after cleanup)** |
| FirstName | string | Customer first name |
| LastName | string | Customer last name |
| Email | string | Customer email |

### IAM Principal (External)
Owned by the IAM service.

| Field | Type | Description |
|-------|------|-------------|
| PrincipalId | Guid | Unique Identifier |
| PrincipalType | string | "user" or "service" |
| LinkedService | string | "CustomerService" |

## State Transitions

### New Customer Registration
1. System receives `CreateCustomerRequest`.
2. System calls `IAM.CreatePrincipal`.
3. System receives `PrincipalId`.
4. System creates `Customer` record with `PrincipalId`.

### Backfill Migration
1. System identifies `Customer` with `PrincipalId == NULL`.
2. System calls `IAM.CreatePrincipal`.
3. System updates `Customer` with `PrincipalId`.