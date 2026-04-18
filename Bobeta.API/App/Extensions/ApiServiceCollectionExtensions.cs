using System.Text;
using Bobeta.API.App.Services;
using Bobeta.Application.Extensions;
using Bobeta.Application.Interfaces;
using Bobeta.Identity.Extensions;
using Bobeta.Infrastructure.Extensions;
using Bobeta.Persistence.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.SignalR;
using Microsoft.IdentityModel.Tokens;

namespace Bobeta.API.App.Extensions;

/// <summary>Registers all Bobeta services (persistence, application, identity, infrastructure), JWT auth, and SignalR.</summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>Name used by Azure App Service flexible PostgreSQL / portal templates for the DB connection string.</summary>
    public const string AzurePostgresConnectionStringName = "AZURE_POSTGRESQL_CONNECTIONSTRING";

    /// <summary>Adds persistence, application, identity, infrastructure, JWT bearer auth (including SignalR query token), and SignalR (Azure SignalR Service when <c>Azure:SignalR:ConnectionString</c> is set).</summary>
    public static IServiceCollection AddBobetaServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Prefer Azure App Service connection string name so production overrides packaged appsettings.json.
        var connectionString = configuration.GetConnectionString(AzurePostgresConnectionStringName)
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=Bobeta;Username=postgres;Password=postgres";
        services.AddBobetaPersistence(connectionString);
        services.AddBobetaApplication();
        services.AddScoped<IGameSessionNotifier, GameSessionSignalRNotifier>();
        services.AddBobetaIdentity();
        services.AddBobetaInfrastructure(configuration);

        var key = configuration["Jwt:Key"] ?? "BobetaDefaultSecretKeyForJwtSigningThatIsLongEnough";
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "Bobeta",
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"] ?? "Bobeta",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                            ctx.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        // Local dev: in-process SignalR. Production (or any host with scale-out): set Azure:SignalR:ConnectionString
        // (e.g. App Service application setting Azure__SignalR__ConnectionString) so hubs use Azure SignalR Service.
        var signalR = services.AddSignalR();
        var azureSignalRConnectionString = configuration["Azure:SignalR:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(azureSignalRConnectionString))
            signalR.AddAzureSignalR(azureSignalRConnectionString);

        return services;
    }
}
