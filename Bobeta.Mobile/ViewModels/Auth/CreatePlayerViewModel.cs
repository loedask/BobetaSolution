using Bobeta.Client.Models.Players;
using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Auth;

public class CreatePlayerViewModel(
    AuthService authService,
    AppStateService appState,
    INavigationService nav,
    InfluencerService influencerService) : ViewModelBase
{
    private readonly AuthService _authService = authService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;
    private readonly InfluencerService _influencerService = influencerService;

    private string _playerName = "";

    public string PlayerName
    {
        get => _playerName;
        set
        {
            if (_playerName == value) return;
            _playerName = value;
            RaiseStateChanged();
        }
    }

    public bool IsPlayerNameValid => PlayerName.Length >= 2;
    public bool CanSubmit => IsPlayerNameValid && !IsLoading && !string.IsNullOrEmpty(_appState.State.PhoneNumber);

    public async Task RegisterAsync()
    {
        if (!CanSubmit) return;
        SetLoading(true);
        try
        {
            var req = new CreatePlayerRequest
            {
                PhoneNumber = _appState.State.PhoneNumber!,
                PlayerName = PlayerName
            };
            var res = await _authService.RegisterAsync(req);
            if (res.IsSuccess && res.Data != null)
            {
                _appState.SetPlayer(res.Data.PlayerId, res.Data.PlayerName, res.Data.Token);
                await _appState.PersistAsync();
                await PendingInviteApplicator.TryApplyAsync(
                    _influencerService,
                    _appState.State.PendingInviteCode,
                    () => _appState.SetPendingInviteCode(null),
                    () => _appState.PersistAsync());
                await _nav.ToMainTabsAsync("Dashboard");
            }
            else
                SetError(res.ErrorMessage ?? "Registration failed.");
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
        }
    }
}
