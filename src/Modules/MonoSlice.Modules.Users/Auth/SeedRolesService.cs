using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Users.Domain;

namespace MonoSlice.Modules.Users.Auth;

public sealed class SeedRolesService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SeedRolesService> _logger;

    public SeedRolesService(IServiceProvider serviceProvider, ILogger<SeedRolesService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();
            if (roleManager is null) return;

            string[] defaultRoles = ["Student", "Instructor", "Admin", "Proctor", "User", "Manager"];

            foreach (var roleName in defaultRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationRole(roleName, $"Default {roleName} role");
                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Seeded default role: {RoleName}", roleName);
                    }
                    else
                    {
                        _logger.LogError("Failed to seed role {RoleName}: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not seed default roles on startup. Ensure database is accessible.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
