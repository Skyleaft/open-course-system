# MonoSlice Backend Developer Handbook & Skills Reference

This document serves as the master engineering cheatsheet and architectural handbook for developing, testing, and maintaining features in the **MonoSlice .NET 10 Modular Monolith** backend.

---

## 1. Architectural Principles

1. **Modular Monolith**:
   - Organized into 6 decoupled domain modules under `src/Modules/`:
     - `MonoSlice.Modules.Users` (Schema: `identity`)
     - `MonoSlice.Modules.Orders` (Schema: `payments`)
     - `MonoSlice.Modules.Catalog` (Schema: `courses`)
     - `MonoSlice.Modules.Exams` (Schema: `exams`)
     - `MonoSlice.Modules.Assessments` (Schema: `assessments`)
     - `MonoSlice.Modules.Communications` (Schema: `communications`)
   - Cross-module communication strictly adheres to:
     - **Synchronous Contracts:** Interfaces in `MonoSlice.Shared.Abstractions.Contracts` (`ICoursesModuleApi`, etc.).
     - **Asynchronous Events:** Redis Streams with Consumer Groups via `IEventStreamPublisher`.
   - **Direct database access across module boundaries is strictly prohibited.**

2. **Vertical Slice Architecture**:
   - Each business feature resides in its self-contained slice (`Features/<FeatureName>/`):
     - `*Command.cs` / `*Query.cs`
     - `*CommandHandler.cs` / `*QueryHandler.cs`
     - `*Validator.cs`
     - `*Endpoint.cs`

3. **Performance & Resilience**:
   - **Zero DB Writes During Active Exam**: Realtime answers are cached in Redis (`exam_answers:{submissionId}`) and finalized in bulk upon submission.
   - **Optimistic Concurrency**: Tracked via `xmin` shadow tokens on aggregates.
   - **Distributed Locks**: Redis RedLock pattern via `IDistributedLockProvider`.

---

## 2. Common CLI Runbooks & EF Tools

### A. Solution Build & Test
```bash
# Build the entire solution
dotnet build

# Run all test suites
dotnet test

# Run a specific module test project
dotnet test tests/MonoSlice.Modules.Exams.Tests/MonoSlice.Modules.Exams.Tests.csproj
```

### B. Entity Framework Migrations
Each module manages its own independent schema migrations.

```bash
# 1. Users Module (identity schema)
dotnet ef migrations add <MigrationName> \
  --project src/Modules/MonoSlice.Modules.Users/MonoSlice.Modules.Users.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context UsersDbContext \
  --output-dir Persistence/Migrations

# 2. Orders Module (payments schema)
dotnet ef migrations add <MigrationName> \
  --project src/Modules/MonoSlice.Modules.Orders/MonoSlice.Modules.Orders.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context PaymentsDbContext \
  --output-dir Persistence/Migrations

# 3. Catalog Module (courses schema)
dotnet ef migrations add <MigrationName> \
  --project src/Modules/MonoSlice.Modules.Catalog/MonoSlice.Modules.Catalog.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context CoursesDbContext \
  --output-dir Persistence/Migrations

# 4. Exams Module (exams schema)
dotnet ef migrations add <MigrationName> \
  --project src/Modules/MonoSlice.Modules.Exams/MonoSlice.Modules.Exams.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context ExamsDbContext \
  --output-dir Persistence/Migrations

# 5. Assessments Module (assessments schema)
dotnet ef migrations add <MigrationName> \
  --project src/Modules/MonoSlice.Modules.Assessments/MonoSlice.Modules.Assessments.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context AssessmentsDbContext \
  --output-dir Persistence/Migrations

# 6. Communications Module (communications schema)
dotnet ef migrations add <MigrationName> \
  --project src/Modules/MonoSlice.Modules.Communications/MonoSlice.Modules.Communications.csproj \
  --startup-project src/MonoSlice.Host/MonoSlice.Host.csproj \
  --context CommunicationsDbContext \
  --output-dir Persistence/Migrations
```

---

## 3. Standard Code Recipes

### Recipe 1: Defining a Command & Handler
> [!IMPORTANT]
> Always define CQRS commands and queries as **`public sealed partial class`** (never `sealed record`) for seamless source generator and deserializer compatibility.

```csharp
namespace MonoSlice.Modules.Exams.Features.CreateExam;

public sealed partial class CreateExamCommand : ICommand<ApiResponse<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public QuizMode Mode { get; init; } = QuizMode.RealExam;
    public int DurationMinutes { get; init; } = 60;
    public decimal PassingScore { get; init; } = 70.00m;
}

public sealed class CreateExamCommandHandler : ICommandHandler<CreateExamCommand, ApiResponse<Guid>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateExamCommandHandler(ExamsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<Guid>> Handle(CreateExamCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiResponse.Fail<Guid>("Unauthorized.", 401);

        var exam = QuizExam.Create(
            instructorId: _currentUser.UserId,
            title: command.Title,
            description: command.Description,
            mode: command.Mode,
            durationMinutes: command.DurationMinutes,
            passingScore: command.PassingScore);

        await _dbContext.Exams.AddAsync(exam, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(exam.Id, "Exam created successfully.", 201);
    }
}
```

### Recipe 2: Writing a Domain Unit Test
```csharp
using MonoSlice.Modules.Exams.Domain;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class QuizExamDomainTests
{
    [Fact]
    public void AddSection_ShouldAddSectionWithQuestionBankReference()
    {
        var exam = QuizExam.Create(Guid.NewGuid(), "Sample Quiz", "Desc", QuizMode.RealExam, 60, 75m);
        var questionBankId = Guid.NewGuid();

        var section = exam.AddSection(questionBankId, "Core Algorithms", pointsOverride: 10m, questionCount: 5);

        Assert.Single(exam.Sections);
        Assert.Equal(questionBankId, section.QuestionBankId);
        Assert.Equal(10m, section.PointsOverride);
        Assert.Equal(5, section.QuestionCount);
    }
}
```

---

## 4. Module Registry & DI Configuration

Each module registers its own dependencies and routes inside a `<ModuleName>Module.cs` class implementing `IModule`:

```csharp
public sealed class ExamsModule : IModule
{
    public string Name => "Exams";

    public IServiceCollection RegisterModule(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ExamsDbContext>((sp, options) =>
        {
            var conn = configuration.GetConnectionString("Database");
            options.UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "exams");
            });
        });

        services.AddScoped<IExamFinalizerService, ExamFinalizerService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/exams")
            .WithTags("Exams");

        // Map slice endpoints here
        return endpoints;
    }
}
```
