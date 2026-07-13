using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Dashboard;

namespace Bobeta.Mobile.Pages;

public partial class DashboardPage : ContentPage
{
    private DashboardViewModel? _vm;

    public DashboardPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<DashboardViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        WelcomePrefix.Text = i18n.T("welcome_back");
        WalletTitle.Text = i18n.T("wallet_balance");
        DepositBtn.Text = i18n.T("deposit");
        WithdrawBtn.Text = i18n.T("withdraw");
        CreateTile.Text = i18n.T("create_game");
        JoinTile.Text = i18n.T("join_game");
        HistoryTile.Text = i18n.T("history");
        RecentTitle.Text = i18n.T("recent_activity");
        SeeAllBtn.Text = i18n.T("see_all");
        TrustLabel.Text = i18n.T("trust_message");
        EmptyTx.Text = "No recent activity";
        InviteHaveCodeLabel.Text = i18n.T("invite_have_code");
        InviteHintLabel.Text = i18n.T("invite_enter_hint");
        InviteDismissBtn.Text = i18n.T("invite_prompt_dismiss");
        InviteApplyBtn.Text = i18n.T("invite_apply");
        InviteCodeEntry.Placeholder = i18n.T("invite_enter_code");

        _ = _vm.LoadAsync();
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
        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        ErrorLabel.Text = _vm.ErrorMessage ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_vm.ErrorMessage);
        Busy.IsRunning = _vm.IsLoading && _vm.Transactions.Count == 0;

        InvitePromptPanel.IsVisible = _vm.ShowInvitePrompt;
        InviteSuccessLabel.IsVisible = !string.IsNullOrEmpty(_vm.InviteSuccessMessage);
        InviteSuccessLabel.Text = _vm.InviteSuccessMessage ?? "";
        InviteApplyBtn.IsEnabled = !_vm.IsLoading && !string.IsNullOrWhiteSpace(_vm.InviteCodeInput);

        var hasPending = _vm.InviteStatus?.HasPendingCode == true;
        InvitePendingBanner.IsVisible = hasPending && !_vm.ShowInvitePrompt;
        if (hasPending && _vm.InviteStatus != null)
        {
            InvitePendingLabel.Text = string.Format(
                i18n.T("invite_pending_status"),
                _vm.InviteStatus.Code ?? "",
                _vm.InviteStatus.DiscountPercent.ToString("N0"));
        }
    }

    private async void OnApplyInvite(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.ApplyInviteCodeAsync();
    }

    private async void OnDismissInvite(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.DismissInvitePromptAsync();
    }

    private async void OnDeposit(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.GoToDepositAsync();
    }

    private async void OnWithdraw(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.GoToWithdrawAsync();
    }

    private async void OnGoCreate(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//MainTabs/CreateGame");

    private async void OnGoJoin(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//MainTabs/JoinGame");

    private async void OnGoHistory(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//MainTabs/History");
}
