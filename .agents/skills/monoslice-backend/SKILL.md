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
├── AddQuestionValidator.cs        # FluentValidation AbstractValidator<AddQuestionCommand>
└── AddQuestionEndpoint.cs         # FastEndpoint / MapGroup HTTP route
```

> [!IMPORTANT]
> **CQRS Class Convention**: Always define commands and queries as **`public sealed partial class`** (with init-only or get/set properties), **never** as `sealed record`. This ensures proper compatibility with source generators (Sannr/Mediator/Serialization).

### Command Definition Example
```csharp
public sealed partial class AddQuestionCommand : ICommand<ApiResponse<Guid>>
{
    public Guid? BankId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public QuestionType Type { get; init; } = QuestionType.SingleChoice;
    public decimal Points { get; init; } = 1m;
    public string? Explanation { get; init; }
    public List<QuestionOptionDto> Options { get; init; } = [];
}
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

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasDefaultSchema("exams");
}
```

### Adding New Migrations via EF Tools
Always run with the module project as `--project`, Host as `--startup-project`, and explicit `--context`:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/MonoSlice.Modules.Exams/MonoSlice.Modules.Exams.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context ExamsDbContext \
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
