using Microsoft.AspNetCore.Components;
using Bobeta.Client.Models.Players;

namespace Bobeta.Web.ViewModels.Auth;

public class CreatePlayerViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly AppStateService _appState;
    private readonly NavigationManager _nav;

    public CreatePlayerViewModel(AuthService authService, AppStateService appState, NavigationManager nav)
    {
        _authService = authService;
        _appState = appState;
        _nav = nav;
    }

    public string PlayerName { get; set; } = "";

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
                _nav.NavigateTo("/dashboard");
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