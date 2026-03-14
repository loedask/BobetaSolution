namespace Bobeta.Client.Contracts;

/// <summary>
/// Provides the current access token for authenticating API requests.
/// Implement in the host (e.g. mobile app) and register when using bearer auth.
/// </summary>
public interface IAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
