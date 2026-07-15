using Bobeta.Client.Contracts;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Services;
using Bobeta.Client.Services.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Bobeta.Client;

/// <summary>Dependency injection registration for the Bobeta API client layer.</summary>
public static class ServiceRegistration
{
    /// <summary>Named HttpClient key for the Bobeta API.</summary>
    public const string HttpClientName = "Bobeta";

    /// <summary>
    /// Registers feature services that call the API via <see cref="BaseHttpService"/>.
    /// Configure the API base URL via <paramref name="configureHttpClient"/> (e.g. client.BaseAddress = new Uri(options.BaseUrl)).
    /// When <paramref name="useBearerToken"/> is true, register <see cref="IAccessTokenProvider"/> in the host.
    /// </summary>
    /// <param name="configureHttpClientBuilder">Optional (e.g. MAUI Android): set <see cref="IHttpClientBuilder.ConfigurePrimaryHttpMessageHandler"/>.</param>
    public static IServiceCollection AddBobetaClient(
        this IServiceCollection services,
        Action<HttpClient>? configureHttpClient = null,
        bool useBearerToken = false,
        Action<IHttpClientBuilder>? configureHttpClientBuilder = null)
    {
        var httpClientBuilder = services.AddHttpClient(HttpClientName, client =>
        {
            configureHttpClient?.Invoke(client);
        });

        if (useBearerToken)
        {
            services.AddTransient<BearerTokenHandler>();
            httpClientBuilder.AddHttpMessageHandler<BearerTokenHandler>();
        }

        configureHttpClientBuilder?.Invoke(httpClientBuilder);

        IAccessTokenProvider? TokenProvider(IServiceProvider sp) =>
            useBearerToken ? sp.GetRequiredService<IAccessTokenProvider>() : null;

        services.AddScoped<IGameService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new GameService(http, TokenProvider(sp));
        });

        services.AddScoped<WalletService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new WalletService(http, TokenProvider(sp));
        });

        services.AddScoped<AuthService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new AuthService(http, TokenProvider(sp));
        });

        services.AddScoped<GamePlayService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new GamePlayService(http, TokenProvider(sp));
        });

        services.AddScoped<HistoryService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new HistoryService(http, TokenProvider(sp));
        });

        services.AddScoped<InfluencerService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new InfluencerService(http, TokenProvider(sp));
        });

        services.AddScoped<NotificationApiService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new NotificationApiService(http, TokenProvider(sp));
        });

        return services;
    }
}
