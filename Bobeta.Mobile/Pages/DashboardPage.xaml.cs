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
        ErrorLabel.Text = _vm.ErrorMessage ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_vm.ErrorMessage);
        Busy.IsRunning = _vm.IsLoading && _vm.Transactions.Count == 0;
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
