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

    protected override void OnAppearing()
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
        EmptyLabel.Text = "No open games. Create one from the dashboard.";

        _ = _vm.LoadGamesAsync();
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
        Busy.IsRunning = _vm.IsLoading && _vm.OpenGames.Count == 0;
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
