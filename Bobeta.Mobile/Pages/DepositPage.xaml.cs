using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Wallet;

namespace Bobeta.Mobile.Pages;

public partial class DepositPage : ContentPage
{
    private DepositViewModel? _vm;

    public DepositPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<DepositViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        Title = i18n.T("deposit");
        BalTitle.Text = i18n.T("current_balance");
        AmtTitle.Text = i18n.T("amount_fcfa");
        MomoTitle.Text = i18n.T("payment_method_momo") + " — " + i18n.T("instant_deposit_momo");
        SubmitBtn.Text = i18n.T("confirm_deposit");
        SuccessTitle.Text = i18n.T("deposit_successful");
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
        Busy.IsRunning = _vm.IsProcessing;
        SubmitBtn.IsEnabled = _vm.CanSubmit && !_vm.IsProcessing;
        FormSection.IsVisible = !_vm.IsSuccess;
        SuccessSection.IsVisible = _vm.IsSuccess;
        if (_vm.IsSuccess)
        {
            var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
            SuccessDetail.Text = $"+{_vm.SuccessAmount:N0} FCFA {i18n.T("added_to_wallet")}";
        }
    }

    private void OnPreset(object? sender, EventArgs e)
    {
        if (sender is Button b && int.TryParse(b.Text, out var n))
            _vm?.SetPresetAmount(n);
    }

    private async void OnSubmit(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.SubmitAsync();
    }
}
