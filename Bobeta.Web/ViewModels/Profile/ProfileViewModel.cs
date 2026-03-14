using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.ViewModels.Profile;

public class ProfileViewModel : ViewModelBase
{
    private readonly AppStateService _appState;
    private readonly NavigationManager _nav;

    public ProfileViewModel(AppStateService appState, NavigationManager nav)
    {
        _appState = appState;
        _nav = nav;
    }

    public string PlayerName => _appState.State.CurrentPlayerName ?? "Player";
    public string? PhoneNumber => _appState.State.PhoneNumber ?? "—";
    public bool ShowSignOutModal { get; set; }

    public void ShowSignOut() { ShowSignOutModal = true; RaiseStateChanged(); }
    public void HideSignOut() { ShowSignOutModal = false; RaiseStateChanged(); }

    public async Task SignOutAsync()
    {
        _appState.ClearSession();
        await _appState.PersistAsync();
        ShowSignOutModal = false;
        _nav.NavigateTo("/");
        RaiseStateChanged();
    }
}
