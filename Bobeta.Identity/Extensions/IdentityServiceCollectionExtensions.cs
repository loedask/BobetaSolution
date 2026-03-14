using Bobeta.Application.Interfaces;
using Bobeta.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Identity.Extensions;

/// <summary>Registers identity services: OTP, JWT generation, and auth (send OTP, verify, register).</summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>Adds OtpService, IJwtTokenService, and IAuthService implementations to the container.</summary>
    public static IServiceCollection AddBobetaIdentity(this IServiceCollection services)
    {
        services.AddScoped<OtpService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
