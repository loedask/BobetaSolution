namespace Bobeta.Web.Services;

/// <summary>Test-only: calls the simulate-AI endpoint for local single-browser testing.</summary>
public class GamePlayTestService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GamePlayTestService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>POST api/GamePlayTest/simulate-ai?sessionId=... Uses the same Bobeta HttpClient (API base + bearer).</summary>
    public async Task<bool> SimulateAiMoveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(Bobeta.Client.ServiceRegistration.HttpClientName);
        var response = await client.PostAsync($"api/GamePlayTest/simulate-ai?sessionId={sessionId}", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
