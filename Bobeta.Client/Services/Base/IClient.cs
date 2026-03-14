namespace Bobeta.Client.Services.Base;

/// <summary>
/// Extended IClient: exposes HttpClient for BaseHttpService. API operations use the generated methods when NSwag client is added.
/// </summary>
public partial interface IClient
{
    HttpClient HttpClient { get; }
}
