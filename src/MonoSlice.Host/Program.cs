using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Modules.Catalog;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Modules.Communications;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Modules.Customization;
using MonoSlice.Modules.Customization.Persistence;
using MonoSlice.Modules.Exams;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Modules.Orders;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Modules.Users;
using MonoSlice.Modules.Users.Persistence;
using MonoSlice.Shared.Infrastructure;
using MonoSlice.Shared.Infrastructure.Mapping;
using MonoSlice.Shared.Infrastructure.Telemetry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration & Environment
builder.Configuration.AddEnvironmentVariables();

// OpenTelemetry (Traces, Metrics, Logging)
builder.Services.AddMonoSliceOpenTelemetry(builder.Configuration);
builder.Logging.AddMonoSliceOtelLogging(builder.Configuration);

// OpenAPI & Scalar Documentation
builder.Services.AddOpenApi();

// Shared Infrastructure (Caching, Messaging, Mediator Behaviors)
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Mapster Mapping
builder.Services.AddMonoSliceMapping(
    typeof(Program).Assembly,
    typeof(UsersModule).Assembly,
    typeof(OrdersModule).Assembly,
    typeof(CatalogModule).Assembly,
    typeof(ExamsModule).Assembly,
    typeof(AssessmentsModule).Assembly,
    typeof(CommunicationsModule).Assembly,
    typeof(CustomizationModule).Assembly);

// Source-Generated Mediator Dispatcher
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

// Domain Modules
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddCatalogModule(builder.Configuration);
builder.Services.AddExamsModule(builder.Configuration);
builder.Services.AddAssessmentsModule(builder.Configuration);
builder.Services.AddCommunicationsModule(builder.Configuration);
builder.Services.AddCustomizationModule(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecks();

// Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

// Auto-migrate databases on startup if configured / in development
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await usersDb.Database.MigrateAsync();

        var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        await paymentsDb.Database.MigrateAsync();

        var coursesDb = scope.ServiceProvider.GetRequiredService<CoursesDbContext>();
        await coursesDb.Database.MigrateAsync();

        var examsDb = scope.ServiceProvider.GetRequiredService<ExamsDbContext>();
        await examsDb.Database.MigrateAsync();

        var assessmentsDb = scope.ServiceProvider.GetRequiredService<AssessmentsDbContext>();
        await assessmentsDb.Database.MigrateAsync();

        var commsDb = scope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();
        await commsDb.Database.MigrateAsync();

        var customDb = scope.ServiceProvider.GetRequiredService<CustomizationDbContext>();
        await customDb.Database.MigrateAsync();
        await CustomizationDbSeeder.SeedDefaultsAsync(customDb, logger);

        logger.LogInformation("Database schemas migrated and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not apply database migrations on startup. Please ensure PostgreSQL is running.");
    }
}

// Global Middlewares (Request Logging, Exception Handling)
app.UseSharedMiddleware();

app.UseCors("DefaultCorsPolicy");
app.UseRateLimiter();

// Custom Composite Authentication (JWT + Cookie) & Authorization
app.UseUsersAuth();

// API Documentation (OpenAPI + Scalar UI)
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "MonoSlice API Reference";
    options.ShowSidebar = true;
});

// Health Check Endpoint
app.MapHealthChecks("/health");

// Root redirect to Scalar UI
app.MapGet("/", () => Results.Redirect("/scalar"))
   .ExcludeFromDescription();

// Module Endpoints
app.MapUsersEndpoints();
app.MapOrdersEndpoints();
app.MapCatalogEndpoints();
app.MapExamsEndpoints();
app.MapAssessmentsEndpoints();
app.MapCommunicationsEndpoints();
app.MapCustomizationEndpoints();

// Realtime SignalR Hubs
app.MapHub<MonoSlice.Modules.Exams.Hubs.ExamHub>("/hubs/exam");
app.MapHub<MonoSlice.Shared.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

app.Run();

// Marker class for WebApplicationFactory in integration tests
public partial class Program { }
