using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Profile;

public class ProfileViewModel(AppStateService appState, INavigationService nav) : ViewModelBase
{
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;

    public string PlayerName => _appState.State.CurrentPlayerName ?? "Player";
    public string? PhoneNumber => _appState.State.PhoneNumber ?? "—";
    public bool ShowSignOutModal { get; set; }

    public void ShowSignOut()
    {
        ShowSignOutModal = true;
        RaiseStateChanged();
    }

    public void HideSignOut()
    {
        ShowSignOutModal = false;
        RaiseStateChanged();
    }

    public async Task SignOutAsync()
    {
        _appState.ClearSession();
        await _appState.PersistAsync();
        ShowSignOutModal = false;
        await _nav.ToWelcomeAsync();
        RaiseStateChanged();
    }
}
