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
        // Relative Uri resolution is safest when the base URI ends with '/' (RFC 3986).
        var normalizedBase = baseUri.AbsoluteUri.TrimEnd('/') + "/";
        baseUri = new Uri(normalizedBase, UriKind.Absolute);
        return new Uri(baseUri, requestUri);
    }

    private static string WithCacheBuster(string requestUri, long ticks)
    {
        var sep = requestUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{requestUri}{sep}_={ticks}";
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

    protected Task<Response<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default) =>
        GetAsync<T>(requestUri, retryOnTransientFailure: false, cancellationToken);

    /// <summary>GET with optional single retry on timeouts / connection resets (game state sync on poor networks).</summary>
    protected async Task<Response<T>> GetAsync<T>(
        string requestUri,
        bool retryOnTransientFailure,
        CancellationToken cancellationToken = default)
    {
        var attempts = retryOnTransientFailure ? 2 : 1;
        Response<T>? last = null;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                last = await SendGetOnceAsync<T>(requestUri, cancellationToken).ConfigureAwait(false);
                if (last.IsSuccess || last.StatusCode is 401 or 404 || attempt == attempts - 1)
                    return last;
            }
            catch (Exception) when (attempt < attempts - 1 && !cancellationToken.IsCancellationRequested)
            {
                // Timeout or connection reset — one short retry for game-state sync.
            }

            if (attempt < attempts - 1)
                await Task.Delay(400, cancellationToken).ConfigureAwait(false);
        }

        return last ?? Response<T>.Failure("Network error. Please check your connection and try again.", null);
    }

    private async Task<Response<T>> SendGetOnceAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ResolveRequestUri(requestUri));
            await AttachBearerAsync(request, cancellationToken).ConfigureAwait(false);
            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                using var retry = new HttpRequestMessage(HttpMethod.Get, ResolveRequestUri(WithCacheBuster(requestUri, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())));
                await AttachBearerAsync(retry, cancellationToken).ConfigureAwait(false);
                using var response2 = await HttpClient.SendAsync(retry, cancellationToken).ConfigureAwait(false);
                if (!response2.IsSuccessStatusCode)
                    return await ToErrorResponseAsync<T>(response2, cancellationToken).ConfigureAwait(false);
                var data2 = await response2.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
                return Response<T>.Success(data2!);
            }

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
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                using var retry = new HttpRequestMessage(HttpMethod.Post, ResolveRequestUri(WithCacheBuster(requestUri, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())));
                if (body != null)
                    retry.Content = JsonContent.Create(body, mediaType: null, options: JsonOptions);
                await AttachBearerAsync(retry, cancellationToken).ConfigureAwait(false);
                using var response2 = await HttpClient.SendAsync(retry, cancellationToken).ConfigureAwait(false);
                if (!response2.IsSuccessStatusCode)
                    return await ToErrorResponseAsync<T>(response2, cancellationToken).ConfigureAwait(false);
                var data2 = response2.StatusCode == HttpStatusCode.NoContent
                    ? default
                    : await response2.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
                return Response<T>.Success(data2!);
            }

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
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                using var retry = new HttpRequestMessage(HttpMethod.Put, ResolveRequestUri(WithCacheBuster(requestUri, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())));
                if (body != null)
                    retry.Content = JsonContent.Create(body, mediaType: null, options: JsonOptions);
                await AttachBearerAsync(retry, cancellationToken).ConfigureAwait(false);
                using var response2 = await HttpClient.SendAsync(retry, cancellationToken).ConfigureAwait(false);
                if (!response2.IsSuccessStatusCode)
                    return await ToErrorResponseAsync<T>(response2, cancellationToken).ConfigureAwait(false);
                var data2 = response2.StatusCode == HttpStatusCode.NoContent
                    ? default
                    : await response2.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
                return Response<T>.Success(data2!);
            }

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
        string? code = null;
        string message;
        if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("code", out var codeEl))
                    code = codeEl.GetString();
                if (doc.RootElement.TryGetProperty("message", out var msgEl))
                    message = msgEl.GetString() ?? body;
                else
                    message = body;
            }
            catch (JsonException)
            {
                message = body;
            }
        }
        else
            message = string.IsNullOrEmpty(body) ? $"API error: {response.StatusCode}" : body;

        return Response<T>.Failure(message, (int)response.StatusCode, code);
    }
}
