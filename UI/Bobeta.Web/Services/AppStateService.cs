using Bobeta.Web.State;

namespace Bobeta.Web.Services;

public class AppStateService
{
    private readonly LocalStorageService _storage;

    public AppStateService(LocalStorageService storage)
    {
        _storage = storage;
        State = new AppState();
    }

    public AppState State { get; }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var loaded = await _storage.LoadStateAsync(ct).ConfigureAwait(false);
        if (loaded == null) return;
        State.CurrentPlayerName = loaded.CurrentPlayerName;
        State.CurrentPlayerId = loaded.CurrentPlayerId;
        State.PhoneNumber = loaded.PhoneNumber;
        State.AccessToken = loaded.AccessToken;
        State.WalletBalance = loaded.WalletBalance;
        State.LockedBalance = loaded.LockedBalance;
        State.ActiveGameSessionId = loaded.ActiveGameSessionId;
        State.SelectedLanguage = loaded.SelectedLanguage;
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        await _storage.SaveStateAsync(State, ct).ConfigureAwait(false);
    }

    public void ClearAuth()
    {
        State.AccessToken = null;
        State.CurrentPlayerId = null;
        State.CurrentPlayerName = null;
        State.PhoneNumber = null;
        State.ActiveGameSessionId = null;
    }
}
