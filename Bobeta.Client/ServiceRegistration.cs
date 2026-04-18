using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Services;
using Bobeta.Client.Services.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Bobeta.Client;

using ApiClient = Services.Base.Client;

/// <summary>Dependency injection registration for the Bobeta API client layer.</summary>
public static class ServiceRegistration
{
    /// <summary>Named HttpClient key for the Bobeta API.</summary>
    public const string HttpClientName = "Bobeta";

    /// <summary>
    /// Registers the Bobeta API client: named HttpClient, NSwag <see cref="IClient"/>, optional bearer token handler, AutoMapper, and feature services.
    /// Each feature service gets one <see cref="HttpClient"/> from the factory and the same instance backs <see cref="IClient"/> so NSwag calls and <see cref="BaseHttpService"/> helpers always share one pipeline (bearer, WASM fetch options, etc.).
    /// Configure the API base URL via <paramref name="configureHttpClient"/> (e.g. client.BaseAddress = new Uri(options.BaseUrl)).
    /// When <paramref name="useBearerToken"/> is true, register <see cref="Contracts.IAccessTokenProvider"/> in the host so requests include the bearer token.
    /// </summary>
    /// <param name="configureHttpClientBuilder">Optional (e.g. MAUI Android): set <see cref="IHttpClientBuilder.ConfigurePrimaryHttpMessageHandler"/> so DNS/TLS use the platform stack.</param>
    public static IServiceCollection AddBobetaClient(
        this IServiceCollection services,
        Action<HttpClient>? configureHttpClient = null,
        bool useBearerToken = false,
        Action<IHttpClientBuilder>? configureHttpClientBuilder = null)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(ServiceRegistration).Assembly);
        });

        var httpClientBuilder = services.AddHttpClient(HttpClientName, client =>
        {
            configureHttpClient?.Invoke(client);
        });

        configureHttpClientBuilder?.Invoke(httpClientBuilder);

        if (useBearerToken)
        {
            // Transient is recommended for delegating handlers with IHttpClientFactory (avoids scoped lifetime issues outside web request scopes).
            services.AddTransient<BearerTokenHandler>();
            httpClientBuilder.AddHttpMessageHandler<BearerTokenHandler>();
        }

        services.AddScoped<IGameService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new GameService(new ApiClient(http), http);
        });

        services.AddScoped<WalletService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new WalletService(new ApiClient(http), http);
        });

        services.AddScoped<AuthService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new AuthService(new ApiClient(http), http);
        });

        services.AddScoped<GamePlayService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new GamePlayService(new ApiClient(http), http);
        });

        services.AddScoped<HistoryService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new HistoryService(new ApiClient(http), http);
        });

        return services;
    }
}
