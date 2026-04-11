using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Auth;

namespace Bobeta.Mobile.Pages;

public partial class OtpVerificationPage : ContentPage
{
    private OtpVerificationViewModel? _vm;

    public OtpVerificationPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<OtpVerificationViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        HeaderLabel.Text = i18n.T("verification_code");
        PhoneHintLabel.Text = $"{i18n.T("enter_otp_desc")} {_vm.PhoneNumber}";
        VerifyButton.Text = i18n.T("verify");
        ResendButton.Text = i18n.T("resend_code");
        SyncUi();
    }

    protected override void OnDisappearing()
    {
        if (_vm != null)
            _vm.StateChanged -= OnVmChanged;
        base.OnDisappearing();
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(SyncUi);

    private void SyncUi()
    {
        if (_vm == null) return;
        ErrorLabel.Text = _vm.ErrorMessage ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_vm.ErrorMessage);
        Busy.IsRunning = _vm.IsLoading;
        VerifyButton.IsEnabled = _vm.CanVerify && !_vm.IsLoading;
    }

    private async void OnVerify(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.VerifyAsync();
    }

    private async void OnResend(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.ResendAsync();
    }
}
