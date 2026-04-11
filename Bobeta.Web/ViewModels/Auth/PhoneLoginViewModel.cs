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

    /// <summary>Selected country dial code (e.g. +242).</summary>
    public string CountryDial { get; set; } = "+242";

    /// <summary>Expected number of digits for the selected country.</summary>
    public int PhoneDigits { get; set; } = 9;

    public bool CanSubmit => Phone.Length >= PhoneDigits && !IsLoading;

    public void SetCountry(string dial, int digits)
    {
        CountryDial = dial;
        PhoneDigits = digits;
        if (Phone.Length > digits)
            Phone = Phone[..digits];
    }

    public async Task SendOtpAsync()
    {
        if (!CanSubmit) return;
        SetLoading(true);
        try
        {
            var fullNumber = CountryDial + Phone;
            var res = await _authService.SendOtpAsync(fullNumber);
            if (res.IsSuccess)
            {
                _appState.SetPhoneNumber(fullNumber);
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
