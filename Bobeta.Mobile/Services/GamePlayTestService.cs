using System.Net.Http.Headers;
using Bobeta.Client.Contracts;

namespace Bobeta.Mobile.Services;

/// <summary>Test-only: calls the simulate-AI endpoint for local testing.</summary>
public class GamePlayTestService(IHttpClientFactory httpClientFactory, IAccessTokenProvider tokenProvider)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IAccessTokenProvider _tokenProvider = tokenProvider;

    public async Task<bool> SimulateAiMoveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(Bobeta.Client.ServiceRegistration.HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/GamePlayTest/simulate-ai?sessionId={sessionId:D}");
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}
