using Microsoft.AspNetCore.Components;
using Bobeta.Client.Services;

namespace Bobeta.Web.ViewModels.Auth;

public class OtpVerificationViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly AppStateService _appState;
    private readonly NavigationManager _nav;

    public OtpVerificationViewModel(AuthService authService, AppStateService appState, NavigationManager nav)
    {
        _authService = authService;
        _appState = appState;
        _nav = nav;
    }

    public string Otp { get; set; } = "";

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
                _nav.NavigateTo("/create-player");
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
