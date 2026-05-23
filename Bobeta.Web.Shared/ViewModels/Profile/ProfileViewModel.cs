using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.Services.Realtime;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.ViewModels.Profile;

public class ProfileViewModel(AppStateService appState, NavigationManager nav, GameHubClient hub) : ViewModelBase
{
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;
    private readonly GameHubClient _hub = hub;

    public string PlayerName => _appState.State.CurrentPlayerName ?? "Player";
    public string? PhoneNumber => _appState.State.PhoneNumber ?? "—";
    public bool ShowSignOutModal { get; set; }

    public void ShowSignOut() { ShowSignOutModal = true; RaiseStateChanged(); }
    public void HideSignOut() { ShowSignOutModal = false; RaiseStateChanged(); }

    public async Task SignOutAsync()
    {
        try
        {
            await _hub.DisconnectAsync();
        }
        catch
        {
            /* best-effort: still clear local session */
        }

        _appState.ClearSession();
        await _appState.PersistAsync();
        ShowSignOutModal = false;
        _nav.NavigateTo("/", replace: true);
        RaiseStateChanged();
    }
}
