using Bobeta.Client.Services;
using Bobeta.Mobile.Data;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Auth;

public class PhoneLoginViewModel(AuthService authService, AppStateService appState, INavigationService nav) : ViewModelBase
{
    private readonly AuthService _authService = authService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;

    private string _phone = "";

    public string Phone
    {
        get => _phone;
        set
        {
            if (_phone == value) return;
            _phone = value;
            RaiseStateChanged();
        }
    }

    public string CountryDial { get; set; } = CountryDialOption.Default.Dial;
    public int PhoneDigits { get; set; } = CountryDialOption.Default.Digits;

    public bool CanSubmit => Phone.Length >= PhoneDigits && !IsLoading;

    public void SetCountry(string dial, int digits)
    {
        CountryDial = dial;
        PhoneDigits = digits;
        if (Phone.Length > digits)
            Phone = Phone[..digits];
        RaiseStateChanged();
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
                await _nav.ToOtpVerificationAsync();
            }
            else
                SetError(res.ErrorMessage ?? "Failed to send code.");
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
