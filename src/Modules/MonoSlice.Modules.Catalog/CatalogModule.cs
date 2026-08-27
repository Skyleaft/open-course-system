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
using MonoSlice.Modules.Catalog.Features.AttachExam;
using MonoSlice.Modules.Catalog.Features.CreateAssignment;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Modules.Catalog.Features.DeleteCourse;
using MonoSlice.Modules.Catalog.Features.DeleteLesson;
using MonoSlice.Modules.Catalog.Features.DeleteSection;
using MonoSlice.Modules.Catalog.Features.DetachExam;
using MonoSlice.Modules.Catalog.Features.EnrollCourse;
using MonoSlice.Modules.Catalog.Features.GetCourse;
using MonoSlice.Modules.Catalog.Features.GetLesson;
using MonoSlice.Modules.Catalog.Features.ListCourses;
using MonoSlice.Modules.Catalog.Features.PublishCourse;
using MonoSlice.Modules.Catalog.Features.PresignAssignmentUpload;
using MonoSlice.Modules.Catalog.Features.PresignCourseThumbnail;
using MonoSlice.Modules.Catalog.Features.SubmitAssignment;
using MonoSlice.Modules.Catalog.Features.UpdateCourse;
using MonoSlice.Modules.Catalog.Features.UpdateLesson;
using MonoSlice.Modules.Catalog.Features.UpdateSection;
using MonoSlice.Modules.Catalog.Features.CompleteLesson;
using MonoSlice.Modules.Catalog.Features.GetCourseProgress;
using MonoSlice.Modules.Catalog.Features.GetEnrolledCourses;
using MonoSlice.Modules.Catalog.Features.GetCourseEnrollments;
using MonoSlice.Modules.Catalog.Features.AdminEnrollStudent;
using MonoSlice.Modules.Catalog.Features.AdminRemoveEnrollment;
using MonoSlice.Modules.Catalog.Features.Analytics.GetCourseAnalytics;
using MonoSlice.Modules.Catalog.Features.Dashboard.GetStudentDashboardOverview;
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
        coursesV1Group.MapPresignCourseThumbnailEndpoint();
        coursesV1Group.MapListCoursesEndpoint();
        coursesV1Group.MapGetCourseEndpoint();
        coursesV1Group.MapUpdateCourseEndpoint();
        coursesV1Group.MapDeleteCourseEndpoint();
        coursesV1Group.MapPublishCourseEndpoint();
        coursesV1Group.MapEnrollCourseEndpoint();
        coursesV1Group.MapGetEnrolledCoursesEndpoint();
        coursesV1Group.MapGetCourseProgressEndpoint();
        coursesV1Group.MapCompleteLessonEndpoint();
        coursesV1Group.MapGetCourseEnrollmentsEndpoint();
        coursesV1Group.MapAdminEnrollStudentEndpoint();
        coursesV1Group.MapAdminRemoveEnrollmentEndpoint();
        coursesV1Group.MapAddSectionEndpoint();
        coursesV1Group.MapUpdateSectionEndpoint();
        coursesV1Group.MapDeleteSectionEndpoint();
        coursesV1Group.MapGetLessonEndpoint();
        coursesV1Group.MapAddLessonEndpoint();
        coursesV1Group.MapUpdateLessonEndpoint();
        coursesV1Group.MapDeleteLessonEndpoint();
        coursesV1Group.MapCreateAssignmentEndpoint();
        coursesV1Group.MapPresignAssignmentUploadEndpoint();
        coursesV1Group.MapSubmitAssignmentEndpoint();
        coursesV1Group.MapAttachExamEndpoint();
        coursesV1Group.MapDetachExamEndpoint();

        var dashboardGroup = app.MapGroup("/api/v1")
            .WithTags("Dashboard");
        dashboardGroup.MapGetCourseAnalyticsEndpoint();
        dashboardGroup.MapGetStudentDashboardOverviewEndpoint();

        return app;
    }
}
