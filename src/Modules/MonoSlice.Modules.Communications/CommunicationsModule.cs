using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Communications.Contracts;
using MonoSlice.Modules.Communications.Features.CloseDiscussionThread;
using MonoSlice.Modules.Communications.Features.CreateAnnouncement;
using MonoSlice.Modules.Communications.Features.CreateDiscussionThread;
using MonoSlice.Modules.Communications.Features.GetAnnouncement;
using MonoSlice.Modules.Communications.Features.GetAnnouncements;
using MonoSlice.Modules.Communications.Features.GetDiscussionThread;
using MonoSlice.Modules.Communications.Features.GetDiscussionThreads;
using MonoSlice.Modules.Communications.Features.PostThreadComment;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Infrastructure.Messaging;

namespace MonoSlice.Modules.Communications;

public static class CommunicationsModule
{
    public static IServiceCollection AddCommunicationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CommunicationsDb") ??
                               configuration.GetConnectionString("Database") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=lms_db;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<CommunicationsDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<CommunicationsDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", CommunicationsDbContext.DefaultSchema);
                });
            });
        }

        // Register module contract API
        services.AddScoped<ICommunicationsModuleApi, CommunicationsModuleApi>();

        // Register integration event handlers
        services.AddTransient<IIntegrationEventHandler<CourseDeletedIntegrationEvent>, EventHandlers.CourseDeletedIntegrationEventHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapCommunicationsEndpoints(this IEndpointRouteBuilder app)
    {
        var dispatcher = app.ServiceProvider.GetService<IIntegrationEventDispatcher>();
        dispatcher?.RegisterEvent<CourseDeletedIntegrationEvent>();

        var commsGroup = app.MapGroup("/api/v1/communications")
            .WithTags("Communications");

        // Announcements
        commsGroup.MapCreateAnnouncementEndpoint();
        commsGroup.MapGetAnnouncementsEndpoint();
        commsGroup.MapGetAnnouncementByIdEndpoint();

        // Discussion Threads & Comments
        commsGroup.MapCreateDiscussionThreadEndpoint();
        commsGroup.MapGetDiscussionThreadsEndpoint();
        commsGroup.MapGetDiscussionThreadByIdEndpoint();
        commsGroup.MapPostThreadCommentEndpoint();
        commsGroup.MapCloseDiscussionThreadEndpoint();

        return app;
    }
}
