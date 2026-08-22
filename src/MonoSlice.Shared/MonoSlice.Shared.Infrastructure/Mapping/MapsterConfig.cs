using System.Reflection;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace MonoSlice.Shared.Infrastructure.Mapping;

public static class MapsterConfig
{
    public static IServiceCollection AddMonoSliceMapping(this IServiceCollection services, params Assembly[] assemblies)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Default.PreserveReference(true);

        if (assemblies.Length > 0)
        {
            config.Scan(assemblies);
        }

        services.AddSingleton(config);
        return services;
    }
}
