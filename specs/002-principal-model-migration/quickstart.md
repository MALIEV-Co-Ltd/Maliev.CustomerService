# Quickstart: Principal-First Migration

## Developer Environment Setup

1. **IAM Mock**: For local development, ensure you have a mock or local instance of the IAM service running.
2. **Configuration**: Update your `appsettings.Development.json` with the local IAM endpoint:
   ```json
   {
     "ExternalServices": {
       "IAM": {
         "BaseUrl": "http://localhost:8081",
         "ServiceAccountToken": "dev-token"
       }
     }
   }
   ```

## Running the Migration Script (Dev)

To test the backfill on your local database:
```bash
dotnet run --project Maliev.CustomerService.Api --migrate-principals
```

## Verifying the Change

1. Create a customer using the `POST /customer/v1/customers` endpoint.
2. Check the database to see the `PrincipalId` is populated.
3. Call the lookup endpoint:
   ```bash
   curl -X GET http://localhost:8080/customer/v1/customers/by-principal/{principalId}
   ```

## Common Issues

- **IAM Connection Refused**: Verify the IAM service is running and the `BaseUrl` is correct.
- **Unique Constraint Violation**: Ensure you are not trying to register an email that already has a principal linked in IAM but not in CustomerService.
