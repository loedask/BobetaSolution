using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Games;

namespace Bobeta.Mobile.Pages;

public partial class JoinGamePage : ContentPage
{
    private JoinGameViewModel? _vm;

    public JoinGamePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<JoinGameViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        Title = i18n.T("join_game");
        Subtitle.Text = i18n.T("open_game_sessions");
        RefreshBtn.Text = i18n.T("refresh");
        EmptyLabel.Text = i18n.T("no_open_games");
        FilterAllBtn.Text = i18n.T("filter_all_games");
        FilterMakopaBtn.Text = "Makopa";
        FilterKopoBtn.Text = "Kopo";
        InviteHaveCodeLabel.Text = i18n.T("invite_have_code");
        InviteHintLabel.Text = i18n.T("invite_enter_hint");
        InviteApplyBtn.Text = i18n.T("invite_apply");
        InviteCodeEntry.Placeholder = i18n.T("invite_enter_code");

        await Task.WhenAll(_vm.LoadGamesAsync(), _vm.LoadInviteStatusAsync());
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
        Busy.IsRunning = _vm.IsLoading && _vm.OpenGames.Count == 0;
        StyleFilter(FilterAllBtn, _vm.VariantFilter == null);
        StyleFilter(FilterMakopaBtn, _vm.VariantFilter == GameVariant.Makopa);
        StyleFilter(FilterKopoBtn, _vm.VariantFilter == GameVariant.Kopo);

        var hasInvite = _vm.InviteStatus?.HasPendingCode == true;
        InviteBanner.IsVisible = hasInvite;
        InviteEntryPanel.IsVisible = !hasInvite;
        InviteSuccessLabel.IsVisible = !string.IsNullOrEmpty(_vm.InviteSuccessMessage);
        InviteSuccessLabel.Text = _vm.InviteSuccessMessage ?? "";
        InviteApplyBtn.IsEnabled = !_vm.IsLoading && !string.IsNullOrWhiteSpace(_vm.InviteCodeInput);
        if (hasInvite && _vm.InviteStatus != null)
        {
            InviteBannerLabel.Text = string.Format(
                i18n.T("invite_discount_banner"),
                _vm.InviteStatus.DiscountPercent.ToString("N0"));
        }
    }

    private static void StyleFilter(Button btn, bool selected)
    {
        btn.BackgroundColor = selected ? Color.FromArgb("#eab308") : Color.FromArgb("#2a3142");
        btn.TextColor = selected ? Color.FromArgb("#12151f") : Color.FromArgb("#e2e8f0");
    }

    private void OnFilterAll(object? sender, EventArgs e) => _vm?.SetVariantFilter(null);
    private void OnFilterMakopa(object? sender, EventArgs e) => _vm?.SetVariantFilter(GameVariant.Makopa);
    private void OnFilterKopo(object? sender, EventArgs e) => _vm?.SetVariantFilter(GameVariant.Kopo);

    private async void OnApplyInvite(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.ApplyInviteCodeAsync();
    }

    private async void OnRefresh(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.LoadGamesAsync();
    }

    private async void OnJoinOne(object? sender, EventArgs e)
    {
        if (_vm == null || sender is not Button { BindingContext: GameSessionViewModel g }) return;
        await _vm.JoinGameAsync(g.Id);
    }
}
