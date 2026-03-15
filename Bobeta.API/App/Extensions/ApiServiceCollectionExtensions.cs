using System.Text;
using Bobeta.Application.Extensions;
using Bobeta.Identity.Extensions;
using Bobeta.Infrastructure.Extensions;
using Bobeta.Persistence.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Bobeta.API.App.Extensions;

/// <summary>Registers all Bobeta services (persistence, application, identity, infrastructure), JWT auth, and SignalR.</summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>Adds persistence, application, identity, infrastructure, JWT bearer auth (including SignalR query token), and SignalR.</summary>
    public static IServiceCollection AddBobetaServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=Bobeta;Username=postgres;Password=postgres";
        services.AddBobetaPersistence(connectionString);
        services.AddBobetaApplication();
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

        services.AddSignalR();
        return services;
    }
}
