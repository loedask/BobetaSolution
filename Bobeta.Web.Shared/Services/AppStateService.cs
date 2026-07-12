using Bobeta.Web.Shared.State;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.Services;

public class AppStateService(LocalStorageService storage)
{
    private readonly LocalStorageService _storage = storage;

    public AppState State { get; } = new AppState();

    /// <summary>Fired when state changes so UI can refresh.</summary>
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
        State.PendingInviteCode = loaded.PendingInviteCode;
        RaiseStateChanged();
    }

    /// <summary>Persist state to localStorage (e.g. after login, balance update, language change).</summary>
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

    public void SetPendingInviteCode(string? code)
    {
        State.PendingInviteCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
        RaiseStateChanged();
    }

    /// <summary>Clear auth and session; call PersistAsync after to save. Keeps <see cref="AppState.PendingInviteCode"/>.</summary>
    public void ClearSession()
    {
        State.AccessToken = null;
        State.CurrentPlayerId = null;
        State.CurrentPlayerName = null;
        State.PhoneNumber = null;
        State.ActiveGameSessionId = null;
        State.WalletBalance = 0;
        State.LockedBalance = 0;
        RaiseStateChanged();
    }

    public void ClearAuth() => ClearSession();

    /// <summary>
    /// When the API returns 401 (expired or invalid JWT, or session cleared server-side), clears local auth
    /// and navigates to login instead of leaving the user on a half-broken screen.
    /// </summary>
    public async Task<bool> HandleUnauthorizedAsync(int? statusCode, NavigationManager navigation, CancellationToken ct = default)
    {
        if (statusCode != 401)
            return false;

        ClearSession();
        await PersistAsync(ct).ConfigureAwait(false);
        navigation.NavigateTo("/login?reason=session-expired", replace: true);
        return true;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke();
}
