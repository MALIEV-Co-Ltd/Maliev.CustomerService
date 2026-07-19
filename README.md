# Maliev Customer Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.CustomerService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

Comprehensive customer and company management system for the global Maliev platform.

**Role in MALIEV Architecture**: The authoritative source for customer profiles and business relationships. It manages the complete lifecycle of individuals and companies, including NDA workflows, address hierarchies, and relationship metadata used by Sales and CRM services.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Distributed Cache**: Redis 7.x (High-frequency profile resolution)
- **Messaging**: RabbitMQ via MassTransit
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[EmailAddress]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Entity Relationship Engine**: Sophisticated management of B2B relationships between individual customers and parent companies.
- **NDA Lifecycle**: Fully automated Non-Disclosure Agreement workflow from draft to approval and revocation.
- **Hierarchical Addressing**: Polymorphic address management for billing, shipping, and registered offices across multiple entities.
- **Customer Segmentation**: Dynamic tiering (Bronze to Platinum) and categorization (Retail, Wholesale, VIP).
- **Document Management**: Seamless integration for storing and versioning contracts and certifications via the platform upload ecosystem.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.CustomerService.git
cd Maliev.CustomerService
```

2. **Spin up Infrastructure**
```bash
docker run --name customer-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name customer-redis -p 6379:6379 -d redis:7-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__CustomerDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__Cache="YOUR_REDIS_CONNECTION_STRING"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.CustomerService.Data
dotnet run --project Maliev.CustomerService.Api
```

The service will be available at `http://localhost:5000/customer`. Access the interactive documentation at `http://localhost:5000/customer/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/customer/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/customers` | List and filter customer profiles |
| POST | `/companies` | Register a new business entity |
| POST | `/ndas` | Initiate a new NDA workflow |
| GET | `/addresses/owner/{type}/{id}` | Retrieve entity-associated addresses |

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /customer/liveness`
- **Readiness**: `GET /customer/readiness` (Checks DB and Redis connectivity)
- **Metrics**: `GET /customer/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Contract Tests**: Ensure API stability for consumers.

---

## ✅ Validation and release boundary

Pull requests, `main`, `develop`, and `release/v*` tags run the same read-only
.NET validation workflow. Validation checks out immutable public revisions of
the MALIEV shared sources and restores only from NuGet.org, so it does not need
repository secrets or package credentials.

No workflow in this repository publishes images, authenticates to Google
Cloud, changes GitOps, or deploys to GKE. Release must be introduced separately
through an explicitly reviewed flow.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-customer-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
