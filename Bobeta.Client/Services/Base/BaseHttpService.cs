using Bobeta.Client.Contracts;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bobeta.Client.Services.Base;

/// <summary>
/// Base service that wraps the API client and provides generic HTTP helpers
/// with standardized Response&lt;T&gt; and ApiException handling.
/// </summary>
public abstract class BaseHttpService(IClient client, HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected IClient Client { get; } = client;
    protected HttpClient HttpClient { get; } = httpClient;

    protected async Task<Response<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
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
            using var response = await HttpClient.PostAsJsonAsync(requestUri, body, JsonOptions, cancellationToken).ConfigureAwait(false);
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
            using var response = await HttpClient.PutAsJsonAsync(requestUri, body, JsonOptions, cancellationToken).ConfigureAwait(false);
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
            using var response = await HttpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
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
