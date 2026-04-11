namespace Bobeta.Mobile.Services;

/// <summary>Test-only: calls the simulate-AI endpoint for local testing.</summary>
public class GamePlayTestService(IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<bool> SimulateAiMoveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(Bobeta.Client.ServiceRegistration.HttpClientName);
        var response = await client.PostAsync($"api/GamePlayTest/simulate-ai?sessionId={sessionId}", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
