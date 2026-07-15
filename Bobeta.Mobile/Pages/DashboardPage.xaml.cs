using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Dashboard;
using Bobeta.Mobile.ViewModels.Notifications;

namespace Bobeta.Mobile.Pages;

public partial class DashboardPage : ContentPage
{
    private DashboardViewModel? _vm;
    private NotificationInboxViewModel? _inbox;

    public DashboardPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<DashboardViewModel>();
        _inbox = MauiProgram.Services.GetRequiredService<NotificationInboxViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;
        _inbox.StateChanged -= OnInboxChanged;
        _inbox.StateChanged += OnInboxChanged;

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
        InboxTitle.Text = i18n.T("notifications");
        MarkAllBtn.Text = i18n.T("notif_mark_all_read");
        InboxEmptyTitle.Text = i18n.T("notif_empty_title");
        InboxEmptyBody.Text = i18n.T("notif_empty_body");
        BellBtn.Text = "🔔";

        _ = LoadAsync();
        SyncUi();
    }

    protected override void OnDisappearing()
    {
        if (_vm != null)
            _vm.StateChanged -= OnVmChanged;
        if (_inbox != null)
            _inbox.StateChanged -= OnInboxChanged;
        base.OnDisappearing();
    }

    private async Task LoadAsync()
    {
        if (_vm != null)
            await _vm.LoadAsync();
        if (_inbox != null)
            await _inbox.InitializeAsync();
        SyncUi();
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(SyncUi);
    private void OnInboxChanged() => MainThread.BeginInvokeOnMainThread(SyncUi);

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

        if (_inbox == null) return;
        UnreadDot.IsVisible = _inbox.UnreadCount > 0;
        InboxOverlay.IsVisible = _inbox.IsOpen;
        InboxList.ItemsSource = _inbox.Rows;
        MarkAllBtn.IsVisible = _inbox.UnreadCount > 0;
        InboxUnread.Text = _inbox.UnreadCount > 0
            ? string.Format(i18n.T("notif_unread_count"), _inbox.UnreadCount)
            : "";
        InboxError.Text = _inbox.ErrorMessage ?? "";
        InboxError.IsVisible = !string.IsNullOrEmpty(_inbox.ErrorMessage);
    }

    private async void OnOpenInbox(object? sender, EventArgs e)
    {
        if (_inbox == null) return;
        await _inbox.OpenAsync();
    }

    private void OnCloseInbox(object? sender, EventArgs e) => _inbox?.Close();

    private async void OnMarkAllRead(object? sender, EventArgs e)
    {
        if (_inbox == null) return;
        await _inbox.MarkAllReadAsync();
    }

    private async void OnInboxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_inbox == null) return;
        if (e.CurrentSelection.FirstOrDefault() is not NotificationRow row)
            return;
        InboxList.SelectedItem = null;
        await _inbox.OpenItemAsync(row.Source);
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
