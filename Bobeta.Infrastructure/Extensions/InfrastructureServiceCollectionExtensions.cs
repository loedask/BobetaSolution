using Bobeta.Application.Interfaces;
using Bobeta.Infrastructure.External;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBobetaInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IMobileMoneyService, PlaceholderMobileMoneyService>();
        return services;
    }
}
