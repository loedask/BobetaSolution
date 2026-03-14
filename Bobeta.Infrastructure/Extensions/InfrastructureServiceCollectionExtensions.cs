using Bobeta.Application.Interfaces;
using Bobeta.Infrastructure.External;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Infrastructure.Extensions;

/// <summary>Registers infrastructure services (e.g. Mobile Money placeholder).</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Adds IMobileMoneyService implementation (placeholder by default).</summary>
    public static IServiceCollection AddBobetaInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IMobileMoneyService, PlaceholderMobileMoneyService>();
        return services;
    }
}
