namespace Bobeta.Client.Services.Base;

/// <summary>
/// Partial class for extending the NSwag-generated API client. Exposes HttpClient for BaseHttpService.
/// </summary>
public partial class Client : IClient
{
    public HttpClient HttpClient => _httpClient;
}
