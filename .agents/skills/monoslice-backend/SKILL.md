---
name: monoslice-backend
description: >-
  Expert engineering playbook and architecture cheatsheet for the MonoSlice .NET 10
  Modular Monolith backend (Vertical Slice, CQRS, EF Core Multi-Schema, Redis Streams,
  FastEndpoints, and Domain-Driven Design).
metadata:
  version: 1.0.0
  source: workspace
---

# MonoSlice Backend Engineering Skill

Guide and standard conventions for authoring, refactoring, and maintaining features in the **MonoSlice** .NET 10 Modular Monolith backend.

---

## 1. Solution Architecture & Module Structure

MonoSlice is structured as a **Modular Monolith** using **Vertical Slice Architecture** and **Domain-Driven Design (DDD)**.

```
src/
├── MonoSlice.Host/                   # Single entry point / ASP.NET Core Web Host
├── MonoSlice.Shared/
│   ├── MonoSlice.Shared.Abstractions/# Base entities, CQRS interfaces, Result envelopes, Interfaces
│   └── MonoSlice.Shared.Infrastructure/# EF Core, Redis Streams, Distributed Lock, S3, Email
└── Modules/
    ├── MonoSlice.Modules.Users/      # Identity & RBAC (schema: identity)
    ├── MonoSlice.Modules.Orders/     # Checkout & Mock Payments (schema: payments)
    ├── MonoSlice.Modules.Catalog/    # Course Syllabus & Materials (schema: courses)
    ├── MonoSlice.Modules.Exams/      # Exam Engine, Question Bank & Proctoring (schema: exams)
    ├── MonoSlice.Modules.Assessments/# Final Grades & Certificates (schema: assessments)
    └── MonoSlice.Modules.Communications/# Announcements & Discussions (schema: communications)
```

---

## 2. Module Boundary & Cross-Module Rules

1. **Direct DbContext Access across modules is Strictly FORBIDDEN**:
   - Module A may NEVER inject or query Module B's `DbContext`.
2. **Synchronous Cross-Module Calls**:
   - Use strongly-typed API contracts defined in `MonoSlice.Shared.Abstractions.Contracts` (e.g. `ICoursesModuleApi`, `IAssessmentsModuleApi`).
3. **Asynchronous Cross-Module Events**:
   - Publish integration events via `IEventStreamPublisher` to Redis Streams (`stream:*-events`).
   - Background workers implement `BackgroundService` to consume consumer groups.

---

## 3. Vertical Slice & CQRS Conventions

Every feature slice lives in `Features/<FeatureName>/` within its module:

```
Features/AddQuestion/
├── AddQuestionCommand.cs           # Command: public sealed partial class AddQuestionCommand : ICommand<ApiResponse<Guid>>
├── AddQuestionCommandHandler.cs   # ICommandHandler<AddQuestionCommand, ApiResponse<Guid>>
└── AddQuestionEndpoint.cs         # FastEndpoint / MapGroup HTTP route
```

> [!IMPORTANT]
> **CQRS Class & Validation Convention**:
> 1. Always define CQRS commands and queries as **`public sealed partial class`** (with init-only or get/set properties), **NEVER** as `record` or `sealed record`. This ensures proper compatibility with compile-time source generators (Sannr validation generators, Mediator pipelines, and JSON source generators).
> 2. Always use **Sannr** for input validation directly on command/query properties (`using Sannr;`). Use validation attributes such as `[Required]`, `[Range(min, max)]`, `[StringLength(max, MinimumLength = min)]`, and `[EmailAddress]`.

### Command Definition Example (with Sannr Validation)
```csharp
using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.AddQuestion;

public sealed partial class AddQuestionCommand : ICommand<ApiResponse<Guid>>
{
    [Required]
    public Guid BankId { get; init; }

    [Required]
    [StringLength(2000, MinimumLength = 3)]
    public string QuestionText { get; init; } = string.Empty;

    public QuestionType Type { get; init; } = QuestionType.SingleChoice;

    [Range(0.25, 1000)]
    public decimal Points { get; init; } = 1m;

    public string? Explanation { get; init; }
    public List<QuestionOptionDto> Options { get; init; } = [];
}
```

### Query Definition Example (with Sannr Validation)
```csharp
using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ListExams;

public sealed partial class ListExamsQuery : IQuery<ApiResponse<PaginatedList<ExamSummaryDto>>>
{
    public QuizMode? Mode { get; init; }
    public bool? IsPublished { get; init; }
    public string? SearchTerm { get; init; }

    [Range(1, int.MaxValue)]
    public int PageIndex { get; init; } = 1;

    [Range(1, 1000)]
    public int PageSize { get; init; } = 20;
}
```

### Supported Sannr Validation Attributes

| Attribute | Description | Example |
| :--- | :--- | :--- |
| `[Required]` | Ensures field is not null/empty | `[Required(ErrorMessage = "Name is required")]` |
| `[StringLength]` | Validates string length | `[StringLength(50, MinimumLength = 2)]` |
| `[Range]` | Numeric range validation | `[Range(18, 65)]` |
| `[EmailAddress]` | Email format validation | `[EmailAddress]` |
| `[Url]` | URL format validation | `[Url]` |
| `[Phone]` | Phone number validation | `[Phone]` |
| `[CreditCard]` | Credit card format | `[CreditCard]` |
| `[FileExtensions]` | File extension validation | `[FileExtensions(Extensions = "pdf,docx")]` |
| `[FutureDate]` | Date must be in future | `[FutureDate]` |
| `[AllowedValues]` | Whitelist validation | `[AllowedValues("Active", "Inactive")]` |
| `[RequiredIf]` | Conditional required | `[RequiredIf("IsEmployed", true)]` |
| `[ConditionalRange]` | Conditional range | `[ConditionalRange("MinValue", "MaxValue")]` |

### Sanitization Attributes
Automatically clean and transform input data:

```csharp
public class UserProfile
{
    [Sanitize(Trim = true, ToUpper = true)]
    public string? Username { get; set; }
    
    [Sanitize(ToLower = true)]
    public string? Email { get; set; }
}
```

### Custom Validators
Implement complex business logic with async support:

```csharp
[CustomValidator(typeof(UserValidator))]
public class User
{
    public string? Username { get; set; }
    public string? Email { get; set; }
}

public class UserValidator : SannrValidator<User>
{
    public override async Task<ValidationResult> ValidateAsync(User instance, ValidationContext context)
    {
        var result = ValidationResult.Success();
        
        // Custom async validation logic
        if (await IsUsernameTaken(instance.Username))
        {
            result.Errors.Add(new ValidationError("Username", "Username already exists"));
        }
        
        return result;
    }
}
```

### Validation Groups
Control which validations run in different scenarios:

```csharp
public class Order
{
    [Required]
    public string? CustomerName { get; set; }
    
    [Required(Group = "Shipping")]
    public string? ShippingAddress { get; set; }
    
    [Required(Group = "Billing")]
    public string? BillingAddress { get; set; }
}

// Validate only shipping fields
var result = await validator.ValidateAsync(order, group: "Shipping");
```

### Error Message Resources
Support for localized error messages:

```csharp
[Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(Resources.Validation))]
public string? Name { get; set; }
```

### Handler Pattern
```csharp
public sealed class AddQuestionCommandHandler : ICommandHandler<AddQuestionCommand, ApiResponse<Guid>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public AddQuestionCommandHandler(ExamsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<Guid>> Handle(AddQuestionCommand command, CancellationToken cancellationToken)
    {
        var bank = await _dbContext.QuestionBanks
            .Include(b => b.Questions)
            .FirstOrDefaultAsync(b => b.Id == command.BankId, cancellationToken);

        if (bank is null)
            return ApiResponse.Fail<Guid>("Question Bank pool not found.", 404);

        var q = bank.AddQuestion(command.QuestionText, command.Type, command.Points, command.Explanation, command.Options);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(q.Id, "Question created successfully.", 201);
    }
}
```

### Endpoint Pattern & Role-Based Authorization Policy

> [!IMPORTANT]
> **MANDATORY ROLE-BASED AUTHORIZATION POLICY ON ENDPOINTS**:
> Whenever securing an endpoint with `.RequireAuthorization()`, **NEVER** use parameterless `.RequireAuthorization()`. Always specify explicit role-based requirements via policy lambda `.RequireAuthorization(policy => policy.RequireRole(...))`:
> - **Management & Authoring Endpoints (Instructor/Admin)**:
>   ```csharp
>   .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
>   ```
> - **System Admin / Platform Governance Endpoints**:
>   ```csharp
>   .RequireAuthorization(policy => policy.RequireRole("Admin"));
>   ```
> - **Student & Candidate Action Endpoints**:
>   ```csharp
>   .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
>   ```
> - **Proctor / Monitor Endpoints**:
>   ```csharp
>   .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin", "Proctor"));
>   ```
>
> Public endpoints must be explicitly marked with `.AllowAnonymous()`. Never leave an endpoint with ambiguous or unconstrained authentication.

---

## 4. Entity Framework Core & PostgreSQL Multi-Schema Conventions

### Schema Mapping
Each module has its own PostgreSQL schema:
- `identity` -> `UsersDbContext`
- `payments` -> `PaymentsDbContext`
- `courses`  -> `CoursesDbContext`
- `exams`    -> `ExamsDbContext`
- `assessments` -> `AssessmentsDbContext`
- `communications` -> `CommunicationsDbContext`
- `customization` -> `CustomizationDbContext`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasDefaultSchema("customization");
}
```

### Adding New Migrations via EF Tools (Mandatory on Any Schema Change)

> [!IMPORTANT]
> **MANDATORY EF CORE MIGRATION RULE**:
> Whenever creating a new `DbContext`, adding new entities, or modifying entity configurations/table structures in any module, you **MUST IMMEDIATELY** generate the corresponding EF Core migration files before finalizing your changes. Never leave a DbContext without its migration snapshot.

Always run with the module project as `--project`, Host as `--startup-project`, and explicit `--context`:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/MonoSlice.Modules.<ModuleName>/MonoSlice.Modules.<ModuleName>.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context <Module>DbContext \
  --output-dir Persistence/Migrations
```

**Example for Customization**:
```bash
dotnet ef migrations add InitialCustomizationMigration \
  --project src/Modules/MonoSlice.Modules.Customization/MonoSlice.Modules.Customization.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context CustomizationDbContext \
  --output-dir Persistence/Migrations
```

---

## 5. Caching & Zero-DB-Write Strategy (Exams Runtime)

During active exam attempts:
- **No Database Writes**: All student answer ticks are debounced into Redis via `ICacheService`:
  `exam_answers:{submissionId}` (Key).
- **Exam Completion / Finalization**:
  The `ExamFinalizerService` flushes cached answers in one single transaction, grades questions based on section `PointsOverride ?? BankQuestion.Points`, marks status as `Completed`, and publishes `ExamSubmittedIntegrationEvent` to Redis Streams.

---

## 6. Testing Strategy

- **Domain Tests**: Test entities & aggregates without databases in `tests/MonoSlice.Modules.*.Tests/`.
- **Handler Tests**: Use `UseInMemoryDatabase(Guid.CreateVersion7().ToString())` and NSubstitute for mocked interfaces.
- **Run Tests**:
  ```bash
  dotnet test
  ```
