# MonoSlice 🍕

> **A modern, scalable, AOT-friendly .NET 10 Modular Monolith with Vertical Slice Architecture & Domain-Driven Design (DDD).**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Vertical%20Slice%20%2B%20DDD-blue.svg)](#architecture-overview)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 🌟 Key Features

- **🎯 .NET 10 Minimal APIs**: High performance, modern API design without heavyweight MVC overhead.
- **🍰 Vertical Slice Architecture**: Features are organized by feature folder (Commands, Queries, Handlers, Endpoints, DTOs in one place) instead of technical layers.
- **🏰 Domain-Driven Design (DDD)**: Rich domain models, `AggregateRoot<TId>`, `Entity<TId>`, domain events, and repository/unit-of-work patterns.
- **⚡ AOT Build Friendly**: Uses compile-time source generation with **`Mediator.SourceGenerator`** and trim-safe patterns to minimize reflection.
- **🐘 PostgreSQL + EF Core**: Module-isolated `DbContext` per domain with automatic schema separation (`users`, `catalog`) and audit timestamp handling.
- **🐰 Native Messaging (RabbitMQ / Kafka)**: Native publisher and background consumer implementations without MassTransit, dynamically switchable via environment variables.
- **🔐 Hybrid Authentication & Authorization**:
  - ASP.NET Core Identity with **GuidV7** (`Guid.CreateVersion7()`) keys.
  - Custom **Composite Auth Middleware** supporting both **JWT Bearer** tokens and **Cookie Authentication**.
  - Role-based authorization (`Admin`, `User`, `Manager`).
  - Access token & Refresh token lifecycle.
- **🗺️ Mapster Mapping**: Fast, compile-time adaptable object mapping.
- **💾 Dual Caching Support**: Seamlessly switch between in-memory cache and **Redis** distributed cache via configuration.
- **🔭 OpenTelemetry & Jaeger**: Distributed tracing, metrics, and structured logs with OTLP exporter integration.
- **📜 Scalar OpenAPI UI**: Beautiful, interactive API documentation replacing default Swagger.
- **🛡️ DataAnnotations Validation**: Fast request validation executed via Mediator pipeline behavior.
- **📦 Standardized API Responses**: Every response wrapped in `ApiResponse<T>` with consistent error codes and validation details.
- **🐳 Docker & Docker Compose**: Complete setup including API, PostgreSQL 17, RabbitMQ Management, Redis, and Jaeger.

---

## 🏛️ Architecture Overview

```
MonoSlice/
├── src/
│   ├── MonoSlice.Host/                    # Composition root & API host
│   ├── MonoSlice.Shared/
│   │   ├── MonoSlice.Shared.Abstractions/ # Core interfaces, CQRS, DDD base types, Contracts, Events
│   │   └── MonoSlice.Shared.Infrastructure/ # Caching, Messaging, Middleware, Behaviors
│   └── Modules/
│       ├── MonoSlice.Modules.Users/       # Identity, JWT, Cookie auth, Role management & UsersModuleApi
│       ├── MonoSlice.Modules.Catalog/     # Catalog domain, stock management, CatalogModuleApi & async consumers
│       └── MonoSlice.Modules.Orders/      # Orders domain, sync/async inter-module communication & background worker
├── tests/
│   ├── MonoSlice.Modules.Users.Tests/    # Users module unit tests
│   ├── MonoSlice.Modules.Catalog.Tests/  # Catalog module unit tests
│   ├── MonoSlice.Modules.Orders.Tests/   # Orders module unit tests
│   └── MonoSlice.IntegrationTests/       # Full integration tests with WebApplicationFactory
├── docker/
│   ├── Dockerfile                        # Multi-stage container build
│   └── docker-compose.yml                # Full local stack (API, DB, RabbitMQ, Redis, Jaeger)
├── .env.example                          # Environment variable templates
└── README.md
```

### Module Boundary Separation & Inter-Module Communication
MonoSlice demonstrates three distinct patterns of decoupling and communication:

1. **Synchronous Inter-Module Queries (Contract-Based)**:
   - Modules do **not** reference private internal entities or `DbContext` of other modules.
   - Modules expose public contract interfaces registered in DI (e.g. `ICatalogModuleApi`, `IUsersModuleApi` in `MonoSlice.Shared.Abstractions/Contracts`).
   - When placing an order, `MonoSlice.Modules.Orders` synchronously queries `ICatalogModuleApi` for product pricing and stock availability, and `IUsersModuleApi` for customer status validation.

2. **Asynchronous Inter-Module Messaging (Event-Driven)**:
   - For state changes and side effects across modules, modules publish `IntegrationEvent` records via `IEventBus` (RabbitMQ / Kafka / In-Memory).
   - When an order is placed, `OrderPlacedIntegrationEvent` is published; `MonoSlice.Modules.Catalog` consumes this event asynchronously via `OrderPlacedIntegrationEventHandler` to decrement inventory.

3. **Asynchronous In-Process Background Queue Processing**:
   - For heavy background workloads (payment fulfillment simulation, invoice generation, fraud checks), `MonoSlice.Modules.Orders` leverages a non-blocking `Channel<T>` queue (`IOrderProcessingQueue`) consumed by a dedicated `OrderProcessingBackgroundService` worker without stalling HTTP requests.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for containers)

### 1. Clone & Build
```bash
git clone https://github.com/Skyleaft/MonoSlice.git
cd MonoSlice
dotnet restore
dotnet build
```

### 2. Run Tests
```bash
dotnet test
```

### 3. Run with Docker Compose (Recommended)
Launch the entire stack (PostgreSQL, RabbitMQ, Redis, Jaeger, and API):
```bash
cd docker
docker-compose up --build -d
```

Access the services:
- **Scalar API Reference**: [http://localhost:8080/scalar](http://localhost:8080/scalar)
- **Health Check**: [http://localhost:8080/health](http://localhost:8080/health)
- **RabbitMQ Management**: [http://localhost:15672](http://localhost:15672) (User: `guest`, Pass: `guest`)
- **Jaeger Tracing UI**: [http://localhost:16686](http://localhost:16686)

---

## ⚙️ Configuration & Environment Variables

Every setting can be overridden via environment variables or `.env`:

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__UsersDb` | `Host=localhost;Database=monoslice_users...` | PostgreSQL connection string for Users module |
| `ConnectionStrings__CatalogDb` | `Host=localhost;Database=monoslice_catalog...` | PostgreSQL connection string for Catalog module |
| `ConnectionStrings__OrdersDb` | `Host=localhost;Database=monoslice_orders...` | PostgreSQL connection string for Orders module |
| `Auth__JwtSecret` | `MonoSlice_Super_Secret_Key...` | Symmetric secret key for JWT signing |
| `Auth__AccessTokenExpiryMinutes` | `60` | JWT expiration time in minutes |
| `Auth__EnableCookieAuth` | `true` | Enables cookie-based fallback authentication |
| `Cache__Provider` | `Memory` | Cache backend: `Memory` or `Redis` |
| `Cache__Redis__ConnectionString` | `localhost:6379` | Redis connection string (if Provider is Redis) |
| `Messaging__Provider` | `RabbitMQ` | Event broker: `RabbitMQ` or `Kafka` |
| `Messaging__RabbitMQ__Host` | `localhost` | RabbitMQ server hostname |
| `Messaging__Kafka__BootstrapServers` | `localhost:9092` | Kafka broker servers list |
| `OpenTelemetry__Endpoint` | `http://localhost:4317` | OTLP gRPC collector endpoint (e.g. Jaeger) |

---

## 📡 API Endpoints

### 👤 Users Module (`/api/users`)
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| `POST` | `/api/users/register` | Register new user account | Anonymous |
| `POST` | `/api/users/login` | Login and receive JWT + refresh token | Anonymous |
| `POST` | `/api/users/logout` | Sign out current user session | Authorized |
| `POST` | `/api/users/refresh-token` | Refresh an expired access token | Anonymous |
| `GET` | `/api/users/me` | Get profile of logged-in user | Authorized |
| `POST` | `/api/users/assign-role` | Assign role (`Admin`, `Manager`, `User`) | Admin only |

### 📦 Catalog Module (`/api/catalog`)
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| `GET` | `/api/catalog/products` | List products with pagination & search | Anonymous |
| `GET` | `/api/catalog/products/{id}` | Get product details (Cached) | Anonymous |
| `POST` | `/api/catalog/products` | Create product (publishes async event) | Admin, Manager |
| `PUT` | `/api/catalog/products/{id}` | Update product details | Admin, Manager |
| `DELETE` | `/api/catalog/products/{id}` | Delete product | Admin only |

### 🛒 Orders Module (`/api/orders`)
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| `POST` | `/api/orders` | Create order (Sync inter-module check, async event + queue) | Anonymous |
| `GET` | `/api/orders/{id}` | Get order details and items | Anonymous |
| `GET` | `/api/orders` | List orders with pagination and status filter | Anonymous |
| `POST` | `/api/orders/{id}/process-async` | Trigger async background fulfillment job | Anonymous |
| `POST` | `/api/orders/{id}/cancel` | Cancel a pending/processing order | Anonymous |

---

## 🧱 Vertical Slice Structure Example

A typical feature slice contains everything in a single, focused directory:

```
src/Modules/MonoSlice.Modules.Catalog/Features/CreateProduct/
├── CreateProductCommand.cs        # Input DTO with DataAnnotations + Response DTO
├── CreateProductCommandHandler.cs # Core business logic & persistence
└── CreateProductEndpoint.cs       # Minimal API endpoint definition & route mapping
```

---

## ⚡ Self-Contained Trimming & Native AOT Compatibility

MonoSlice is engineered with trim-safe patterns, source generation, and compile-time optimization in .NET 10:

1. **Source-Generated JSON & Mediator**:
   - `AppJsonSerializerContext` provides compile-time JSON metadata for all CQRS commands, queries, responses, and OpenAPI schemas without runtime reflection.
   - `Mediator.SourceGenerator` generates strongly-typed dispatch pipelines at build time.
2. **EF Core Compiled Models**:
   - Runtime model building overhead is eliminated with precompiled models generated via `dotnet ef dbcontext optimize` and registered using `options.UseModel(...)`.
   - To regenerate compiled models after modifying domain entities:
     ```bash
     # Users Module
     dotnet ef dbcontext optimize --output-dir Persistence/CompiledModels --namespace MonoSlice.Modules.Users.Persistence.CompiledModels --context UsersDbContext --project src/Modules/MonoSlice.Modules.Users/MonoSlice.Modules.Users.csproj --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj --nativeaot

     # Catalog Module
     dotnet ef dbcontext optimize --output-dir Persistence/CompiledModels --namespace MonoSlice.Modules.Catalog.Persistence.CompiledModels --context CatalogDbContext --project src/Modules/MonoSlice.Modules.Catalog/MonoSlice.Modules.Catalog.csproj --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj --nativeaot
     ```
3. **Self-Contained IL Trimming (Docker Image ~60MB)**:
   - Uses `PublishTrimmed=true` with `TrimMode=partial` to prune unused framework and assembly code while retaining full CoreCLR JIT support for dynamic LINQ queries and EF Core features.
   - `rd.xml` (Trimmer Root Descriptor) ensures Identity and core services are preserved during trimming.
4. **Publishing Self-Contained Binary**:
   ```bash
   dotnet publish src/MonoSlice.Host/MonoSlice.Host.csproj -c Release -r linux-x64 --self-contained -p:PublishTrimmed=true -p:TrimMode=partial
   ```

---

## 🧪 Testing Strategy

- **Unit Tests**: Test handlers in isolation with `NSubstitute` mocks and in-memory EF Core.
- **Domain Tests**: Verify domain model invariants, state transitions, and domain events.
- **Integration Tests**: End-to-end API testing using `WebApplicationFactory<Program>` without external infrastructure dependencies.

```bash
# Run all tests
dotnet test --logger "console;verbosity=normal"
```

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).
