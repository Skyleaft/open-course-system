using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Assessments.Contracts;
using MonoSlice.Modules.Assessments.EventHandlers;
using MonoSlice.Modules.Assessments.Features.Admin.GetDeadLetters;
using MonoSlice.Modules.Assessments.Features.Admin.RedriveDeadLetter;
using MonoSlice.Modules.Assessments.Features.GetCertificate;
using MonoSlice.Modules.Assessments.Features.GetMyCertificates;
using MonoSlice.Modules.Assessments.Features.IssueCertificate;
using MonoSlice.Modules.Assessments.Features.VerifyCertificate;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Modules.Assessments.Workers;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Infrastructure.Messaging;

namespace MonoSlice.Modules.Assessments;

public static class AssessmentsModule
{
    public static IServiceCollection AddAssessmentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AssessmentsDb") ??
                               configuration.GetConnectionString("ExamsDb") ??
                               configuration.GetConnectionString("Database") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=lms_db;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AssessmentsDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<AssessmentsDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", AssessmentsDbContext.DefaultSchema);
                });
            });
        }

        // Register module contract API
        services.AddScoped<IAssessmentsModuleApi, AssessmentsModuleApi>();

        // Register background worker for stream processing
        services.AddHostedService<GradingBackgroundWorker>();

        // Register integration event handlers
        services.AddTransient<IIntegrationEventHandler<ExamDeletedIntegrationEvent>, ExamDeletedIntegrationEventHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapAssessmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var dispatcher = app.ServiceProvider.GetService<IIntegrationEventDispatcher>();
        dispatcher?.RegisterEvent<ExamDeletedIntegrationEvent>();

        var certsGroup = app.MapGroup("/api/v1/certificates")
            .WithTags("Certificates");

        certsGroup.MapVerifyCertificateEndpoint();
        certsGroup.MapGetMyCertificatesEndpoint();
        certsGroup.MapGetCertificateEndpoint();
        certsGroup.MapIssueCertificateEndpoint();

        var adminDlqGroup = app.MapGroup("/api/v1/admin/assessments")
            .WithTags("Admin Assessments");

        adminDlqGroup.MapGetDeadLettersEndpoint();
        adminDlqGroup.MapRedriveDeadLetterEndpoint();

        return app;
    }
}
