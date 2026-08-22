using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Shared.Abstractions.Persistence;

namespace MonoSlice.Shared.Infrastructure.Persistence;

/// <summary>
/// Extension methods for registering Dapper and database connection factories.
/// </summary>
public static class DapperExtensions
{
    public static IServiceCollection AddDapper(this IServiceCollection services, IConfiguration configuration)
    {
        // Register connection factory as singleton/scoped
        services.AddSingleton<ISqlConnectionFactory>(_ => new NpgsqlConnectionFactory(configuration));
        return services;
    }
}
