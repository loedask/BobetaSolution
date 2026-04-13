using Microsoft.AspNetCore.Components;
using Bobeta.Client.Services;
using Bobeta.Domain.Authentication;
using Bobeta.Web.Services;

namespace Bobeta.Web.ViewModels.Auth;

public class OtpVerificationViewModel(AuthService authService, AppStateService appState, NavigationManager nav) : ViewModelBase
{
    private readonly AuthService _authService = authService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;

    public string Otp { get; set; } = "";

    public string? PhoneNumber => _appState.State.PhoneNumber;

    public bool CanVerify => Otp.Length >= PhoneAuthConstants.OtpDigitLength && !IsLoading;

    public void UpdateOtp(int index, string value)
    {
        if (value.Length > 1) value = value[^1].ToString();
        var arr = Otp.PadRight(PhoneAuthConstants.OtpDigitLength).ToCharArray();
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
            if (!res.IsSuccess || res.Data == null)
            {
                SetError(res.ErrorMessage ?? "Verification failed.");
                return;
            }

            if (!string.IsNullOrEmpty(res.Data.Token) && res.Data.PlayerId is { } playerId)
            {
                _appState.SetPlayer(playerId, res.Data.PlayerName, res.Data.Token);
                await _appState.PersistAsync();
                _nav.NavigateTo("/dashboard");
            }
            else
                _nav.NavigateTo("/create-player");
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
