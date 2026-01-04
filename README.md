# Maliev.CustomerService

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/MALIEV-Co-Ltd/Maliev.CustomerService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/PostgreSQL-18-336791.svg)](https://www.postgresql.org/)
[![Tests](https://img.shields.io/badge/tests-143%2F143%20passing-brightgreen.svg)](https://github.com/MALIEV-Co-Ltd/Maliev.CustomerService)
[![License](https://img.shields.io/badge/license-Proprietary-red.svg)](LICENSE)

Comprehensive customer and company management system for the MALIEV platform. Handles customer lifecycle, NDA workflows, company relationships, address management, and document handling with full IAM integration and event-driven communication via MessagingContracts.

---

## Architecture & Tech Stack

### Technology Stack
- **.NET 10.0**: ASP.NET Core Web API with C# 13
- **PostgreSQL 18**: Primary database with Entity Framework Core 10.x
- **Redis**: Distributed caching for frequently accessed customer data
- **RabbitMQ**: Event-driven messaging via MassTransit 8.5.7
- **OpenTelemetry**: Structured logging, metrics, and distributed tracing
- **Testcontainers**: Integration testing with real PostgreSQL, Redis, RabbitMQ

### Project Structure
```
Maliev.CustomerService/
├── Maliev.CustomerService.Api/          # Presentation layer
│   ├── Controllers/                     # REST API endpoints
│   ├── Services/                        # Business logic
│   ├── Models/                          # DTOs (Request/Response)
│   └── Consumers/                       # MassTransit event consumers
├── Maliev.CustomerService.Data/         # Data access layer
│   ├── Models/                          # EF Core entities
│   ├── Configurations/                  # Entity configurations
│   └── Migrations/                      # Database migrations
└── Maliev.CustomerService.Tests/        # Integration tests
    ├── Integration/                     # API integration tests
    ├── Services/                        # Service unit tests
    └── Infrastructure/                  # Test infrastructure
```

### Dependencies

**Databases:**
- **PostgreSQL 18**: Customer master data, companies, addresses, NDAs, documents, audit trail
- **Redis**: Caching for frequently accessed customer information

**Messaging:**
- **RabbitMQ**: Event publishing (customer lifecycle events) and consumption (file deletion)

**External Services:**
- **IAM Service**: Authentication, authorization, and permission management
- **Upload Service**: Document file storage and management
- **Order Service**: Customer order history and relationship

---

## ⚠️ Constitution Rules

These rules are **non-negotiable** and apply to ALL Maliev microservices:

### Banned Libraries
| ❌ BANNED | ✅ USE INSTEAD |
|-----------|----------------|
| AutoMapper | Explicit manual mapping |
| FluentValidation | Data Annotations (`[Required]`, `[StringLength]`, etc.) |
| FluentAssertions | xUnit `Assert.*` methods |
| In-memory test DB | Testcontainers (real PostgreSQL) |
| `/src` or `/tests` folders | Flat project structure at repo root |

### Mandatory Practices
- **No Secrets in Code**: All secrets injected via Google Secret Manager (environment variables)
- **TreatWarningsAsErrors**: Enabled in all `.csproj` files - zero warnings tolerated
- **XML Documentation**: Required on ALL public methods, properties, and classes
- **MessagingContracts Only**: ALL events use `Maliev.MessagingContracts` package (no local events)
- **ServiceDefaults Integration**: Use `Maliev.Aspire.ServiceDefaults` for infrastructure patterns

---

## Key Features

### Customer Management
- **Complete Lifecycle**: From registration to deactivation with soft delete
- **Customer Types**: Individual consumers and business customers
- **Segmentation**: Customer categorization (Retail, Wholesale, VIP)
- **Tier System**: Bronze, Silver, Gold, Platinum with benefits
- **Profile Management**: Preferences, language, timezone, communication settings
- **Activity Tracking**: Last login, last order, last contact timestamps

### Company Management
- **B2B Support**: Company registration and management
- **Customer Association**: Link multiple customers to companies
- **Company Hierarchy**: Parent-subsidiary relationships
- **Industry Classification**: Industry and sub-industry tracking
- **Tax Information**: Tax ID, VAT registration, compliance documents

### NDA Lifecycle Management
```
Draft → Pending Signature → Signed → Active/Expired/Revoked
```

**Status Transitions:**
- **Draft**: NDA created, not yet sent
- **Pending Signature**: Sent to customer for signing
- **Signed**: Customer signed, awaiting approval
- **Active**: NDA approved and in effect
- **Expired**: NDA validity period ended
- **Revoked**: NDA terminated before expiration

### Address Management
- **Polymorphic Design**: Addresses for customers, companies, and other entities
- **Address Types**: Billing, shipping, registered office, branch
- **Validation**: Country-specific address validation
- **Geocoding**: Integration ready for address geocoding
- **Multiple Addresses**: Customers/companies can have multiple addresses

### Document Management
- **Upload Integration**: Seamless integration with UploadService
- **Document Types**: Contracts, NDAs, certifications, compliance docs
- **Deferred Deletion**: 30-day retention after customer deletion
- **Document Versioning**: Track document revisions
- **Access Control**: Permission-based document access

### Internal Notes System
- **Employee-Only**: Notes invisible to customers
- **Categorization**: Call logs, complaints, follow-ups, sales notes
- **Actor Tracking**: Who created each note and when
- **Search**: Full-text search across notes
- **Audit Trail**: Complete history of note creation and updates

### Event-Driven Integration
- **Events Published** (via MessagingContracts):
  - `CustomerCreatedEvent` - New customer registered
  - `CustomerUpdatedEvent` - Customer information modified
  - `CustomerDeactivatedEvent` - Customer account deactivated
  - `CustomerReactivatedEvent` - Customer account reactivated
  - `CompanyCreatedEvent` - New company registered
  - `CompanyUpdatedEvent` - Company information modified
  - `NDASignedEvent` - NDA signed by customer
  - `NDAExpiredEvent` - NDA expired
  - `NDARevokedEvent` - NDA revoked

- **Events Consumed**:
  - `FileDeletedEvent` - Update document status when files are deleted from UploadService

---

## Quick Start

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL 18 (local or via Kubernetes port-forward)
- Redis (optional, for caching)
- RabbitMQ (optional, for event messaging)

### Local Development

1. **Clone Repository**
   ```bash
   git clone https://github.com/MALIEV-Co-Ltd/Maliev.CustomerService.git
   cd Maliev.CustomerService
   ```

2. **Configure Database Connection**
   ```bash
   # Set connection string environment variable
   export ConnectionStrings__CustomerDbContext="Host=localhost;Port=5432;Database=customer_app_db;Username=postgres;Password=<password>;"
   ```

3. **Apply Database Migrations**
   ```bash
   dotnet ef database update --project Maliev.CustomerService.Data
   ```

4. **Run the Service**
   ```bash
   cd Maliev.CustomerService.Api
   dotnet run
   ```

5. **Access API Documentation**
   - Scalar UI: http://localhost:5000/customer/scalar
   - OpenAPI Spec: http://localhost:5000/customer/openapi/v1.json
   - Health Check: http://localhost:5000/customer/readiness

### Docker Deployment

```bash
# Build image
docker build -t maliev/customer-service:latest .

# Run container
docker run -p 8080:8080 \
  -e ConnectionStrings__CustomerDbContext="Host=postgres;Port=5432;Database=customer_app_db;..." \
  -e Jwt__PublicKey="<base64-encoded-public-key>" \
  maliev/customer-service:latest
```

---

## API Endpoints

All endpoints are prefixed with `/customer` (configured via `UsePathBase("/customer")`):

### Customers
- `GET /customer/v1/customers` - List customers (paginated, filterable)
- `POST /customer/v1/customers` - Create new customer
- `GET /customer/v1/customers/{id}` - Get customer details
- `PUT /customer/v1/customers/{id}` - Update customer
- `DELETE /customer/v1/customers/{id}` - Soft delete customer
- `POST /customer/v1/customers/{id}/reactivate` - Reactivate customer
- `GET /customer/v1/customers/{id}/orders` - Get customer orders
- `GET /customer/v1/customers/{id}/documents` - Get customer documents
- `GET /customer/v1/customers/search` - Search customers

### Companies
- `GET /customer/v1/companies` - List companies
- `POST /customer/v1/companies` - Create new company
- `GET /customer/v1/companies/{id}` - Get company details
- `PUT /customer/v1/companies/{id}` - Update company
- `DELETE /customer/v1/companies/{id}` - Soft delete company
- `POST /customer/v1/companies/{id}/customers` - Associate customer with company
- `GET /customer/v1/companies/{id}/customers` - Get company customers

### NDAs
- `GET /customer/v1/ndas` - List NDAs
- `POST /customer/v1/ndas` - Create NDA draft
- `GET /customer/v1/ndas/{id}` - Get NDA details
- `PUT /customer/v1/ndas/{id}` - Update NDA draft
- `POST /customer/v1/ndas/{id}/send` - Send NDA for signature
- `POST /customer/v1/ndas/{id}/sign` - Record NDA signature
- `POST /customer/v1/ndas/{id}/approve` - Approve signed NDA
- `POST /customer/v1/ndas/{id}/revoke` - Revoke active NDA
- `GET /customer/v1/ndas/customer/{customerId}` - Get customer NDAs

### Addresses
- `GET /customer/v1/addresses/owner/{ownerType}/{ownerId}` - Get addresses for owner
- `POST /customer/v1/addresses` - Create address
- `GET /customer/v1/addresses/{id}` - Get address details
- `PUT /customer/v1/addresses/{id}` - Update address
- `DELETE /customer/v1/addresses/{id}` - Delete address

### Internal Notes
- `GET /customer/v1/notes/customer/{customerId}` - Get customer notes
- `POST /customer/v1/notes` - Create note
- `GET /customer/v1/notes/{id}` - Get note details
- `PUT /customer/v1/notes/{id}` - Update note
- `DELETE /customer/v1/notes/{id}` - Delete note

### Documents
- `GET /customer/v1/documents/owner/{ownerType}/{ownerId}` - Get documents for owner
- `POST /customer/v1/documents` - Upload document
- `GET /customer/v1/documents/{id}` - Get document details
- `DELETE /customer/v1/documents/{id}` - Mark document for deletion

---

## Health & Monitoring

### Health Endpoints
- **Liveness**: `GET /customer/liveness` - Service is running
- **Readiness**: `GET /customer/readiness` - Service is ready (DB + dependencies healthy)

### Observability
- **Metrics**: Prometheus metrics at `/customer/metrics`
- **Tracing**: OpenTelemetry distributed tracing to configured OTLP endpoint
- **Logging**: Structured logging with correlation IDs via ServiceDefaults

### Health Check Components
- PostgreSQL connection
- Redis cache availability
- RabbitMQ connection
- External service connectivity (IAM, Upload, Order)

---

## Configuration

### Required Secrets (Google Secret Manager)
```
ConnectionStrings__CustomerDbContext - PostgreSQL connection string
Jwt__PublicKey  - Base64-encoded RSA-2048 public key (PEM format)
```

### Environment Variables
```bash
ConnectionStrings__CustomerDbContext="Host=postgres;Port=5432;Database=customer_app_db;Username=app;Password=..."
ConnectionStrings__redis="redis:6379"
ConnectionStrings__rabbitmq="amqp://guest:guest@rabbitmq:5672"
Jwt__Issuer="https://dev.api.maliev.com/auth"
Jwt__Audience="https://dev.api.maliev.com"
ExternalServices__IAMService__BaseUrl="http://iam-service:8080"
ExternalServices__UploadService__BaseUrl="http://upload-service:8080"
ExternalServices__OrderService__BaseUrl="http://order-service:8080"
Features__PrincipalBasedAuthEnabled="true"
```

### Configuration Files
- `appsettings.json` - Production settings (no secrets)
- `appsettings.Development.json` - Local development overrides
- `appsettings.Testing.json` - Test configuration with test keys

---

## IAM Integration

### Required Permissions
- `customer.customers.read` - View customers
- `customer.customers.write` - Create and update customers
- `customer.customers.delete` - Delete customers
- `customer.companies.read` - View companies
- `customer.companies.write` - Create and update companies
- `customer.companies.delete` - Delete companies
- `customer.ndas.read` - View NDAs
- `customer.ndas.write` - Create and manage NDAs
- `customer.ndas.approve` - Approve signed NDAs
- `customer.addresses.read` - View addresses
- `customer.addresses.write` - Manage addresses
- `customer.notes.read` - View internal notes
- `customer.notes.write` - Create and edit notes
- `customer.documents.read` - View documents
- `customer.documents.write` - Upload and manage documents

### Predefined Roles
- **Customer Service Representative**: Manage customers and companies (`customer.customers.*`, `customer.companies.read`, `customer.notes.*`)
- **Sales Manager**: Full access including NDAs (`customer.*`)
- **Account Manager**: Customer relationship management (`customer.customers.read`, `customer.notes.*`, `customer.ndas.read`)
- **Compliance Officer**: NDA and document management (`customer.ndas.*`, `customer.documents.*`)

---

## Testing

### Test Coverage
**143/143 integration tests passing (100%)**

Test suites:
- **CustomersController Tests** (45 tests)
  - CRUD operations (create, read, update, delete, reactivate)
  - Search and filtering
  - Authorization checks (permission-based)
- **CompaniesController Tests** (28 tests)
  - Company management
  - Customer association
- **NDAsController Tests** (32 tests)
  - NDA lifecycle (draft, send, sign, approve, revoke)
  - Status transitions
- **AddressesController Tests** (18 tests)
  - Polymorphic address management
- **NotesController Tests** (12 tests)
  - Internal note CRUD operations
- **DocumentsController Tests** (6 tests)
  - Document upload and management
- **IAM Integration Tests** (1 test)
  - Principal creation with IAM service
- **Service Unit Tests** (1 test)
  - CustomerService business logic

### Running Tests

```bash
# Run all tests
dotnet test Maliev.CustomerService.sln --verbosity normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~CustomersControllerTests"
```

### Test Infrastructure
- **Testcontainers**: Real PostgreSQL 18, Redis, RabbitMQ containers
- **xUnit**: Test framework with `Assert.*` assertions
- **Test Database Fixture**: Shared test database with cleanup between tests

---

## Database

### Database Schema

**PostgreSQL 18** with Entity Framework Core migrations.

**Main Tables:**
- `Customers` - Customer master data (name, email, segment, tier, profile settings)
- `Companies` - Company information (name, tax ID, industry, parent company)
- `NDAs` - NDA documents (status, validity, signed date, revoked date)
- `Addresses` - Polymorphic addresses (owner type/ID, address type, country, postal code)
- `DocumentReferences` - Document metadata (owner type/ID, file reference, document type)
- `InternalNotes` - Employee notes (category, actor, visibility)
- `AuditLogs` - Complete audit trail (action, actor, entity, changes)

**Customer Status Values:**
- `Active`, `Inactive`, `Suspended`, `Deleted`

**NDA Status Values:**
- `Draft`, `PendingSignature`, `Signed`, `Active`, `Expired`, `Revoked`

### Database Migrations

```bash
# Port forward to PostgreSQL pod (MUST use pod, not service)
kubectl port-forward -n <namespace> <postgres-pod> 5432:5432

# Set connection string environment variable
export ConnectionStrings__CustomerDbContext="Host=localhost;Port=5432;Database=customer_app_db;Username=postgres;Password=<password>;"

# Create migration
dotnet ef migrations add MigrationName --project Maliev.CustomerService.Data

# Apply migration
dotnet ef database update --project Maliev.CustomerService.Data

# Rollback migration
dotnet ef database update PreviousMigrationName --project Maliev.CustomerService.Data
```

---

## Deployment

### Kubernetes Deployment

Service uses GitHub Actions workflows for automated deployment to development, staging, and production environments.

Deployments are managed via GitOps (ArgoCD).

### Port Forwarding

```bash
# Forward to service
kubectl port-forward -n <namespace> svc/<service-name> 8080:8080

# Forward to PostgreSQL (for migrations)
kubectl port-forward -n <namespace> <postgres-pod> 5432:5432

# Forward to Redis
kubectl port-forward -n <namespace> svc/redis 6379:6379
```

### Logs

```bash
# Tail logs
kubectl logs -f deployment/<service-name> -n <namespace>

# Get pod status
kubectl get pods -n <namespace> | grep <service-name>

# Describe pod
kubectl describe pod <pod-name> -n <namespace>
```

---

## Common Issues

### Issue: Tests fail with "Database connection string not configured"
**Solution**: Set `ConnectionStrings__CustomerDbContext` environment variable before running tests, or configure via User Secrets:
```bash
cd Maliev.CustomerService.Tests
dotnet user-secrets set "ConnectionStrings:CustomerDbContext" "Host=localhost;Port=5432;..."
```

### Issue: Migration fails with "Cannot connect to database"
**Solution**: Ensure PostgreSQL is accessible. If using Kubernetes, port-forward to the pod (NOT service):
```bash
kubectl port-forward -n <namespace> <postgres-pod> 5432:5432
```

### Issue: Scalar UI returns 404
**Solution**: Scalar is disabled in production. Check environment is Development or Staging.

### Issue: All JWT validations fail
**Solution**: Ensure `Jwt:PublicKey` is correctly configured in Google Secret Manager and matches the AuthService private key.

### Issue: Events not publishing
**Solution**: Verify RabbitMQ connection string is configured and MessagingContracts package is up-to-date.

### Issue: FileDeletedEvent consumer not triggering
**Solution**: Ensure UploadService is publishing events to correct exchange and CustomerService consumer is registered.

---

## Development Guidelines

### Adding New Endpoints
1. Create request/response models in `Models/`
2. Add validators using Data Annotations
3. Implement service logic in `Services/`
4. Add controller action in `Controllers/`
5. Write integration tests in `Tests/Integration/`
6. Update API documentation in README.md

### Adding New Database Entities
1. Create entity class in `Data/Models/`
2. Add EF Core configuration in `Data/Configurations/`
3. Update `CustomerDbContext.cs` with DbSet
4. Create migration: `dotnet ef migrations add EntityName --project Maliev.CustomerService.Data`
5. Apply migration: `dotnet ef database update --project Maliev.CustomerService.Data`

### Code Style
- Follow .NET naming conventions
- Use async/await for all I/O operations
- Use dependency injection for all services
- Add XML documentation comments for public APIs
- Include structured logging with correlation IDs

---

## Support

- **CLAUDE.md**: Service-specific development guidelines
- **ServiceDefaults Documentation**: See Maliev.Aspire.ServiceDefaults repository
- **MessagingContracts**: See Maliev.MessagingContracts repository

---

## License

**Proprietary** - Copyright © 2025 MALIEV Co., Ltd. All rights reserved.
