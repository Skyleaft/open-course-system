using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MonoSlice.Modules.Users.Auth;
using MonoSlice.Modules.Users.Contracts;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Modules.Users.Features.AssignRole;
using MonoSlice.Modules.Users.Features.GetProfile;
using MonoSlice.Modules.Users.Features.GoogleAuth;
using MonoSlice.Modules.Users.Features.Login;
using MonoSlice.Modules.Users.Features.Logout;
using MonoSlice.Modules.Users.Features.RefreshToken;
using MonoSlice.Modules.Users.Features.Register;
using MonoSlice.Modules.Users.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authSection = configuration.GetSection(AuthSettings.SectionName);
        services.Configure<AuthSettings>(authSection);
        var authSettings = authSection.Get<AuthSettings>() ?? new AuthSettings();

        var connectionString = configuration.GetConnectionString("UsersDb") ??
                               configuration.GetConnectionString("Database") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=lms_db;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<UsersDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<UsersDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", UsersDbContext.DefaultSchema);
                });
            });
        }

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredUniqueChars = 0;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<UsersDbContext>()
        .AddDefaultTokenProviders();

        services.AddAuthentication()
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authSettings.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = authSettings.JwtAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.JwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = authSettings.CookieName;
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(authSettings.RefreshTokenExpiryDays);
            options.SlidingExpiration = true;
        });

        services.AddAuthorization();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<IUsersModuleApi, UsersModuleApi>();
        services.AddScoped<IIdentityModuleApi, UsersModuleApi>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddHostedService<SeedRolesService>();

        return services;
    }

    public static IApplicationBuilder UseUsersAuth(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseMiddleware<CompositeAuthMiddleware>();
        app.UseMiddleware<SessionGuardMiddleware>();
        app.UseAuthorization();
        return app;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var authV1Group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        authV1Group.MapRegisterEndpoint();
        authV1Group.MapLoginEndpoint();
        authV1Group.MapGoogleAuthEndpoint();
        authV1Group.MapRefreshTokenEndpoint();
        authV1Group.MapLogoutEndpoint();
        authV1Group.MapGetProfileEndpoint();
        authV1Group.MapAssignRoleEndpoint();

        return app;
    }
}
