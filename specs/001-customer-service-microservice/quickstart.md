# Quickstart Guide: Customer Service Microservice

**Date**: 2025-10-08
**Feature**: Customer Service Microservice
**Target Audience**: Developers setting up local development environment

## Overview

This guide helps you set up and run the Customer Service microservice locally for development. The service manages customer and company master data, user account management for customers, addresses, documents, and NDA lifecycle.

## Prerequisites

### Required Software

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **PostgreSQL 18** (via Docker) or local installation
- **Git** for version control
- **Visual Studio 2022**, **VS Code**, or **Rider** (recommended IDEs)

### Optional Tools

- **Postman** or **curl** for API testing
- **pgAdmin** or **DBeaver** for database management
- **k9s** or **kubectl** for Kubernetes management (for deployment)

---

## Quick Start (5 Minutes)

### 1. Clone Repository

```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.CustomerService.git
cd Maliev.CustomerService
```

### 2. Start PostgreSQL (Docker)

```bash
docker run --name customer-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=customer_app_db \
  -p 5432:5432 \
  -d postgres:18
```

### 3. Set Connection String

**Windows (PowerShell)**:
```powershell
$env:ConnectionStrings__CustomerDbContext="Server=localhost;Port=5432;Database=customer_app_db;User Id=postgres;Password=postgres;"
```

**Linux/macOS (Bash)**:
```bash
export ConnectionStrings__CustomerDbContext="Server=localhost;Port=5432;Database=customer_app_db;User Id=postgres;Password=postgres;"
```

### 4. Apply Migrations

```bash
dotnet ef database update --project Maliev.CustomerService.Data --startup-project Maliev.CustomerService.Api
```

### 5. Run the Service

```bash
dotnet run --project Maliev.CustomerService.Api
```

### 6. Verify Health

Open browser to: **http://localhost:8080/customers/liveness**

Expected response: `Healthy`

### 7. Access Swagger UI

Open browser to: **http://localhost:8080/customers/swagger**

You can now explore and test all API endpoints interactively.

---

## Detailed Setup

### Database Setup

#### Option A: Docker (Recommended for Local Development)

```bash
# Create docker-compose.yml for local development
docker-compose -f docker-compose.test.yml up -d postgres
```

**docker-compose.test.yml**:
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:18
    container_name: customer-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: customer_app_db
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data

volumes:
  postgres-data:
```

#### Option B: Local PostgreSQL Installation

1. Install PostgreSQL 18 from [postgresql.org](https://www.postgresql.org/download/)
2. Create database:
   ```sql
   CREATE DATABASE customer_app_db;
   ```

### Configuration

#### appsettings.Development.json

The service uses `appsettings.Development.json` for local development with localhost defaults:

```json
{
  "ConnectionStrings": {
    "CustomerDbContext": "Server=localhost;Port=5432;Database=customer_app_db;User Id=postgres;Password=postgres;"
  },
  "Jwt": {
    "Issuer": "maliev-dev",
    "Audience": "maliev-dev",
    "SecurityKey": "dev-secret-key-minimum-32-characters-long-for-local"
  },
  "ExternalServices": {
    "UploadService": {
      "BaseUrl": "http://localhost:8081",
      "TimeoutSeconds": 180
    },
    "CountryService": {
      "BaseUrl": "http://localhost:8082",
      "TimeoutSeconds": 180
    }
  }
}
```

**IMPORTANT**: Never commit real secrets to source control. Use environment variables for production.

#### Environment Variables (Production Pattern)

For staging/production, secrets are loaded from Google Secret Manager at `/mnt/secrets`:

```bash
# Example environment variable names (double underscore converts to colon in IConfiguration)
ConnectionStrings__CustomerDbContext="Server=prod-db;Port=5432;Database=customer_app_db;..."
Jwt__SecurityKey="production-key-from-secret-manager"
ExternalServices__UploadService__BaseUrl="https://api.maliev.com/uploads/v1"
```

### Running Migrations

#### 1. Scaffold ASP.NET Core Identity (First Time Setup)

```bash
# Install code generator tool (if not already installed)
dotnet tool install -g dotnet-aspnet-codegenerator

# Scaffold Identity with default UI
dotnet aspnet-codegenerator identity \
  --project Maliev.CustomerService.Api \
  --dbContext CustomerDbContext \
  --files "Account.Register;Account.Login;Account.Logout"
```

This generates:
- ApplicationUser class extending IdentityUser
- Identity DbContext configuration
- Default Identity pages (Register, Login, Logout)
- Initial migration for Identity tables

#### 2. Create Custom Extensions Migration

```bash
# Add migration for custom fields (linked_customer_id, is_active)
dotnet ef migrations add AddCustomUserFields \
  --project Maliev.CustomerService.Data \
  --startup-project Maliev.CustomerService.Api \
  --output-dir Migrations
```

#### 3. Create Business Entities Migration

```bash
# Add migration for Customer, Company, Address, DocumentReference, NDARecord, AuditLog
dotnet ef migrations add CreateBusinessEntities \
  --project Maliev.CustomerService.Data \
  --startup-project Maliev.CustomerService.Api \
  --output-dir Migrations
```

#### 4. Add Business Intelligence and Localization Fields

```bash
# Add migration for customer/company segmentation, localization preferences, and activity tracking
dotnet ef migrations add AddSegmentationAndLocalization \
  --project Maliev.CustomerService.Data \
  --startup-project Maliev.CustomerService.Api \
  --output-dir Migrations
```

**This migration adds**:
- Customer: `segment`, `tier`, `preferred_language`, `timezone`, `communication_preferences` (JSONB)
- Company: `segment`, `tier`
- AspNetUsers: `last_login_at`
- Indexes on segment, tier, preferred_language, last_login_at for efficient queries

#### 5. Apply All Migrations

```bash
dotnet ef database update \
  --project Maliev.CustomerService.Data \
  --startup-project Maliev.CustomerService.Api
```

#### Rollback Migration

```bash
dotnet ef database update PreviousMigrationName \
  --project Maliev.CustomerService.Data \
  --startup-project Maliev.CustomerService.Api
```

### Build and Run

#### Build Solution

```bash
dotnet build Maliev.CustomerService.sln
```

#### Run Tests

```bash
# Run all tests
dotnet test Maliev.CustomerService.sln --verbosity normal

# Run with coverage (requires coverlet.msbuild package)
dotnet test Maliev.CustomerService.sln /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

#### Run Service

```bash
# Development mode (auto-reload on file changes)
dotnet watch run --project Maliev.CustomerService.Api

# Production mode
dotnet run --project Maliev.CustomerService.Api --configuration Release
```

---

## Testing the API

### Using Swagger UI

1. Navigate to **http://localhost:8080/customers/swagger**
2. Click "Authorize" button
3. Enter JWT token (see "Authentication" section below)
4. Test endpoints interactively

### Using curl

#### Create Customer User Account

```bash
curl -X POST http://localhost:8080/customers/v1/users \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "username": "john.doe",
    "email": "john.doe@example.com",
    "password": "SecureP@ssw0rd",
    "roles": ["Customer"]
  }'
```

#### Validate Customer Credentials

```bash
curl -X POST http://localhost:8080/customers/v1/validate \
  -H "Content-Type: application/json" \
  -d '{
    "username": "john.doe",
    "password": "SecureP@ssw0rd"
  }'
```

#### Create Customer

```bash
curl -X POST http://localhost:8080/customers/v1/customers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "+66812345678",
    "segment": "Retail",
    "tier": "Bronze",
    "preferredLanguage": "en",
    "timezone": "Asia/Bangkok",
    "communicationPreferences": {
      "email_opt_in": true,
      "sms_opt_in": false,
      "marketing_opt_in": true
    }
  }'
```

#### Get Customer by ID

```bash
curl -X GET http://localhost:8080/customers/v1/customers/{id} \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### List Customers (Paginated)

```bash
curl -X GET "http://localhost:8080/customers/v1/customers?page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Using Postman

1. Import OpenAPI specification from `specs/001-customer-service-microservice/contracts/openapi.yaml`
2. Set environment variable `baseUrl` to `http://localhost:8080/customers/v1`
3. Set environment variable `token` to your JWT token
4. Use `{{baseUrl}}` and `{{token}}` in requests

---

## Authentication

### Development JWT Token

For local development, use the TestAuthHandler in Testing environment or generate a development JWT:

**Testing Environment** (automatically uses TestAuthHandler with Admin claims):
```bash
$env:ASPNETCORE_ENVIRONMENT="Testing"
dotnet run --project Maliev.CustomerService.Api
```

**Development JWT** (manual generation using jwt.io):

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "550e8400-e29b-41d4-a716-446655440000",
    "name": "John Doe",
    "email": "john.doe@example.com",
    "role": "Customer",
    "userType": "customer",
    "iat": 1696780800,
    "exp": 1696867200
  }
}
```

**Secret for signing**: `dev-secret-key-minimum-32-characters-long-for-local`

Use [jwt.io](https://jwt.io) to generate the token.

### Authorization Policies

The service enforces the following authorization policies:

- **Customer**: Requires `Customer` role
- **Employee**: Requires `Employee` role (validated by Employee Service)
- **Manager**: Requires `Manager` role (validated by Employee Service)
- **Admin**: Requires `Admin` role (validated by Employee Service)
- **EmployeeOrHigher**: Requires `Employee`, `Manager`, or `Admin` role

---

## External Service Dependencies

The Customer Service depends on two external services:

### 1. Upload Service

- **Purpose**: File storage and management for document references
- **Endpoint**: `/uploads/v1`
- **Required for**: Document creation and deletion
- **Local Mock**: Run mock server on `http://localhost:8081` or use WireMock

### 2. Country Service

- **Purpose**: Country master data validation
- **Endpoint**: `/countries`
- **Required for**: Address creation and updates
- **Local Mock**: Run mock server on `http://localhost:8082` or use WireMock

### Mock External Services (WireMock Example)

```bash
# Install WireMock
docker run -d -p 8081:8080 \
  -v $(pwd)/wiremock/upload-service:/home/wiremock \
  wiremock/wiremock:latest

docker run -d -p 8082:8080 \
  -v $(pwd)/wiremock/country-service:/home/wiremock \
  wiremock/wiremock:latest
```

**Upload Service Mock** (`wiremock/upload-service/mappings/validate-file.json`):
```json
{
  "request": {
    "method": "GET",
    "urlPattern": "/uploads/v1/.*"
  },
  "response": {
    "status": 200,
    "jsonBody": {
      "id": "mock-file-id",
      "filename": "test.pdf",
      "status": "Complete"
    }
  }
}
```

**Country Service Mock** (`wiremock/country-service/mappings/validate-country.json`):
```json
{
  "request": {
    "method": "GET",
    "urlPattern": "/countries/.*"
  },
  "response": {
    "status": 200,
    "jsonBody": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Thailand",
      "code": "TH"
    }
  }
}
```

---

## Docker Development

### Build Docker Image

```bash
docker build -t maliev-customer-service:dev .
```

### Run Docker Container

```bash
docker run -d \
  --name customer-service \
  -p 8080:8080 \
  -e ConnectionStrings__CustomerDbContext="Server=host.docker.internal;Port=5432;Database=customer_app_db;User Id=postgres;Password=postgres;" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  maliev-customer-service:dev
```

### Docker Compose (Full Stack)

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:18
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: customer_app_db
    ports:
      - "5432:5432"

  customer-service:
    build: .
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__CustomerDbContext: "Server=postgres;Port=5432;Database=customer_app_db;User Id=postgres;Password=postgres;"
      ASPNETCORE_ENVIRONMENT: Development
    depends_on:
      - postgres
```

Run:
```bash
docker-compose up -d
```

---

## Troubleshooting

### Issue: Database Connection Failed

**Symptom**: `Npgsql.NpgsqlException: Failed to connect to localhost:5432`

**Solution**:
1. Verify PostgreSQL is running: `docker ps` or `pg_isready -h localhost -p 5432`
2. Check connection string in environment variables
3. Ensure port 5432 is not blocked by firewall

### Issue: Migration Failed

**Symptom**: `Build failed` when running `dotnet ef database update`

**Solution**:
1. Ensure connection string is set correctly
2. Build the solution first: `dotnet build Maliev.CustomerService.sln`
3. Check PostgreSQL permissions for user

### Issue: External Service Unavailable

**Symptom**: `HTTP 503 Service Unavailable` when creating addresses or documents

**Solution**:
1. Verify Upload Service and Country Service are running
2. Check `appsettings.Development.json` for correct URLs
3. Use mock services (WireMock) for local development

### Issue: JWT Authentication Failed

**Symptom**: `HTTP 401 Unauthorized` on all endpoints

**Solution**:
1. Verify JWT token is valid (use jwt.io)
2. Check `Jwt:SecurityKey` matches between token generation and appsettings
3. Use Testing environment for automatic TestAuthHandler: `$env:ASPNETCORE_ENVIRONMENT="Testing"`

### Issue: Zero Warnings Policy Failed

**Symptom**: Build fails with warnings treated as errors

**Solution**:
1. Fix all warnings in the code
2. Verify `TreatWarningsAsErrors` is set to `true` in `.csproj`
3. Run `dotnet build` to see all warnings
4. Never suppress warnings with `#pragma` - fix the root cause

---

## IDE Setup

### Visual Studio 2022

1. Open `Maliev.CustomerService.sln`
2. Set `Maliev.CustomerService.Api` as startup project
3. Configure environment variables in Project Properties → Debug → Environment Variables
4. Press F5 to run

### VS Code

1. Install C# extension
2. Open folder containing solution
3. Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/Maliev.CustomerService.Api/bin/Debug/net9.0/Maliev.CustomerService.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/Maliev.CustomerService.Api",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ConnectionStrings__CustomerDbContext": "Server=localhost;Port=5432;Database=customer_app_db;User Id=postgres;Password=postgres;"
      }
    }
  ]
}
```

4. Press F5 to run

### JetBrains Rider

1. Open `Maliev.CustomerService.sln`
2. Right-click `Maliev.CustomerService.Api` → Edit Configurations
3. Set environment variables under "Environment variables"
4. Run/Debug

---

## Development Workflow

### 1. Feature Development

1. Create feature branch: `git checkout -b feature/your-feature`
2. Implement changes following TDD:
   - Write failing test (Red)
   - Implement minimal code to pass (Green)
   - Refactor for clarity (Refactor)
3. Run tests: `dotnet test`
4. Commit with descriptive message

### 2. Database Changes

1. Update entity model in `Maliev.CustomerService.Data/Models/`
2. Update EF configuration in `Maliev.CustomerService.Data/Configurations/`
3. Create migration: `dotnet ef migrations add MigrationName`
4. Review generated migration code
5. Apply migration: `dotnet ef database update`
6. Test with integration tests

### 3. API Changes

1. Update DTOs in `Maliev.CustomerService.Api/DTOs/`
2. Update validators in `Maliev.CustomerService.Api/Validators/`
3. Update controller in `Maliev.CustomerService.Api/Controllers/`
4. Update OpenAPI specification: `specs/001-customer-service-microservice/contracts/openapi.yaml`
5. Write contract tests in `Maliev.CustomerService.Tests/Contract/`

### 4. Code Review Checklist

- [ ] All tests pass (`dotnet test`)
- [ ] Code coverage ≥ 80% for business logic
- [ ] Zero warnings (`dotnet build`)
- [ ] OpenAPI spec updated for API changes
- [ ] FluentValidation validators written for all requests
- [ ] Audit logs created for all mutations
- [ ] Optimistic concurrency enforced with RowVersion
- [ ] External service integrations use Polly retry policies
- [ ] No secrets in source code

---

## Useful Commands

### Database

```bash
# Reset database (WARNING: destroys all data)
dotnet ef database drop --force --project Maliev.CustomerService.Data --startup-project Maliev.CustomerService.Api
dotnet ef database update --project Maliev.CustomerService.Data --startup-project Maliev.CustomerService.Api

# List migrations
dotnet ef migrations list --project Maliev.CustomerService.Data --startup-project Maliev.CustomerService.Api

# Generate SQL script for migration
dotnet ef migrations script --project Maliev.CustomerService.Data --startup-project Maliev.CustomerService.Api --output migration.sql
```

### Testing

```bash
# Run specific test class
dotnet test --filter "FullyQualifiedName~CustomersEndpointTests"

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/ /p:CoverletOutputFormat=opencover

# Generate HTML coverage report (requires ReportGenerator)
reportgenerator -reports:./coverage/coverage.opencover.xml -targetdir:./coverage/html -reporttypes:Html
```

### Swagger

```bash
# Generate OpenAPI JSON from running service
curl http://localhost:8080/customers/swagger/v1/swagger.json -o openapi.json
```

---

## Next Steps

1. **Read the Specification**: `specs/001-customer-service-microservice/spec.md`
2. **Review Data Model**: `specs/001-customer-service-microservice/data-model.md`
3. **Explore API Contracts**: `specs/001-customer-service-microservice/contracts/openapi.yaml`
4. **Run Integration Tests**: `dotnet test Maliev.CustomerService.Tests`
5. **Deploy to Kubernetes**: See deployment documentation in `maliev-gitops` repository

---

## Support

- **Documentation**: `specs/001-customer-service-microservice/`
- **Issues**: https://github.com/MALIEV-Co-Ltd/Maliev.CustomerService/issues
- **Team Contact**: engineering@maliev.com
