using Microsoft.AspNetCore.Components;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Presentation;
using Bobeta.Web.Services;
using Bobeta.Web.Services.Realtime;
using Bobeta.Client.Services;

namespace Bobeta.Web.ViewModels.Games;

public class GamePlayViewModel : ViewModelBase
{
    private readonly GamePlayService _gamePlayService;
    private readonly IGameService _gameService;
    private readonly AppStateService _appState;
    private readonly NavigationManager _nav;
    private readonly GameHubClient? _hubClient;
    private readonly GamePlayTestService? _testService;
    private readonly I18nService? _i18n;
    private CancellationTokenSource? _aiTriggerCts;

    public GamePlayViewModel(
        GamePlayService gamePlayService,
        IGameService gameService,
        AppStateService appState,
        NavigationManager nav,
        GameHubClient? hubClient = null,
        GamePlayTestService? testService = null,
        I18nService? i18n = null)
    {
        _gamePlayService = gamePlayService;
        _gameService = gameService;
        _appState = appState;
        _nav = nav;
        _hubClient = hubClient;
        _testService = testService;
        _i18n = i18n;
    }

    public string SessionId { get; private set; } = "";
    public bool IsPlayerTurn { get; private set; }
    public decimal PotAmount { get; private set; }
    public string? OpponentDisplayName { get; private set; }
    public bool WaitingForOpponent { get; private set; }

    public List<CardViewModel> PlayerCards { get; private set; } = new();
    public List<CardViewModel> OpponentCards { get; private set; } = new();

    public CardViewModel? LastPlayedCard { get; private set; }

    public string? WinnerPlayerName { get; private set; }
    public bool ShowGameResult { get; private set; }

    public Guid? CurrentPlayerId { get; private set; }

    /// <summary>Shown after a trick resolves; cleared when the next trick starts.</summary>
    public string? TrickOutcomeMessage { get; private set; }
    public bool CanTakeCard { get; private set; }

    /// <summary>Hands won in the match from this seat (API), for display alongside opponent.</summary>
    public int MyRoundWins { get; private set; }
    public int OpponentRoundWins { get; private set; }

    /// <summary>Localized “Hands won …” line during play.</summary>
    public string? MatchRoundScoreText { get; private set; }

    /// <summary>From server: we are responding to opponent&apos;s lead (client must not infer this from <see cref="LastPlayedCard"/> alone — it can be our own lead while we wait).</summary>
    public bool MustFollowLedSuit { get; private set; }

    public async Task LoadGameAsync(string sessionId)
    {
        SessionId = sessionId;
        SetLoading(true);
        ClearError();
        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                SetError("Invalid game session.");
                return;
            }

            var res = await _gameService.GetGameStateAsync(sessionGuid);
            if (!res.IsSuccess || res.Data == null)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Failed to load game.");
                return;
            }

            var state = res.Data;
            WaitingForOpponent = state.WaitingForGameStart;
            PotAmount = state.LobbyPotAmount;
            OpponentDisplayName = state.OpponentDisplayName;
            CurrentPlayerId = state.CurrentTurnPlayerId;
            IsPlayerTurn = !WaitingForOpponent && state.CurrentTurnPlayerId == _appState.State.CurrentPlayerId;
            PlayerCards = ParseCards(state.MyCards ?? new List<string>());
            LastPlayedCard = string.IsNullOrEmpty(state.LastPlayedCard) ? null : ParseCard(state.LastPlayedCard);
            OpponentCards = new List<CardViewModel>();
            MustFollowLedSuit = state.MustFollowLedSuit;

            if (state.GameOver)
                HandleGameResult(state.WinnerPlayerId);
            else
                ShowGameResult = false;

            ApplyTrickOutcomeMessage(state.LastTrickWinnerPlayerId);
            ApplyMatchRoundScore(state);
            RefreshHandPlayability();

            if (_hubClient != null)
            {
                try
                {
                    await _hubClient.ConnectAsync(sessionId);
                    _hubClient.OnGameStateUpdated -= ApplyGameStateFromHub;
                    _hubClient.OnGameStateUpdated += ApplyGameStateFromHub;
                    _hubClient.OnOpponentMove -= ApplyOpponentMoveFromHub;
                    _hubClient.OnOpponentMove += ApplyOpponentMoveFromHub;
                    _hubClient.OnGameResult -= ApplyGameResultFromHub;
                    _hubClient.OnGameResult += ApplyGameResultFromHub;
                    _hubClient.OnGameStarted -= OnGameStartedReload;
                    _hubClient.OnGameStarted += OnGameStartedReload;
                    _hubClient.OnReconnected -= OnHubReconnected;
                    _hubClient.OnReconnected += OnHubReconnected;
                }
                catch
                {
                    // REST game state already loaded — realtime is optional. SignalR negotiate/CORS issues must not blank the table.
                }
            }

            ScheduleAiOpponentIfNeeded(sessionGuid);

            RaiseStateChanged();
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    public async Task PlayCardAsync(CardViewModel card) =>
        await SubmitCardAsync(card, enforceLocalFollowSuitValidation: true);

    public async Task TakeCardAsync()
    {
        if (!CanTakeCard || string.IsNullOrEmpty(SessionId) || !Guid.TryParse(SessionId, out var sessionGuid))
            return;

        SetLoading(true);
        ClearError();
        try
        {
            var res = await _gamePlayService.VoidFollowDrawAsync(sessionGuid);
            if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Could not draw.");
                return;
            }

            if (res.Data != null)
            {
                WaitingForOpponent = res.Data.WaitingForGameStart;
                PotAmount = res.Data.LobbyPotAmount;
                OpponentDisplayName = res.Data.OpponentDisplayName;
                CurrentPlayerId = res.Data.CurrentTurnPlayerId;
                IsPlayerTurn = !WaitingForOpponent && res.Data.CurrentTurnPlayerId == _appState.State.CurrentPlayerId;
                PlayerCards = ParseCards(res.Data.MyCards ?? new List<string>());
                LastPlayedCard = string.IsNullOrEmpty(res.Data.LastPlayedCard) ? null : ParseCard(res.Data.LastPlayedCard);
                MustFollowLedSuit = res.Data.MustFollowLedSuit;
                if (res.Data.GameOver)
                    HandleGameResult(res.Data.WinnerPlayerId);
                ApplyTrickOutcomeMessage(res.Data.LastTrickWinnerPlayerId);
                ApplyMatchRoundScore(res.Data);
            }

            RefreshHandPlayability();
            RaiseStateChanged();
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task SubmitCardAsync(CardViewModel card, bool enforceLocalFollowSuitValidation)
    {
        if (IsLoading || !IsPlayerTurn || string.IsNullOrEmpty(SessionId)) return;
        if (!Guid.TryParse(SessionId, out var sessionGuid)) return;

        var handStr = PlayerCards.Select(c => c.DisplayValue).ToList();
        if (enforceLocalFollowSuitValidation &&
            MustFollowLedSuit &&
            !MakopaFollowSuit.IsLegalPlay(card.DisplayValue, LastPlayedCard?.DisplayValue, handStr))
        {
            SetError(_i18n?.T("invalid_move_follow_suit") ?? "You must follow the led suit when you can.");
            return;
        }

        SetLoading(true);
        ClearError();
        try
        {
            var request = new GameMoveRequest
            {
                Suit = card.Suit,
                Rank = int.TryParse(card.Rank, out var r) ? r : 0
            };
            var res = await _gamePlayService.PlayCardAsync(sessionGuid, request);
            if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                if (res.StatusCode == 400)
                    SetError(_i18n?.T("invalid_move_follow_suit") ?? res.ErrorMessage ?? "Invalid move.");
                else
                    SetError(res.ErrorMessage ?? "Failed to play card.");
                return;
            }

            if (res.Data != null)
            {
                WaitingForOpponent = res.Data.WaitingForGameStart;
                PotAmount = res.Data.LobbyPotAmount;
                OpponentDisplayName = res.Data.OpponentDisplayName;
                CurrentPlayerId = res.Data.CurrentTurnPlayerId;
                IsPlayerTurn = !WaitingForOpponent && res.Data.CurrentTurnPlayerId == _appState.State.CurrentPlayerId;
                PlayerCards = ParseCards(res.Data.MyCards ?? new List<string>());
                LastPlayedCard = string.IsNullOrEmpty(res.Data.LastPlayedCard) ? null : ParseCard(res.Data.LastPlayedCard);
                MustFollowLedSuit = res.Data.MustFollowLedSuit;
                if (res.Data.GameOver)
                    HandleGameResult(res.Data.WinnerPlayerId);
                ApplyTrickOutcomeMessage(res.Data.LastTrickWinnerPlayerId);
                ApplyMatchRoundScore(res.Data);
            }

            RefreshHandPlayability();
            RaiseStateChanged();
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ApplyMatchRoundScore(GameStateViewModel? state)
    {
        if (state == null || state.WaitingForGameStart)
        {
            MyRoundWins = OpponentRoundWins = 0;
            MatchRoundScoreText = null;
            return;
        }

        MyRoundWins = state.MyRoundWins;
        OpponentRoundWins = state.OpponentRoundWins;
        if (MyRoundWins == 0 && OpponentRoundWins == 0)
        {
            MatchRoundScoreText = null;
            return;
        }

        MatchRoundScoreText = _i18n != null
            ? string.Format(_i18n.T("makopa_round_score"), MyRoundWins, OpponentRoundWins)
            : $"Hands won: {MyRoundWins}\u2013{OpponentRoundWins}";
    }

    private void ApplyTrickOutcomeMessage(Guid? lastTrickWinnerPlayerId)
    {
        if (lastTrickWinnerPlayerId == null)
            TrickOutcomeMessage = null;
        else if (lastTrickWinnerPlayerId == _appState.State.CurrentPlayerId)
            TrickOutcomeMessage = _i18n?.T("trick_outcome_you") ?? "You took this trick.";
        else
            TrickOutcomeMessage = _i18n?.T("trick_outcome_opponent") ?? "Opponent took this trick.";
    }

    public void HandleOpponentMove(Guid? currentTurnPlayerId, string? lastPlayedCard, bool gameOver, Guid? winnerPlayerId)
    {
        CurrentPlayerId = currentTurnPlayerId;
        IsPlayerTurn = currentTurnPlayerId == _appState.State.CurrentPlayerId;
        if (!string.IsNullOrEmpty(lastPlayedCard))
            LastPlayedCard = ParseCard(lastPlayedCard);
        if (gameOver)
            HandleGameResult(winnerPlayerId);
        RefreshHandPlayability();
        RaiseStateChanged();
    }

    public void HandleGameResult(Guid? winnerPlayerId)
    {
        ShowGameResult = true;
        WinnerPlayerName = winnerPlayerId == _appState.State.CurrentPlayerId ? "You" : "Opponent";
        RefreshHandPlayability();
        RaiseStateChanged();
    }

    private static List<CardViewModel> ParseCards(IEnumerable<string> cards)
    {
        return cards.Select(ParseCard).ToList();
    }

    private static CardViewModel ParseCard(string value)
    {
        var parts = value.Split('-', '_');
        var suit = parts.Length > 0 ? parts[0] : "0";
        var rank = parts.Length > 1 ? parts[1] : value;
        return new CardViewModel
        {
            Suit = suit,
            Rank = rank,
            DisplayValue = value,
            CssClass = ""
        };
    }

    private void ApplyGameStateFromHub(GameStateViewModel state)
    {
        WaitingForOpponent = state.WaitingForGameStart;
        PotAmount = state.LobbyPotAmount;
        OpponentDisplayName = state.OpponentDisplayName;
        PlayerCards = ParseCards(state.MyCards ?? new List<string>());
        LastPlayedCard = string.IsNullOrEmpty(state.LastPlayedCard) ? null : ParseCard(state.LastPlayedCard);
        MustFollowLedSuit = state.MustFollowLedSuit;
        CurrentPlayerId = state.CurrentTurnPlayerId;
        IsPlayerTurn = !WaitingForOpponent && state.CurrentTurnPlayerId == _appState.State.CurrentPlayerId;
        if (state.GameOver)
            HandleGameResult(state.WinnerPlayerId);
        else
            ShowGameResult = false;
        ApplyTrickOutcomeMessage(state.LastTrickWinnerPlayerId);
        ApplyMatchRoundScore(state);
        RefreshHandPlayability();
        RaiseStateChanged();
    }

    private void ApplyOpponentMoveFromHub(Guid moverPlayerId, string cardSuitRank)
    {
        if (moverPlayerId == _appState.State.CurrentPlayerId)
            return;
        _aiTriggerCts?.Cancel();
        // Turn/trick state is authoritative from GameState or the play-card HTTP response.
        // Ignoring raw move events here prevents stale out-of-order updates from corrupting local hand playability.
        RefreshHandPlayability();
        RaiseStateChanged();
    }

    private void RefreshHandPlayability()
    {
        var canInteract = IsPlayerTurn && !WaitingForOpponent && !ShowGameResult;
        var enforceFollow = MustFollowLedSuit;
        var last = LastPlayedCard?.DisplayValue;
        var hand = PlayerCards.Select(c => c.DisplayValue).ToList();
        var needsVoid = canInteract && enforceFollow && MakopaFollowSuit.ResponderNeedsVoidFollow(last, hand);
        foreach (var c in PlayerCards)
        {
            if (!canInteract)
                c.IsPlayable = true;
            else if (needsVoid)
                c.IsPlayable = false;
            else if (enforceFollow)
                c.IsPlayable = MakopaFollowSuit.IsLegalPlay(c.DisplayValue, last, hand);
            else
                c.IsPlayable = true;
        }

        CanTakeCard = needsVoid;
    }

    private void ApplyGameResultFromHub(Guid? winnerPlayerId)
    {
        HandleGameResult(winnerPlayerId);
    }

    /// <summary>Local test: if it's opponent's turn and no move after 5s, simulate AI move then reload.</summary>
    private async void ScheduleAiOpponentIfNeeded(Guid sessionGuid)
    {
        _aiTriggerCts?.Cancel();
        _aiTriggerCts = new CancellationTokenSource();
        if (ShowGameResult || WaitingForOpponent || IsPlayerTurn || _testService == null) return;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), _aiTriggerCts.Token);
            if (_aiTriggerCts.Token.IsCancellationRequested) return;
            var ok = await _testService.SimulateAiMoveAsync(sessionGuid, _aiTriggerCts.Token);
            if (ok && !string.IsNullOrEmpty(SessionId))
                await LoadGameAsync(SessionId);
        }
        catch (OperationCanceledException) { }
        catch { /* ignore */ }
    }

    private void OnGameStartedReload()
    {
        if (!string.IsNullOrEmpty(SessionId))
            _ = LoadGameAsync(SessionId);
    }

    private void OnHubReconnected()
    {
        if (!string.IsNullOrEmpty(SessionId))
            _ = LoadGameAsync(SessionId);
    }

    public async ValueTask DisposeAsync()
    {
        _aiTriggerCts?.Cancel();
        if (_hubClient != null)
        {
            _hubClient.OnGameStateUpdated -= ApplyGameStateFromHub;
            _hubClient.OnOpponentMove -= ApplyOpponentMoveFromHub;
            _hubClient.OnGameResult -= ApplyGameResultFromHub;
            _hubClient.OnGameStarted -= OnGameStartedReload;
            _hubClient.OnReconnected -= OnHubReconnected;
            await _hubClient.DisconnectAsync();
        }
    }
}
