# Developer Onboarding Guide

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Last Updated:** June 2026

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Project Structure](#2-project-structure)
3. [Architecture Patterns](#3-architecture-patterns)
4. [Getting Started](#4-getting-started)
5. [How to Add a New Module](#5-how-to-add-a-new-module)
6. [How to Add a New Entity](#6-how-to-add-a-new-entity)
7. [How to Add a Command Handler](#7-how-to-add-a-command-handler)
8. [How to Add a Controller Endpoint](#8-how-to-add-a-controller-endpoint)
9. [Coding Conventions](#9-coding-conventions)
10. [Testing Guidelines](#10-testing-guidelines)
11. [Localization](#11-localization)
12. [CI/CD Pipeline](#12-cicd-pipeline)

---

## 1. Project Overview

The Georgia ERP Platform is a .NET 9 modular monolith designed for retail businesses operating in Georgia. It provides:

- **Product Catalog** with category hierarchy, barcodes, and variants
- **Inventory Management** with multi-warehouse stock tracking, transfers, and stock counts
- **Point of Sale (POS)** with session management and transaction processing
- **Procurement** with purchase orders, supplier management, and goods receipt
- **Pricing** with price lists, promotions, and tiered pricing
- **Finance** with chart of accounts, journal entries, and bank account management
- **Compliance** with RS.GE fiscal integration (waybills, invoices, VAT)
- **CRM** with customer management and loyalty program
- **Organization** management with multi-store and multi-warehouse support
- **User Management** with role-based access control and two-factor authentication
- **Licensing** for commercial distribution

The platform exposes a REST API consumed by a WPF Desktop application and (future) web/mobile clients.

---

## 2. Project Structure

```
Enterprise-Retail-ERP-Platform-/
|
+-- src/
|   +-- GeorgiaERP.Domain/           # Domain entities, enums, value objects
|   +-- GeorgiaERP.Application/      # CQRS commands, queries, handlers, DTOs
|   +-- GeorgiaERP.Infrastructure/   # EF Core, persistence, RS.GE SOAP client
|   +-- GeorgiaERP.Api/              # ASP.NET Core API controllers, middleware
|   +-- GeorgiaERP.Workers/          # Background services (RS.GE queue processor)
|   +-- GeorgiaERP.Desktop/          # WPF desktop client application
|
+-- tests/
|   +-- GeorgiaERP.Tests/            # Unit and integration tests
|
+-- docs/                            # Architecture and operational documentation
|
+-- docker/                          # Alternative Docker Compose setup
|   +-- docker-compose.yml
|   +-- Dockerfile.api
|
+-- docker-compose.yml               # Primary Docker Compose (root)
+-- Dockerfile                       # API Docker image
+-- Dockerfile.workers               # Workers Docker image
+-- .github/workflows/ci.yml         # CI/CD pipeline
```

### Layer Dependencies

```
GeorgiaERP.Api            --> Application, Infrastructure
GeorgiaERP.Workers        --> Application, Infrastructure
GeorgiaERP.Application    --> Domain
GeorgiaERP.Infrastructure --> Application, Domain
GeorgiaERP.Desktop        --> (HTTP client, standalone)
```

**Dependency rule:** Domain has zero external dependencies. Application depends only on Domain. Infrastructure implements interfaces defined in Application. API and Workers are composition roots.

### Domain Layer (`GeorgiaERP.Domain`)

Contains pure domain entities, enumerations, and business rules. No framework dependencies.

**Module structure:**

```
GeorgiaERP.Domain/
+-- Compliance/       # FiscalDocument, RsGeWaybill, VatDeclaration
+-- CRM/              # Customer, LoyaltyTransaction
+-- Finance/          # ChartOfAccount, JournalEntry, BankAccount
+-- Identity/         # User, Role, UserRole, RefreshToken
+-- Inventory/        # StockLevel, StockMovement, TransferOrder, StockCount
+-- Organization/     # Company, Store, Warehouse
+-- POS/              # PosTerminal, PosSession, PosTransaction
+-- Pricing/          # PriceList, PriceListItem, Promotion
+-- Procurement/      # Supplier, PurchaseOrder, GoodsReceiptNote
+-- Products/         # Product, ProductCategory, ProductBarcode, ProductVariant
```

### Application Layer (`GeorgiaERP.Application`)

Contains CQRS commands, queries, handlers, DTOs, and interface definitions.

**Per module:**

```
GeorgiaERP.Application/
+-- Products/
|   +-- Commands/
|   |   +-- CreateProductCommand.cs           # Command record
|   |   +-- CreateProductCommandHandler.cs    # Handler
|   +-- Queries/
|   |   +-- GetProductsQuery.cs
|   |   +-- GetProductByIdQuery.cs
|   +-- DTOs/
|       +-- ProductDto.cs                     # Response DTOs, request records
+-- Common/
    +-- IAppDbContext.cs                      # Database abstraction
    +-- Result.cs                            # Result pattern for error handling
```

### Infrastructure Layer (`GeorgiaERP.Infrastructure`)

Implements persistence (EF Core), external service clients (RS.GE SOAP), and cross-cutting concerns.

**Key files:**

- `Persistence/AppDbContext.cs` - EF Core DbContext
- `Persistence/SeedData.cs` - Initial data seeding
- `Persistence/SeedDemoData.cs` - Demo data for development
- `RsGe/RsGeSoapClient.cs` - RS.GE SOAP integration
- `Identity/JwtTokenService.cs` - JWT token generation

### API Layer (`GeorgiaERP.Api`)

ASP.NET Core controllers, middleware, and startup configuration.

**Key files:**

- `Program.cs` - Application startup, service registration, middleware pipeline
- `Controllers/` - 15 controller classes (14 domain + 1 base)
- `Middleware/` - Exception handling, security headers, permission checks, request size limits, audit logging

---

## 3. Architecture Patterns

### CQRS (Command Query Responsibility Segregation)

The application uses MediatR to implement CQRS:

- **Commands** modify state and return `Result` or `Result<T>`
- **Queries** read data and return DTOs
- **Handlers** contain business logic, one handler per command/query

**Command flow:**

```
Controller -> MediatR.Send(Command) -> CommandHandler -> DbContext -> Database
                                                      -> Result<T>
```

**Query flow:**

```
Controller -> MediatR.Send(Query) -> QueryHandler -> DbContext -> Database
                                                   -> DTO(s)
```

### Result Pattern

All command handlers return `Result` or `Result<T>` instead of throwing exceptions for expected failures:

```csharp
// Success
return Result.Success();
return Result.Success(new SomeDto(...));

// Failure
return Result.Failure("Error message");
return Result.Failure<SomeDto>("Error message");

// With error code
return Result.Failure("Not found", "NOT_FOUND");
```

Controllers use `ToActionResult()` to map results to HTTP responses:

```csharp
var result = await _mediator.Send(command);
return ToActionResult(result);  // Maps NOT_FOUND -> 404, VALIDATION_ERROR -> 400, etc.
```

### Domain Entity Pattern

Domain entities use factory methods and encapsulate behavior:

```csharp
public class Product : BaseEntity
{
    // Private setters - state changes only through methods
    public string Sku { get; private set; }
    public string Name { get; private set; }

    // Factory method
    public static Product Create(string sku, string name, ...) { ... }

    // Behavior methods
    public void Deactivate() { IsActive = false; }
    public void UpdateDetails(string name, ...) { ... }
}
```

### Async RS.GE Integration

RS.GE SOAP operations are handled asynchronously via RabbitMQ:

```
API -> Create FiscalDocument -> Enqueue to RabbitMQ -> Return HTTP 202
Worker -> Dequeue -> Call RS.GE SOAP -> Update FiscalDocument status
```

This prevents RS.GE latency or downtime from blocking business operations.

---

## 4. Getting Started

### First-Time Setup

1. **Clone the repository:**

   ```bash
   git clone https://github.com/your-org/Enterprise-Retail-ERP-Platform-.git
   cd Enterprise-Retail-ERP-Platform-
   ```

2. **Start infrastructure services:**

   ```bash
   docker compose up -d postgres rabbitmq
   ```

3. **Restore packages and build:**

   ```bash
   dotnet restore src/GeorgiaERP.Api/GeorgiaERP.Api.csproj
   dotnet build src/GeorgiaERP.Api/GeorgiaERP.Api.csproj
   ```

4. **Run the API:**

   ```bash
   dotnet run --project src/GeorgiaERP.Api
   ```

   On first run in Development mode, the API will:
   - Create the database and apply migrations
   - Seed roles and admin user
   - Seed demo data (products, customers, etc.)

5. **Open Swagger UI:** Navigate to `http://localhost:5000/swagger`

6. **Login with the default admin account** via `POST /api/v1/auth/login` to get a JWT token.

### Running Tests

```bash
dotnet test tests/GeorgiaERP.Tests/GeorgiaERP.Tests.csproj
```

Tests require a running PostgreSQL instance (the CI pipeline provides one).

### Running the Desktop Client

```bash
dotnet run --project src/GeorgiaERP.Desktop
```

The desktop client connects to the API and provides a WPF UI for POS operations, product management, and reporting.

---

## 5. How to Add a New Module

Follow these steps to add a new module (e.g., "Warehouse Analytics"):

### Step 1: Define Domain Entities

Create a new folder in the Domain project:

```
src/GeorgiaERP.Domain/Analytics/
+-- AnalyticsReport.cs
+-- ReportType.cs  (enum)
```

```csharp
namespace GeorgiaERP.Domain.Analytics;

public class AnalyticsReport : BaseEntity
{
    public string Title { get; private set; } = null!;
    public ReportType Type { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public string? Data { get; private set; }

    public static AnalyticsReport Create(string title, ReportType type)
    {
        return new AnalyticsReport
        {
            Id = Guid.NewGuid(),
            Title = title,
            Type = type,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
```

### Step 2: Add DbSet to IAppDbContext

In `src/GeorgiaERP.Application/Common/IAppDbContext.cs`:

```csharp
DbSet<AnalyticsReport> AnalyticsReports { get; }
```

And in `src/GeorgiaERP.Infrastructure/Persistence/AppDbContext.cs`:

```csharp
public DbSet<AnalyticsReport> AnalyticsReports => Set<AnalyticsReport>();
```

### Step 3: Create EF Core Configuration

In `src/GeorgiaERP.Infrastructure/Persistence/Configurations/`:

```csharp
public class AnalyticsReportConfiguration : IEntityTypeConfiguration<AnalyticsReport>
{
    public void Configure(EntityTypeBuilder<AnalyticsReport> builder)
    {
        builder.ToTable("analytics_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
    }
}
```

### Step 4: Create Application Layer

```
src/GeorgiaERP.Application/Analytics/
+-- Commands/
|   +-- GenerateReportCommand.cs
|   +-- GenerateReportCommandHandler.cs
+-- Queries/
|   +-- GetReportsQuery.cs
+-- DTOs/
    +-- AnalyticsReportDto.cs
```

### Step 5: Create Controller

```
src/GeorgiaERP.Api/Controllers/AnalyticsController.cs
```

### Step 6: Create Migration

```bash
cd src/GeorgiaERP.Api
dotnet ef migrations add AddAnalyticsReports \
  --project ../GeorgiaERP.Infrastructure \
  --startup-project .
```

---

## 6. How to Add a New Entity

Example: Adding a `Warranty` entity to the Products module.

### Step 1: Create the Domain Entity

In `src/GeorgiaERP.Domain/Products/Warranty.cs`:

```csharp
namespace GeorgiaERP.Domain.Products;

public class Warranty : BaseEntity
{
    public Guid ProductId { get; private set; }
    public int DurationMonths { get; private set; }
    public string? Terms { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Warranty Create(Guid productId, int durationMonths, string? terms = null)
    {
        return new Warranty
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            DurationMonths = durationMonths,
            Terms = terms
        };
    }
}
```

### Step 2: Add to DbContext

Add `DbSet<Warranty>` to `IAppDbContext` and `AppDbContext`.

### Step 3: Configure the Entity

Create `WarrantyConfiguration.cs` in the Persistence/Configurations folder.

### Step 4: Create a Migration

```bash
dotnet ef migrations add AddWarranty \
  --project ../GeorgiaERP.Infrastructure \
  --startup-project .
```

---

## 7. How to Add a Command Handler

Example: Adding a `CreateWarrantyCommand`.

### Step 1: Define the Command

In `src/GeorgiaERP.Application/Products/Commands/CreateWarrantyCommand.cs`:

```csharp
using GeorgiaERP.Application.Common;
using MediatR;

namespace GeorgiaERP.Application.Products.Commands;

public record CreateWarrantyCommand(
    Guid ProductId,
    int DurationMonths,
    string? Terms) : IRequest<Result<Guid>>;
```

### Step 2: Create the Handler

In `src/GeorgiaERP.Application/Products/Commands/CreateWarrantyCommandHandler.cs`:

```csharp
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Products.Commands;

public class CreateWarrantyCommandHandler
    : IRequestHandler<CreateWarrantyCommand, Result<Guid>>
{
    private readonly IAppDbContext _dbContext;

    public CreateWarrantyCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> Handle(
        CreateWarrantyCommand request, CancellationToken ct)
    {
        // Validate product exists
        var productExists = await _dbContext.Products
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, ct);

        if (!productExists)
            return Result.Failure<Guid>("Product not found or inactive.");

        // Create entity
        var warranty = Warranty.Create(
            request.ProductId,
            request.DurationMonths,
            request.Terms);

        // Persist
        _dbContext.Warranties.Add(warranty);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(warranty.Id);
    }
}
```

### Key Handler Patterns

- Always validate inputs and return `Result.Failure` for expected errors
- Use `CancellationToken` throughout async calls
- Use `IAppDbContext` for data access (not injecting `AppDbContext` directly)
- Keep handlers focused -- one handler per command
- Log significant operations via `ILogger<T>`
- Use factory methods on domain entities (e.g., `Warranty.Create(...)`)

---

## 8. How to Add a Controller Endpoint

### Step 1: Add the Endpoint

In the relevant controller (e.g., `ProductsController.cs`):

```csharp
[HttpPost("{productId:guid}/warranties")]
public async Task<IActionResult> CreateWarranty(
    Guid productId,
    [FromBody] CreateWarrantyRequest request)
{
    var command = new CreateWarrantyCommand(
        productId,
        request.DurationMonths,
        request.Terms);

    var result = await _mediator.Send(command);

    if (result.IsFailure)
        return ToActionResult(result);

    return Created(
        $"/api/v1/products/{productId}/warranties/{result.Value}",
        new { id = result.Value });
}
```

### Step 2: Define the Request DTO

In the DTOs file or at the bottom of the controller file:

```csharp
public record CreateWarrantyRequest(
    int DurationMonths,
    string? Terms);
```

### Controller Conventions

- Inherit from `ApiControllerBase`
- Use `[Authorize]` at the class level for secured endpoints
- Use `[AllowAnonymous]` for public endpoints
- Use `ToActionResult()` for consistent error mapping
- Use `CreatedAtAction` or `Created` for POST endpoints that create resources
- Accept commands/queries via `[FromBody]` for POST, `[FromQuery]` for GET
- Use route constraints: `{id:guid}` for GUID parameters
- Keep controllers thin -- delegate to MediatR handlers

---

## 9. Coding Conventions

### General

- **Language version:** C# 12 (file-scoped namespaces, primary constructors, records)
- **Target framework:** .NET 9
- **Nullable reference types:** Enabled project-wide
- **Code style:** Follow Microsoft C# coding conventions

### Naming

| Item              | Convention        | Example                     |
|-------------------|-------------------|-----------------------------|
| Class             | PascalCase        | `ProductCategory`           |
| Interface         | IPascalCase       | `IAppDbContext`             |
| Method            | PascalCase        | `CreateProduct`             |
| Property          | PascalCase        | `IsActive`                  |
| Parameter         | camelCase         | `productId`                 |
| Private field     | _camelCase        | `_dbContext`                |
| Constant          | PascalCase        | `VatRate`                   |
| Record            | PascalCase        | `CreateProductCommand`      |
| DTO suffix        | Dto               | `ProductDto`                |
| Command suffix    | Command           | `CreateProductCommand`      |
| Query suffix      | Query             | `GetProductsQuery`          |
| Handler suffix    | CommandHandler / QueryHandler | `CreateProductCommandHandler` |
| Controller suffix | Controller        | `ProductsController`        |

### Bilingual Support

All user-facing text fields support Georgian (Kartvelian script) via `*Ka` properties:

```csharp
public string Name { get; private set; }       // Latin/English
public string? NameKa { get; private set; }    // Georgian (optional)
```

### Dependency Injection

- Register module services via extension methods: `AddApplication()`, `AddInfrastructure()`
- Use constructor injection throughout
- Never resolve services manually from `IServiceProvider` in business logic

### Error Handling

- Use the `Result` pattern for business logic errors
- Throw exceptions only for truly exceptional situations (infrastructure failures)
- The `ExceptionHandlingMiddleware` catches unhandled exceptions and returns 500

### Security

- Validate all inputs at the handler level
- Use `[Authorize]` and `[AllowAnonymous]` attributes appropriately
- Never expose internal IDs, stack traces, or sensitive data in API responses
- Validate TINs, barcodes, and other domain-specific inputs with regex or domain rules
- The `SecurityHeadersMiddleware` adds OWASP-recommended headers to all responses

---

## 10. Testing Guidelines

### Test Project Structure

```
tests/GeorgiaERP.Tests/
+-- Unit/              # Unit tests for domain entities and handlers
+-- Integration/       # Integration tests with real database
+-- Fixtures/          # Shared test setup and configuration
```

### Running Tests

```bash
# Run all tests
dotnet test tests/GeorgiaERP.Tests/GeorgiaERP.Tests.csproj

# Run with coverage
dotnet test tests/GeorgiaERP.Tests/GeorgiaERP.Tests.csproj \
  --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~PricingTests"
```

### Test Requirements

- **Integration tests** require a running PostgreSQL instance
- The CI pipeline provides PostgreSQL as a service container
- Use a dedicated test database (`georgia_erp_test`) to avoid conflicts

### Writing Tests

**Unit test example (domain entity):**

```csharp
[Fact]
public void Create_SetsPropertiesCorrectly()
{
    var product = Product.Create("SKU-001", "Test Product", ...);

    Assert.Equal("SKU-001", product.Sku);
    Assert.True(product.IsActive);
}
```

**Integration test example (command handler):**

```csharp
[Fact]
public async Task CreateProduct_WithValidData_ReturnsSuccess()
{
    // Arrange
    var command = new CreateProductCommand("SKU-001", "Test", ...);

    // Act
    var result = await _mediator.Send(command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
}
```

### Test Conventions

- Use descriptive test names: `MethodName_Scenario_ExpectedResult`
- One assertion per test when practical
- Test both success and failure paths
- Use test fixtures for shared database setup
- Clean up test data in `Dispose` or use transactions

---

## 11. Localization

The platform supports English and Georgian:

- All entity names have optional `*Ka` (Georgian) counterparts
- The Desktop client supports language switching
- API responses include both language variants when available
- User default language is stored in the `DefaultLanguage` field

---

## 12. CI/CD Pipeline

### GitHub Actions Workflow

The CI pipeline (`.github/workflows/ci.yml`) runs on:
- Push to `main` or `claude/**` branches
- Pull requests targeting `main`

### Jobs

**build-test (ubuntu-latest):**
1. Checkout code
2. Setup .NET 9
3. Start PostgreSQL service container
4. Restore NuGet packages for all server projects
5. Build API and Workers in Release mode
6. Run tests with code coverage collection
7. Upload test results as artifacts

**build-desktop (windows-latest):**
1. Checkout code
2. Setup .NET 9
3. Restore and build Desktop project

### Branch Strategy

| Branch Pattern | Purpose |
|---------------|---------|
| `main`        | Production-ready code |
| `claude/**`   | Feature/development branches |
| PR to `main`  | Code review and CI validation |
