using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Services;
using Bobeta.Client.Services.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Bobeta.Client;

using ApiClient = Services.Base.Client;

/// <summary>Dependency injection registration for the Bobeta API client layer.</summary>
public static class ServiceRegistration
{
    /// <summary>Named HttpClient key for the Bobeta API.</summary>
    public const string HttpClientName = "Bobeta";

    /// <summary>
    /// Registers the Bobeta API client: typed HttpClient (IClient), optional bearer token handler, AutoMapper, and feature services.
    /// Configure the API base URL via <paramref name="configureHttpClient"/> (e.g. client.BaseAddress = new Uri(options.BaseUrl)).
    /// When <paramref name="useBearerToken"/> is true, register <see cref="Contracts.IAccessTokenProvider"/> in the host so requests include the bearer token.
    /// </summary>
    public static IServiceCollection AddBobetaClient(this IServiceCollection services, Action<HttpClient>? configureHttpClient = null, bool useBearerToken = false)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(ServiceRegistration).Assembly);
        });

        var httpClientBuilder = services.AddHttpClient<IClient, ApiClient>(HttpClientName, client =>
        {
            configureHttpClient?.Invoke(client);
        });

        if (useBearerToken)
            httpClientBuilder.AddHttpMessageHandler<BearerTokenHandler>();

        services.AddScoped<IGameService>(sp => new GameService(
            sp.GetRequiredService<IClient>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName)));

        services.AddScoped<WalletService>(sp => new WalletService(
            sp.GetRequiredService<IClient>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName)));

        services.AddScoped<AuthService>(sp => new AuthService(
            sp.GetRequiredService<IClient>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName)));

        services.AddScoped<GamePlayService>(sp => new GamePlayService(
            sp.GetRequiredService<IClient>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName)));

        services.AddScoped<HistoryService>(sp => new HistoryService(
            sp.GetRequiredService<IClient>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName)));

        return services;
    }
}
