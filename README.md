# Maliev Customer Service

A comprehensive .NET 9.0 microservice for managing customers, companies, and related entities in the Maliev ecosystem.

## Overview

The Customer Service is a core microservice responsible for:
- **Customer Management** - CRUD operations with versioning and soft delete
- **Company Management** - B2B customer organization handling
- **Address Management** - Polymorphic address system for customers and companies
- **NDA Lifecycle** - Complete NDA workflow with state machine (Draft → Signed → Expired/Revoked)
- **Document Management** - Integration with Upload Service for file handling with deferred deletion
- **Internal Notes** - Employee-only annotation system
- **User Management** - Identity and authentication integration
- **Comprehensive Audit Logging** - Complete tracking of all mutations

## Architecture

### Technology Stack
- **Framework**: ASP.NET Core 9.0 WebAPI
- **Database**: PostgreSQL 18 with CloudNativePG operator
- **ORM**: Entity Framework Core 9.0.8
- **Authentication**: JWT Bearer with role-based authorization
- **Validation**: FluentValidation
- **Logging**: Serilog (console-only for centralized collection)
- **Metrics**: Prometheus with ASP.NET Core metrics
- **Deployment**: Kubernetes (GKE) with ArgoCD GitOps

### Key Design Patterns
- **Optimistic Concurrency Control** - RowVersion/Version on all entities
- **Soft Delete Pattern** - IsDeleted flag instead of physical deletion
- **Polymorphic Relationships** - Owner_type/Owner_id pattern for addresses and documents
- **Audit Logging** - Actor tracking (actorId, actorType) for all mutations
- **State Machine** - NDA lifecycle with terminal states
- **Deferred Deletion** - Document cleanup with retry background service

### Database Schema
- **Snake_case naming convention** for all tables and columns
- **UUID primary keys** for all entities
- **Proper indexing** on frequently queried fields (email, company_id, segment, tier, created_at)
- **Optimistic concurrency** via version/row_version timestamp columns

## API Endpoints

All endpoints are prefixed with `/customers/v1`

### Customer Management
- `POST /customers` - Create new customer
- `GET /customers/{id}` - Retrieve customer by ID
- `PATCH /customers/{id}` - Update customer (with optimistic concurrency)
- `DELETE /customers/{id}` - Soft delete customer
- `GET /customers` - List customers with filtering and pagination
- `GET /customers/preferences` - Get customer preferences for compliance/audit

### Company Management
- `POST /companies` - Create new company
- `GET /companies/{id}` - Retrieve company by ID
- `PATCH /companies/{id}` - Update company
- `DELETE /companies/{id}` - Soft delete company
- `GET /companies` - List companies with filtering and pagination

### Address Management
- `POST /addresses` - Create address (for customer or company)
- `GET /addresses/{id}` - Retrieve address by ID
- `PATCH /addresses/{id}` - Update address
- `DELETE /addresses/{id}` - Soft delete address
- `GET /addresses` - List addresses by owner

### NDA Management
- `POST /ndas` - Create NDA record
- `GET /ndas/{id}` - Retrieve NDA by ID
- `PATCH /ndas/{id}/status` - Update NDA status (lifecycle transitions)
- `GET /ndas/customer/{customerId}` - List NDAs for customer

### Document Management
- `POST /documents` - Create document reference
- `GET /documents/{id}` - Retrieve document by ID
- `PATCH /documents/{id}/complete` - Mark document as complete
- `DELETE /documents/{id}` - Delete document (with Upload Service cleanup)
- `GET /documents` - List documents by owner

### Internal Notes (Employee-Only)
- `POST /internal-notes` - Create internal note
- `GET /internal-notes/{ownerType}/{ownerId}` - Get notes for owner
- `DELETE /internal-notes/{id}` - Delete internal note

### User Management
- `POST /users` - Create user account
- `GET /users/{id}` - Retrieve user by ID
- `PATCH /users/{id}` - Update user
- `PATCH /users/{id}/password` - Change password
- `GET /users/customer/{customerId}` - Get user by customer ID
- `GET /users/email/{email}` - Get user by email

### Validation
- `POST /validate/user` - Validate user credentials

### API Documentation
Interactive Swagger UI available at: **`/customers/swagger`**

## Local Development

### Prerequisites
```bash
# Required tools
.NET 9.0 SDK
Docker Desktop
kubectl (for K8s operations)
```

### Quick Start

1. **Clone the repository**
```bash
git clone https://github.com/MALIEV-Co-Ltd/maliev.git
cd maliev/Maliev.CustomerService
```

2. **Restore dependencies**
```bash
dotnet restore Maliev.CustomerService.sln
```

3. **Build the solution**
```bash
dotnet build Maliev.CustomerService.sln
```

4. **Run tests**
```bash
dotnet test Maliev.CustomerService.sln --verbosity normal
```

5. **Run the service locally**
```bash
cd Maliev.CustomerService.Api
dotnet run
```

The service will start on `http://localhost:5000` (or as configured in launchSettings.json).

### Database Migrations

To run migrations against a development PostgreSQL instance:

```bash
# Port-forward to PostgreSQL in Kubernetes (MUST target pod, not service)
kubectl port-forward -n maliev-dev postgres-cluster-1 5432:5432

# Set connection string environment variable
export ConnectionStrings__CustomerDbContext="Server=localhost;Port=5432;Database=customer_db;User Id=postgres;Password=YOUR_PASSWORD;"

# Apply migrations
dotnet ef database update --project Maliev.CustomerService.Data

# Create new migration
dotnet ef migrations add MigrationName --project Maliev.CustomerService.Data
```

### Running Tests

```bash
# Run all tests
dotnet test Maliev.CustomerService.sln

# Run with coverage
dotnet test Maliev.CustomerService.sln --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Maliev.CustomerService.Tests/Maliev.CustomerService.Tests.csproj
```

## Configuration

### Environment Variables

The service uses configuration from multiple sources in order of precedence:
1. Google Secret Manager (mounted at `/mnt/secrets` in Kubernetes)
2. Environment variables
3. appsettings.json (no secrets!)

### Required Secrets

Managed via **Google Secret Manager** (External Secrets Operator):

```yaml
# Database
ConnectionStrings__CustomerDbContext: "Server=postgres-cluster-rw;Port=5432;Database=customer_db;..."

# JWT Authentication
JwtSettings__SecretKey: "your-secret-key"
JwtSettings__Issuer: "maliev-auth-service"
JwtSettings__Audience: "maliev-services"

# External Services
UploadServiceOptions__BaseUrl: "http://maliev-upload-service:8080"
```

**IMPORTANT**: Never commit secrets to source code or appsettings.json!

### Development Overrides

For local development, create `appsettings.Development.json` (git-ignored):

```json
{
  "ConnectionStrings": {
    "CustomerDbContext": "Server=localhost;Port=5432;Database=customer_db_dev;User Id=postgres;Password=dev;"
  },
  "JwtSettings": {
    "SecretKey": "dev-secret-key-min-32-characters-long",
    "Issuer": "maliev-auth-service",
    "Audience": "maliev-services"
  }
}
```

## Deployment

### GitOps Workflow

The service uses **ArgoCD** for GitOps-based deployment:

```
Source Repo (maliev)          GitOps Repo (maliev-gitops)        Kubernetes Cluster
───────────────────           ────────────────────────           ──────────────────

develop branch  ──┬──→ CI/CD ──→ Creates PR ──→ Review ──→      maliev-dev namespace
                  │             (dev image)
                  │
staging tag      ──┼──→ CI/CD ──→ Creates PR ──→ Review ──→      maliev-staging namespace
(release/v*)      │             (staging image)
                  │
main branch      ──┴──→ CI/CD ──→ Creates PR ──→ Review ──→      maliev-prod namespace
                                  (prod image)
```

### CI/CD Pipelines

Three GitHub Actions workflows:

1. **ci-develop.yml** - Triggered on push to `develop` branch
   - Builds and pushes to `maliev-website-artifact-dev`
   - Creates PR to update development overlay

2. **ci-staging.yml** - Triggered on tags matching `release/v*`
   - Builds and pushes to `maliev-website-artifact-staging`
   - Creates PR to update staging overlay

3. **ci-main.yml** - Triggered on push to `main` branch
   - Builds and pushes to `maliev-website-artifact-prod`
   - Creates PR to update production overlay (requires careful review)

### Kubernetes Manifests

Located in `maliev-gitops/3-apps/maliev-customer-service/`:

```
base/
├── deployment.yaml          # Base deployment configuration
├── service.yaml             # Service definition
├── hpa.yaml                 # Horizontal Pod Autoscaler
├── servicemonitor.yaml      # Prometheus metrics scraping
├── service-secrets.yaml     # External Secrets reference
└── kustomization.yaml       # Base kustomization

overlays/
├── development/             # Dev-specific patches
├── staging/                 # Staging-specific patches
└── production/              # Prod-specific patches
```

### Health Checks

All endpoints are prefixed with `/customers` via `UsePathBase("/customers")` configuration.

- **Liveness Probe**: `/customers/liveness` - Returns "Healthy"
- **Readiness Probe**: `/customers/readiness` - Checks database connectivity

### Monitoring

- **Metrics**: Exposed at `/metrics` in Prometheus format
- **ServiceMonitor**: Auto-discovered by Prometheus (scrape interval: 30s)
- **Dashboards**: Available in Grafana (access via `scripts/open-grafana.ps1`)

### Debugging in Kubernetes

```bash
# Port-forward to service
kubectl port-forward -n maliev-dev svc/maliev-customer-service 8080:8080

# Access Swagger UI
http://localhost:8080/customers/swagger

# View logs
kubectl logs -f deployment/maliev-customer-service -n maliev-dev

# Execute into pod
kubectl exec -it deployment/maliev-customer-service -n maliev-dev -- /bin/bash
```

## Background Services

### NDA Expiration Service
- Runs every 24 hours
- Automatically transitions Signed NDAs to Expired based on expires_at timestamp
- Logs count of expired NDAs

### Document Deletion Retry Service
- Runs every 6 hours
- Retries deletion of documents in PendingDeletion status
- Handles Upload Service availability issues

## Security

### Authentication & Authorization

- **JWT Bearer Tokens** required for all endpoints (except health checks)
- **Role-based access control**:
  - `Customer` role - Access to own data only
  - `Employee` role - Access to customer management
  - `Manager`/`Admin` roles - Full access

### Authorization Policies

- **EmployeeOrHigher** - Restricts internal notes to employees only
- Automatic 403 Forbidden for unauthorized access

### Input Validation

- **FluentValidation** on all request DTOs
- Conditional validation based on operation type
- Comprehensive error responses with field-level details

### Data Protection

- **Soft delete** prevents accidental data loss
- **Audit logging** tracks all mutations
- **Optimistic concurrency** prevents lost updates
- **No secrets in source code** - all via Google Secret Manager

## Performance

### Target Metrics
- **p95 latency < 200ms** for simple CRUD operations
- **p95 latency < 500ms** for complex queries
- **Database query optimization** with proper indexing
- **Read-only queries** use AsNoTracking() where appropriate

### Caching
- **MemoryCache** configured for simple use (no SizeLimit per CLAUDE.md)
- Appropriate cache expiration policies

## Development Guidelines

### Code Style
- Follow Microsoft C# coding conventions
- Use async/await consistently
- Implement optimistic concurrency on all updates
- Add audit logging for all mutations

### Testing
- Unit tests for business logic
- Integration tests for database operations
- Validation tests for FluentValidation rules

### Database Migrations
- Always create migrations for schema changes
- Test migrations in development before production
- Never modify existing migrations after deployment

## Troubleshooting

### Common Issues

**Build Errors**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

**Migration Issues**
```bash
# Drop and recreate database (DEV ONLY)
dotnet ef database drop --project Maliev.CustomerService.Data
dotnet ef database update --project Maliev.CustomerService.Data
```

**Port Forwarding Issues**
```bash
# Always target pod directly, not service
kubectl port-forward -n maliev-dev postgres-cluster-1 5432:5432
```

## Contributing

1. Create feature branch from `develop`
2. Implement changes with tests
3. Ensure zero warnings in build
4. Create pull request to `develop`
5. Wait for CI/CD to pass and code review

## License

Proprietary - MALIEV Co. Ltd.

## Support

For questions or issues, contact the Maliev development team.
