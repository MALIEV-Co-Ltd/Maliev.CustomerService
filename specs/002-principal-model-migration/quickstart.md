# Quickstart: Principal-First Migration

## Setup
1. Ensure the IAM service is accessible or mocked.
2. Update `appsettings.json` with `ExternalServices:IAM:BaseUrl`.

## Running the Migration
Existing customers have been migrated to use Principals. The `PrincipalId` column is now **REQUIRED** and **UNIQUE**.

## Registration Flow
New customers automatically receive an IAM Principal ID during the creation process. Local user management has been **REMOVED**.

## Verifying
1. Check the database for `principal_id` values:
   ```sql
   SELECT id, principal_id FROM customers;
   -- All active customers must have a principal_id.
   ```
2. Test the lookup endpoint:
   ```bash
   curl -X GET http://localhost:8080/customer/v1/customers/by-principal/{principalId}
   ```