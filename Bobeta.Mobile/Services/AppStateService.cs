using Bobeta.Mobile.State;

namespace Bobeta.Mobile.Services;

public class AppStateService(PreferencesStorageService storage)
{
    private readonly PreferencesStorageService _storage = storage;

    public AppState State { get; } = new();

    public event Action? StateChanged;

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
        RaiseStateChanged();
    }

    public async Task PersistAsync(CancellationToken ct = default)
    {
        await _storage.SaveStateAsync(State, ct).ConfigureAwait(false);
        RaiseStateChanged();
    }

    public Task SaveAsync(CancellationToken ct = default) => PersistAsync(ct);

    public void SetPlayer(Guid playerId, string? playerName, string? token)
    {
        State.CurrentPlayerId = playerId;
        State.CurrentPlayerName = playerName;
        State.AccessToken = token;
        RaiseStateChanged();
    }

    public void SetPhoneNumber(string? phoneNumber)
    {
        State.PhoneNumber = phoneNumber;
        RaiseStateChanged();
    }

    public void SetWalletBalance(decimal balance, decimal lockedBalance)
    {
        State.WalletBalance = balance;
        State.LockedBalance = lockedBalance;
        RaiseStateChanged();
    }

    public void SetActiveGameSession(Guid? sessionId)
    {
        State.ActiveGameSessionId = sessionId;
        RaiseStateChanged();
    }

    public void SetLanguage(string language)
    {
        State.SelectedLanguage = language;
        RaiseStateChanged();
    }

    public void ClearSession()
    {
        State.AccessToken = null;
        State.CurrentPlayerId = null;
        State.CurrentPlayerName = null;
        State.PhoneNumber = null;
        State.ActiveGameSessionId = null;
        RaiseStateChanged();
    }

    public void ClearAuth() => ClearSession();

    private void RaiseStateChanged() => StateChanged?.Invoke();
}
