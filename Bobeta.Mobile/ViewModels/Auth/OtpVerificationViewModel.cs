using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Auth;

public class OtpVerificationViewModel(AuthService authService, AppStateService appState, INavigationService nav) : ViewModelBase
{
    private readonly AuthService _authService = authService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;

    private string _otp = "";

    public string Otp
    {
        get => _otp;
        set
        {
            if (_otp == value) return;
            _otp = value;
            RaiseStateChanged();
        }
    }

    public string? PhoneNumber => _appState.State.PhoneNumber;

    public bool CanVerify => Otp.Length >= 4 && !IsLoading;

    public void UpdateOtp(int index, string value)
    {
        if (value.Length > 1) value = value[^1].ToString();
        var arr = Otp.PadRight(4).ToCharArray();
        arr[index] = value.Length > 0 ? value[0] : ' ';
        Otp = new string(arr).TrimEnd().Replace(" ", "");
        RaiseStateChanged();
    }

    public async Task VerifyAsync()
    {
        if (!CanVerify || string.IsNullOrEmpty(PhoneNumber)) return;
        SetLoading(true);
        try
        {
            var res = await _authService.VerifyOtpAsync(PhoneNumber, Otp);
            if (res.IsSuccess)
                await _nav.ToCreatePlayerAsync();
            else
                SetError(res.ErrorMessage ?? "Verification failed.");
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

    public async Task ResendAsync()
    {
        if (string.IsNullOrEmpty(PhoneNumber)) return;
        SetLoading(true);
        try
        {
            await _authService.SendOtpAsync(PhoneNumber);
            ClearError();
        }
        finally
        {
            SetLoading(false);
        }
    }
}
