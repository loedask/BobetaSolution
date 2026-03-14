// Placeholder for NSwag-generated API client. Replace this file with output from clientsettings.nswag
// when generating from Bobeta.API OpenAPI/Swagger spec. Base URL is configured via HttpClient in DI.

namespace Bobeta.Client.Services.Base;

/// <summary>Placeholder partial interface for the generated client. NSwag will add API methods here.</summary>
public partial interface IClient
{
}

/// <summary>Placeholder partial class for the generated client. NSwag will add API methods and DTOs here.</summary>
public partial class Client
{
    protected readonly HttpClient _httpClient;

    public Client(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }
}
