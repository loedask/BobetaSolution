using Microsoft.AspNetCore.Components;
using Bobeta.Client.Services;
using Bobeta.Web.Services;

namespace Bobeta.Web.ViewModels.Auth;

public class PhoneLoginViewModel(AuthService authService, AppStateService appState, NavigationManager nav) : ViewModelBase
{
    private readonly AuthService _authService = authService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;

    public string Phone { get; set; } = "";

    public bool CanSubmit => Phone.Length >= 9 && !IsLoading;

    public async Task SendOtpAsync()
    {
        if (!CanSubmit) return;
        SetLoading(true);
        try
        {
            var res = await _authService.SendOtpAsync("+237" + Phone);
            if (res.IsSuccess)
            {
                _appState.SetPhoneNumber("+237" + Phone);
                await _appState.PersistAsync();
                _nav.NavigateTo("/verify");
            }
            else
                SetError(res.ErrorMessage ?? "Failed to send code.");
        }
        catch (Exception ex)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
        }
    }
}
