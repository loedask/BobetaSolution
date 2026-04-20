using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Games;

namespace Bobeta.Mobile.Pages;

public partial class GamePlayPage : ContentPage, IQueryAttributable
{
    private GamePlayViewModel? _vm;
    private string _sessionId = "";

    public GamePlayPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("SessionId", out var v))
            _sessionId = Uri.UnescapeDataString(v?.ToString() ?? "");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<GamePlayViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        Title = i18n.T("game");
        PotTableLabel.Text = i18n.T("pot_table");
        PotSeatsLabel.Text = i18n.T("pot_seats");
        LastPlayTitle.Text = "Last played";
        ResultTitle.Text = "Game over";
        DoneBtn.Text = i18n.T("return_home");

        if (!string.IsNullOrEmpty(_sessionId))
            await _vm.LoadGameAsync(_sessionId);

        SyncUi();
    }

    protected override async void OnDisappearing()
    {
        if (_vm != null)
        {
            _vm.StateChanged -= OnVmChanged;
            await _vm.DisposeAsync();
        }

        base.OnDisappearing();
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(SyncUi);

    private void SyncUi()
    {
        if (_vm == null) return;
        ErrorLabel.Text = _vm.ErrorMessage ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_vm.ErrorMessage);
        Busy.IsRunning = _vm.IsLoading && _vm.PlayerCards.Count == 0;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        TurnLabel.Text = _vm.WaitingForOpponent
            ? i18n.T("waiting_for_opponent")
            : _vm.IsPlayerTurn
                ? i18n.T("your_turn")
                : i18n.T("opponent_turn");
        TurnLabel.TextColor = _vm.IsPlayerTurn ? Color.FromArgb("#2dd48e") : Color.FromArgb("#8a93a8");
        HandView.IsEnabled = _vm.IsPlayerTurn && !_vm.ShowGameResult;

        var half = _vm.PotAmount > 0 ? _vm.PotAmount / 2m : 0m;
        var halfStr = half.ToString("N0");
        PotTotalLabel.Text = $"{_vm.PotAmount:N0} FCFA";
        ChipLeftText.Text = halfStr;
        ChipRightText.Text = halfStr;
        PotHintLabel.Text = string.Format(i18n.T("pot_activity_hint"), halfStr);
    }

    private async void OnHandCardTapped(object? sender, TappedEventArgs e)
    {
        if (_vm == null || !_vm.IsPlayerTurn || _vm.ShowGameResult) return;
        if (sender is not Border { BindingContext: CardViewModel card }) return;
        if (!card.IsPlayable) return;
        await _vm.PlayCardAsync(card);
    }

    private async void OnDone(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//MainTabs/Dashboard");
}
