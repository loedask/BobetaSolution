using Bobeta.Client.Contracts;

namespace Bobeta.Web.Services;

public class WebAccessTokenProvider : IAccessTokenProvider
{
    private readonly AppStateService _appState;

    public WebAccessTokenProvider(AppStateService appState) => _appState = appState;

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_appState.State.AccessToken);
    }
}
