using Bobeta.Client.Contracts;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bobeta.Client.Services.Base;

/// <summary>
/// Base service that wraps the API client and provides generic HTTP helpers
/// with standardized Response&lt;T&gt; and ApiException handling.
/// </summary>
public abstract class BaseHttpService(IClient client, HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected IClient Client { get; } = client;
    protected HttpClient HttpClient { get; } = httpClient;

    /// <summary>Resolves relative paths (e.g. <c>api/History/player</c>) against <see cref="HttpClient.BaseAddress"/> so WASM/browser handlers always target the API host.</summary>
    private Uri ResolveRequestUri(string requestUri)
    {
        if (Uri.TryCreate(requestUri, UriKind.Absolute, out var absolute))
            return absolute;
        var baseUri = HttpClient.BaseAddress ?? throw new InvalidOperationException("HttpClient.BaseAddress is required for relative API paths.");
        return new Uri(baseUri, requestUri);
    }

    /// <summary>Attaches Authorization when an <see cref="IAccessTokenProvider"/> was supplied (Blazor WASM / hosts where delegating-handler token injection is unreliable).</summary>
    private async Task AttachBearerAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (accessTokenProvider == null) return;
        var token = await accessTokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token)) return;
        token = token.Trim();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<Response<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ResolveRequestUri(requestUri));
            await AttachBearerAsync(request, cancellationToken).ConfigureAwait(false);
            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return await ToErrorResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return Response<T>.Success(data!);
        }
        catch (ApiException ex)
        {
            return Response<T>.Failure(ex.Message, ex.StatusCode);
        }
    }

    protected async Task<Response<T>> PostAsync<T>(string requestUri, object? body, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResolveRequestUri(requestUri));
            if (body != null)
                request.Content = JsonContent.Create(body, mediaType: null, options: JsonOptions);
            await AttachBearerAsync(request, cancellationToken).ConfigureAwait(false);
            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return await ToErrorResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
            var data = response.StatusCode == HttpStatusCode.NoContent
                ? default
                : await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return Response<T>.Success(data!);
        }
        catch (ApiException ex)
        {
            return Response<T>.Failure(ex.Message, ex.StatusCode);
        }
    }

    protected async Task<Response<T>> PutAsync<T>(string requestUri, object? body, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, ResolveRequestUri(requestUri));
            if (body != null)
                request.Content = JsonContent.Create(body, mediaType: null, options: JsonOptions);
            await AttachBearerAsync(request, cancellationToken).ConfigureAwait(false);
            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return await ToErrorResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
            var data = response.StatusCode == HttpStatusCode.NoContent
                ? default
                : await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return Response<T>.Success(data!);
        }
        catch (ApiException ex)
        {
            return Response<T>.Failure(ex.Message, ex.StatusCode);
        }
    }

    protected async Task<Response<bool>> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, ResolveRequestUri(requestUri));
            await AttachBearerAsync(request, cancellationToken).ConfigureAwait(false);
            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return await ToErrorResponseAsync<bool>(response, cancellationToken).ConfigureAwait(false);
            return Response<bool>.Success(true);
        }
        catch (ApiException ex)
        {
            return Response<bool>.Failure(ex.Message, ex.StatusCode);
        }
    }

    private static async Task<Response<T>> ToErrorResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var message = string.IsNullOrEmpty(body) ? $"API error: {response.StatusCode}" : body;
        return Response<T>.Failure(message, (int)response.StatusCode);
    }
}
