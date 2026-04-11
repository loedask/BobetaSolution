using Bobeta.Client.Contracts;

namespace Bobeta.Mobile.Services;

public class MobileAccessTokenProvider(AppStateService appState) : IAccessTokenProvider
{
    private readonly AppStateService _appState = appState;

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_appState.State.AccessToken);
}
