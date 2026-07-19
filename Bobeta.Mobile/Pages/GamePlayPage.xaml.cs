using Bobeta.Client.Models.Games;
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
        _vm.NavigateHomeRequested -= OnNavigateHomeFromInactivity;
        _vm.NavigateHomeRequested += OnNavigateHomeFromInactivity;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        Title = i18n.T("game");
        PotTableLabel.Text = i18n.T("pot_table");
        PotSeatsLabel.Text = i18n.T("pot_seats");
        PotOpponentLaneLabel.Text = i18n.T("pot_opponent_lane");
        LastPlayTitle.Text = "Last played";
        ResultTitle.Text = "Game over";
        DoneBtn.Text = i18n.T("done_short");
        TakeCardButton.Text = i18n.T("take_card");
        RulesLink.Text = i18n.T("makopa_rules_link");
        InactivityContinueBtn.Text = i18n.T("continue_play");
        InactivityCancelBtn.Text = i18n.T("cancel_game");

        KopoBoard.CellTapped -= OnKopoCellTapped;
        KopoBoard.CellTapped += OnKopoCellTapped;
        NgolaBoard.PitTapped -= OnNgolaPitTapped;
        NgolaBoard.PitTapped += OnNgolaPitTapped;
        DominoBoard.ActionRequested -= OnDominoActionRequested;
        DominoBoard.ActionRequested += OnDominoActionRequested;
        AbbiaBoard.ThrowRequested -= OnAbbiaThrowRequested;
        AbbiaBoard.ThrowRequested += OnAbbiaThrowRequested;
        NzengueBoard.MoveRequested -= OnNzengueMoveRequested;
        NzengueBoard.MoveRequested += OnNzengueMoveRequested;

        if (!string.IsNullOrEmpty(_sessionId))
            await _vm.LoadGameAsync(_sessionId);

        SyncUi();
    }

    protected override async void OnDisappearing()
    {
        if (_vm != null)
        {
            _vm.StateChanged -= OnVmChanged;
            _vm.NavigateHomeRequested -= OnNavigateHomeFromInactivity;
            await _vm.DisposeAsync();
        }

        KopoBoard.CellTapped -= OnKopoCellTapped;
        NgolaBoard.PitTapped -= OnNgolaPitTapped;
        DominoBoard.ActionRequested -= OnDominoActionRequested;
        AbbiaBoard.ThrowRequested -= OnAbbiaThrowRequested;
        NzengueBoard.MoveRequested -= OnNzengueMoveRequested;

        base.OnDisappearing();
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(SyncUi);

    private void SyncUi()
    {
        if (_vm == null) return;
        ErrorLabel.Text = _vm.ErrorMessage ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_vm.ErrorMessage);
        Busy.IsRunning = _vm.ShowLoadingShell;

        var isKopo = _vm.IsKopo;
        var isNgola = _vm.IsNgola;
        var isDomino = _vm.IsDomino;
        var isAbbia = _vm.IsAbbia;
        var isNzengue = _vm.IsNzengue;
        var isMakopa = !isKopo && !isNgola && !isDomino && !isAbbia && !isNzengue;
        MakopaPanel.IsVisible = isMakopa;
        HandView.IsVisible = isMakopa;
        TakeCardButton.IsVisible = isMakopa;
        TakeCardHintLabel.IsVisible = isMakopa;
        KopoBoard.IsVisible = isKopo && _vm.Kopo != null;
        NgolaBoard.IsVisible = isNgola && _vm.Ngola != null;
        DominoBoard.IsVisible = isDomino && _vm.Domino != null;
        AbbiaBoard.IsVisible = isAbbia && _vm.Abbia != null;
        NzengueBoard.IsVisible = isNzengue && _vm.Nzengue != null;
        KopoChainHint.IsVisible = isKopo && (_vm.Kopo?.MustContinueChain ?? false);

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        TurnLabel.Text = _vm.WaitingForOpponent
            ? i18n.T("waiting_for_opponent")
            : _vm.IsPlayerTurn
                ? i18n.T("your_turn")
                : i18n.T("opponent_turn");
        TurnLabel.TextColor = _vm.IsPlayerTurn ? Color.FromArgb("#2dd48e") : Color.FromArgb("#8a93a8");
        HandView.IsEnabled = _vm.IsPlayerTurn && !_vm.ShowGameResult && !_vm.ShowInactivityOverlay && !_vm.IsSendingMove;
        TakeCardButton.IsEnabled = _vm.CanTakeCard && !_vm.IsSendingMove && !_vm.ShowInactivityOverlay;
        SendingMoveOverlay.IsVisible = _vm.IsSendingMove;
        if (_vm.IsSendingMove)
            SendingMoveLabel.Text = i18n.T("sending_move");
        TakeCardButton.Opacity = TakeCardButton.IsEnabled ? 1.0 : 0.55;
        TakeCardHintLabel.Text = _vm.CanTakeCard
            ? i18n.T("take_card_hint_enabled")
            : i18n.T("take_card_hint_disabled");

        if (isKopo && _vm.Kopo != null && _vm.MyPlayerId is { } pid)
        {
            KopoBoard.BoardSize = _vm.Kopo.BoardSize;
            KopoBoard.Pieces = _vm.Kopo.Pieces;
            KopoBoard.MyPlayerId = pid;
            KopoBoard.SelectionPath = _vm.KopoSelectionPath.ToList();
            KopoBoard.CanInteract = _vm.IsPlayerTurn && !_vm.ShowGameResult && !_vm.ShowInactivityOverlay && !_vm.IsSendingMove;
            KopoChainHint.Text = i18n.T("kopo_continue_capture");
            RulesLink.IsVisible = true;
            RulesLink.Text = i18n.T("kopo_rules_link");
            RoundScoreLabel.IsVisible = false;
        }
        else if (isNgola && _vm.Ngola != null)
        {
            NgolaBoard.MyPits = _vm.Ngola.MyPits;
            NgolaBoard.OpponentPits = _vm.Ngola.OpponentPits;
            NgolaBoard.MyScore = _vm.Ngola.MyScore;
            NgolaBoard.OpponentScore = _vm.Ngola.OpponentScore;
            NgolaBoard.CanInteract = _vm.IsPlayerTurn && !_vm.ShowGameResult
                && !_vm.ShowInactivityOverlay && !_vm.IsSendingMove;
            RulesLink.IsVisible = true;
            RulesLink.Text = i18n.T("ngola_rules_link");
            RoundScoreLabel.IsVisible = false;
        }
        else if (isDomino && _vm.Domino != null)
        {
            DominoBoard.MyHand = _vm.Domino.MyHand;
            DominoBoard.OpponentHandCount = _vm.Domino.OpponentHandCount;
            DominoBoard.BoneyardCount = _vm.Domino.BoneyardCount;
            DominoBoard.Chain = _vm.Domino.Chain;
            DominoBoard.LeftEnd = _vm.Domino.LeftEnd;
            DominoBoard.RightEnd = _vm.Domino.RightEnd;
            DominoBoard.IsOpening = _vm.Domino.IsOpening;
            DominoBoard.OpeningTile = _vm.Domino.OpeningTile;
            DominoBoard.MustDraw = _vm.Domino.MustDraw;
            DominoBoard.MustPass = _vm.Domino.MustPass;
            DominoBoard.CanInteract = _vm.IsPlayerTurn && !_vm.ShowGameResult
                && !_vm.ShowInactivityOverlay && !_vm.IsSendingMove;
            RulesLink.IsVisible = true;
            RulesLink.Text = i18n.T("domino_rules_link");
            RoundScoreLabel.IsVisible = false;
        }
        else if (isAbbia && _vm.Abbia != null)
        {
            AbbiaBoard.Abbia = _vm.Abbia;
            AbbiaBoard.CanInteract = _vm.IsPlayerTurn && !_vm.ShowGameResult
                && !_vm.ShowInactivityOverlay && !_vm.IsSendingMove;
            RulesLink.IsVisible = true;
            RulesLink.Text = i18n.T("abbia_rules_link");
            RoundScoreLabel.IsVisible = false;
        }
        else if (isNzengue && _vm.Nzengue != null)
        {
            NzengueBoard.State = _vm.Nzengue;
            NzengueBoard.CanInteract = _vm.IsPlayerTurn && !_vm.ShowGameResult
                && !_vm.ShowInactivityOverlay && !_vm.IsSendingMove;
            RulesLink.IsVisible = true;
            RulesLink.Text = i18n.T("nzengue_rules_link");
            RoundScoreLabel.IsVisible = false;
        }
        else if (_vm.WaitingForOpponent)
        {
            RoundScoreLabel.Text = "";
            RoundScoreLabel.IsVisible = false;
            RulesLink.IsVisible = false;
        }
        else
        {
            RulesLink.IsVisible = true;
            RulesLink.Text = i18n.T("makopa_rules_link");
            var rounds = _vm.MyRoundWins + _vm.OpponentRoundWins > 0;
            RoundScoreLabel.IsVisible = rounds;
            RoundScoreLabel.Text = rounds ? string.Format(i18n.T("makopa_round_score"), _vm.MyRoundWins, _vm.OpponentRoundWins) : "";
        }

        if (!string.IsNullOrEmpty(_vm.TrickOutcomeMessage))
        {
            TrickOutcomeLabel.Text = _vm.TrickOutcomeMessage;
            TrickOutcomeLabel.IsVisible = true;
        }
        else
        {
            TrickOutcomeLabel.IsVisible = false;
        }

        var half = _vm.PotAmount > 0 ? _vm.PotAmount / 2m : 0m;
        var halfStr = half.ToString("N0");
        PotTotalLabel.Text = $"{_vm.PotAmount:N0} FCFA";
        ChipLeftText.Text = halfStr;
        ChipRightText.Text = halfStr;
        PotHintLabel.Text = string.Format(i18n.T("pot_activity_hint"), halfStr);

        var opp = _vm.OpponentDisplayName;
        if (string.IsNullOrWhiteSpace(opp))
        {
            PotOpponentBadge.IsVisible = false;
        }
        else
        {
            PotOpponentBadge.IsVisible = true;
            PotOpponentNameLabel.Text = opp.Trim();
            PotOpponentInitialsLabel.Text = InitialsFromName(opp);
        }

        if (_vm.ShowGameResult)
        {
            if (_vm.IsDraw)
                ResultTitle.Text = "It's a draw";
            else if (_vm.EndedByForfeit && _vm.WinnerPlayerName == "You")
                ResultTitle.Text = i18n.T("forfeit_win");
            else if (_vm.EndedByForfeit)
                ResultTitle.Text = i18n.T("forfeit_loss");
            else if (_vm.WinnerPlayerName == "You")
                ResultTitle.Text = i18n.T("result_you_won");
            else
                ResultTitle.Text = i18n.T("result_game_over");
        }

        InactivityOverlay.IsVisible = _vm.ShowInactivityOverlay;
        if (_vm.ShowInactivityOverlay)
        {
            InactivityMessageLabel.Text = _vm.InactivityShowButtons
                ? i18n.T("inactivity_warning_first")
                : i18n.T("inactivity_warning_second");
            InactivityCountLabel.Text = _vm.InactivityCountdownSeconds.ToString();
            InactivityButtonsRow.IsVisible = _vm.InactivityShowButtons;
            InactivityContinueBtn.IsEnabled = !_vm.InactivityActionBusy;
            InactivityCancelBtn.IsEnabled = !_vm.InactivityActionBusy;
        }
    }

    private async void OnNavigateHomeFromInactivity()
    {
        var message = _vm?.SessionLeaveMessage;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.GoToAsync("//MainTabs/Dashboard");
            if (!string.IsNullOrEmpty(message))
                await DisplayAlertAsync(Title, message, "OK");
        });
    }

    private async void OnInactivityContinue(object? sender, EventArgs e)
    {
        if (_vm == null || _vm.InactivityActionBusy)
            return;
        await _vm.ContinueInactivityAsync();
    }

    private async void OnInactivityCancel(object? sender, EventArgs e)
    {
        if (_vm == null || _vm.InactivityActionBusy)
            return;
        await _vm.CancelGameFromInactivityAsync();
    }

    private static string InitialsFromName(string name)
    {
        var t = name.Trim();
        var parts = t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[1][0]));
        return t.Length >= 2 ? t[..2].ToUpperInvariant() : char.ToUpperInvariant(t[0]).ToString();
    }

    private async void OnHandCardTapped(object? sender, TappedEventArgs e)
    {
        if (_vm == null || !_vm.IsPlayerTurn || _vm.ShowGameResult || _vm.ShowInactivityOverlay) return;
        if (sender is not Border { BindingContext: CardViewModel card }) return;
        if (!card.IsPlayable) return;
        await _vm.PlayCardAsync(card);
    }

    private async void OnDone(object? sender, EventArgs e) =>
        await TryLeaveWithForfeitConfirmAsync();

    protected override bool OnBackButtonPressed()
    {
        _ = TryLeaveWithForfeitConfirmAsync();
        return true;
    }

    private async Task TryLeaveWithForfeitConfirmAsync()
    {
        if (_vm == null)
        {
            await Shell.Current.GoToAsync("//MainTabs/Dashboard");
            return;
        }

        if (!_vm.ShouldConfirmLeave)
        {
            await Shell.Current.GoToAsync("//MainTabs/Dashboard");
            return;
        }

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        var leave = await DisplayAlertAsync(
            i18n.T("forfeit_confirm_title"),
            i18n.T("forfeit_confirm"),
            i18n.T("yes"),
            i18n.T("no"));
        if (!leave)
            return;

        await _vm.ConfirmForfeitAndLeaveAsync();
    }

    private async void OnTakeCardTapped(object? sender, EventArgs e)
    {
        if (_vm == null)
            return;

        await _vm.TakeCardAsync();
    }

    private async void OnRulesTapped(object? sender, TappedEventArgs e)
    {
        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        if (_vm?.IsKopo == true)
            await DisplayAlertAsync(i18n.T("kopo_how_to_play_title"), i18n.T("kopo_rules_body"), i18n.T("done_short"));
        else if (_vm?.IsNgola == true)
            await DisplayAlertAsync(i18n.T("ngola_how_to_play_title"), i18n.T("ngola_rules_body"), i18n.T("done_short"));
        else if (_vm?.IsDomino == true)
            await DisplayAlertAsync(i18n.T("domino_how_to_play_title"), i18n.T("domino_rules_body"), i18n.T("done_short"));
        else if (_vm?.IsAbbia == true)
            await DisplayAlertAsync(i18n.T("abbia_how_to_play_title"), i18n.T("abbia_rules_body"), i18n.T("done_short"));
        else if (_vm?.IsNzengue == true)
            await DisplayAlertAsync(i18n.T("nzengue_how_to_play_title"), i18n.T("nzengue_rules_body"), i18n.T("done_short"));
        else
            await DisplayAlertAsync(i18n.T("makopa_how_to_play_title"), i18n.T("makopa_rules_body"), i18n.T("done_short"));
    }

    private async void OnKopoCellTapped(object? sender, (int Row, int Col) e)
    {
        if (_vm == null) return;
        await _vm.OnKopoSquareClickedAsync(e.Row, e.Col);
    }

    private async void OnNgolaPitTapped(object? sender, int pitIndex)
    {
        if (_vm == null) return;
        await _vm.OnNgolaPitClickedAsync(pitIndex);
    }

    private async void OnDominoActionRequested(object? sender, (string Action, int? High, int? Low, string? End) e)
    {
        if (_vm == null) return;
        await _vm.OnDominoActionAsync(e.Action, e.High, e.Low, e.End);
    }

    private async void OnAbbiaThrowRequested(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.OnAbbiaThrowAsync();
    }

    private async void OnNzengueMoveRequested(object? sender, (int? FromPoint, int ToPoint) e)
    {
        if (_vm == null) return;
        await _vm.OnNzengueMoveAsync(e.FromPoint, e.ToPoint);
    }
}
