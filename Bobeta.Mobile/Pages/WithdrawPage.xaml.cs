using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Wallet;

namespace Bobeta.Mobile.Pages;

public partial class WithdrawPage : ContentPage
{
    private WithdrawViewModel? _vm;

    public WithdrawPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<WithdrawViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        Title = i18n.T("withdraw");
        BalTitle.Text = i18n.T("available_balance");
        AmtTitle.Text = i18n.T("amount_fcfa");
        MomoTitle.Text = i18n.T("momo_number");
        SubmitBtn.Text = i18n.T("confirm_withdrawal");
        SuccessTitle.Text = i18n.T("withdrawal_successful");
        RefreshMinMax();
        SyncUi();
    }

    protected override void OnDisappearing()
    {
        if (_vm != null)
            _vm.StateChanged -= OnVmChanged;
        base.OnDisappearing();
    }

    private void RefreshMinMax()
    {
        if (_vm == null) return;
        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        MinMaxHint.Text = $"{i18n.T("min_withdrawal")}: 200 FCFA · {i18n.T("max_withdrawal")}: {_vm.AvailableBalance:N0} FCFA";
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(() =>
    {
        RefreshMinMax();
        SyncUi();
    });

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
            SuccessDetail.Text = $"{_vm.SuccessAmount:N0} FCFA {i18n.T("sent_to_momo")}";
        }
    }

    private async void OnSubmit(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.SubmitAsync();
    }
}
