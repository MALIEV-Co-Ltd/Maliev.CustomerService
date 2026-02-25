# Quickstart: Company Tier System

## Build & Run

```bash
# Build the solution
dotnet build Maliev.CustomerService.slnx

# Run tests
dotnet test

# Run with specific project
dotnet run --project Maliev.CustomerService.Api/Maliev.CustomerService.Api.csproj
```

## Key Endpoints

### Get Company with Tier Info
```bash
GET /customer/v1/companies/{id}
Authorization: Bearer <token> (requires customer.companies.read)
```

### Manual Tier Recalculation
```bash
POST /customer/v1/companies/{id}/calculate-tier
Authorization: Bearer <token> (requires customer.companies.manage)
```

### List Tier Settings
```bash
GET /customer/v1/tier-settings
Authorization: Bearer <token> (requires customer.tiers.read)
```

### Create/Update Tier Settings
```bash
POST /customer/v1/tier-settings
PUT /customer/v1/tier-settings/{id}
Authorization: Bearer <token> (requires customer.tiers.manage)
```

### Company Documents
```bash
GET /customer/v1/companies/{companyId}/documents
POST /customer/v1/companies/{companyId}/documents
DELETE /customer/v1/companies/{companyId}/documents/{id}
Authorization: Bearer <token> (requires customer.companies.read/write)
```

## Testing

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~TierCalculationServiceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Configuration

Tier thresholds are configured via the Tier Settings API (database-driven), not appsettings.json.

Default values:
- **Silver**: MinPurchaseValue=100,000 THB, MinOrderCount=10
- **Gold**: MinPurchaseValue=500,000 THB, MinOrderCount=50
