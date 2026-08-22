using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Catalog.Contracts;
using MonoSlice.Modules.Catalog.EventHandlers;
using MonoSlice.Modules.Catalog.Features.AddLesson;
using MonoSlice.Modules.Catalog.Features.AddSection;
using MonoSlice.Modules.Catalog.Features.CreateAssignment;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Modules.Catalog.Features.EnrollCourse;
using MonoSlice.Modules.Catalog.Features.GetCourse;
using MonoSlice.Modules.Catalog.Features.ListCourses;
using MonoSlice.Modules.Catalog.Features.PublishCourse;
using MonoSlice.Modules.Catalog.Features.SubmitAssignment;
using MonoSlice.Modules.Catalog.Features.UpdateCourse;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Infrastructure.Messaging;

namespace MonoSlice.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CoursesDb") ??
                               configuration.GetConnectionString("CatalogDb") ??
                               configuration.GetConnectionString("Database") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=lms_db;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<CoursesDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<CoursesDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", CoursesDbContext.DefaultSchema);
                });
            });
        }

        // Register module contract API for synchronous inter-module communication
        services.AddScoped<ICoursesModuleApi, CoursesModuleApi>();

        // Register integration event handlers for asynchronous inter-module messaging
        services.AddTransient<IIntegrationEventHandler<OrderPaidIntegrationEvent>, OrderPaidIntegrationEventHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var dispatcher = app.ServiceProvider.GetService<IIntegrationEventDispatcher>();
        dispatcher?.RegisterEvent<OrderPaidIntegrationEvent>();

        var coursesV1Group = app.MapGroup("/api/v1/courses")
            .WithTags("Courses");

        coursesV1Group.MapCreateCourseEndpoint();
        coursesV1Group.MapListCoursesEndpoint();
        coursesV1Group.MapGetCourseEndpoint();
        coursesV1Group.MapUpdateCourseEndpoint();
        coursesV1Group.MapPublishCourseEndpoint();
        coursesV1Group.MapEnrollCourseEndpoint();
        coursesV1Group.MapAddSectionEndpoint();
        coursesV1Group.MapAddLessonEndpoint();
        coursesV1Group.MapCreateAssignmentEndpoint();
        coursesV1Group.MapSubmitAssignmentEndpoint();

        return app;
    }
}
