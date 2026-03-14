using Bobeta.Application.Interfaces;
using Bobeta.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Identity.Extensions;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddBobetaIdentity(this IServiceCollection services)
    {
        services.AddScoped<OtpService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
