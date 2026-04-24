using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Presentation;
using Bobeta.Client.Services;
using Bobeta.Mobile.Services;
using Bobeta.Mobile.Services.Realtime;

namespace Bobeta.Mobile.ViewModels.Games;

public class GamePlayViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly GamePlayService _gamePlayService;
    private readonly IGameService _gameService;
    private readonly AppStateService _appState;
    private readonly GameHubClient? _hubClient;
    private readonly GamePlayTestService? _testService;
    private readonly I18nService _i18n;
    private CancellationTokenSource? _aiTriggerCts;

    public GamePlayViewModel(
        GamePlayService gamePlayService,
        IGameService gameService,
        AppStateService appState,
        I18nService i18n,
        GameHubClient? hubClient = null,
        GamePlayTestService? testService = null)
    {
        _gamePlayService = gamePlayService;
        _gameService = gameService;
        _appState = appState;
        _i18n = i18n;
        _hubClient = hubClient;
        _testService = testService;
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

    public string? TrickOutcomeMessage { get; private set; }

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

            if (state.GameOver)
                HandleGameResult(state.WinnerPlayerId);
            else
                ShowGameResult = false;

            ApplyTrickOutcomeMessage(state.LastTrickWinnerPlayerId);
            RefreshHandPlayability();

            if (_hubClient != null)
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

    public async Task PlayCardAsync(CardViewModel card)
    {
        if (IsLoading || !IsPlayerTurn || string.IsNullOrEmpty(SessionId)) return;
        if (!Guid.TryParse(SessionId, out var sessionGuid)) return;

        var handStr = PlayerCards.Select(c => c.DisplayValue).ToList();
        if (MakopaFollowSuit.RulesApply(IsPlayerTurn, WaitingForOpponent, ShowGameResult) &&
            !MakopaFollowSuit.IsLegalPlay(card.DisplayValue, LastPlayedCard?.DisplayValue, handStr))
        {
            SetError(_i18n.T("invalid_move_follow_suit"));
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
                if (res.StatusCode == 400)
                    SetError(_i18n.T("invalid_move_follow_suit"));
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
                if (res.Data.GameOver)
                    HandleGameResult(res.Data.WinnerPlayerId);
                ApplyTrickOutcomeMessage(res.Data.LastTrickWinnerPlayerId);
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

    private void ApplyTrickOutcomeMessage(Guid? lastTrickWinnerPlayerId)
    {
        if (lastTrickWinnerPlayerId == null)
            TrickOutcomeMessage = null;
        else if (lastTrickWinnerPlayerId == _appState.State.CurrentPlayerId)
            TrickOutcomeMessage = _i18n.T("trick_outcome_you");
        else
            TrickOutcomeMessage = _i18n.T("trick_outcome_opponent");
    }

    public void HandleGameResult(Guid? winnerPlayerId)
    {
        ShowGameResult = true;
        WinnerPlayerName = winnerPlayerId == _appState.State.CurrentPlayerId ? "You" : "Opponent";
        RefreshHandPlayability();
        RaiseStateChanged();
    }

    private static List<CardViewModel> ParseCards(IEnumerable<string> cards) =>
        cards.Select(ParseCard).ToList();

    private static CardViewModel ParseCard(string value)
    {
        var parts = value.Split('-', '_');
        var suit = parts.Length > 0 ? parts[0] : "0";
        var rank = parts.Length > 1 ? parts[1] : value;
        return new CardViewModel
        {
            Suit = suit,
            Rank = rank,
            DisplayValue = value
        };
    }

    private void ApplyGameStateFromHub(GameStateViewModel state)
    {
        WaitingForOpponent = state.WaitingForGameStart;
        PotAmount = state.LobbyPotAmount;
        OpponentDisplayName = state.OpponentDisplayName;
        PlayerCards = ParseCards(state.MyCards ?? new List<string>());
        LastPlayedCard = string.IsNullOrEmpty(state.LastPlayedCard) ? null : ParseCard(state.LastPlayedCard);
        CurrentPlayerId = state.CurrentTurnPlayerId;
        IsPlayerTurn = !WaitingForOpponent && state.CurrentTurnPlayerId == _appState.State.CurrentPlayerId;
        if (state.GameOver)
            HandleGameResult(state.WinnerPlayerId);
        else
            ShowGameResult = false;
        ApplyTrickOutcomeMessage(state.LastTrickWinnerPlayerId);
        RefreshHandPlayability();
        RaiseStateChanged();
    }

    private void ApplyOpponentMoveFromHub(Guid moverPlayerId, string cardSuitRank)
    {
        if (moverPlayerId == _appState.State.CurrentPlayerId)
            return;
        _aiTriggerCts?.Cancel();
        LastPlayedCard = ParseCard(cardSuitRank);
        RefreshHandPlayability();
        RaiseStateChanged();
    }

    private void RefreshHandPlayability()
    {
        var rules = MakopaFollowSuit.RulesApply(IsPlayerTurn, WaitingForOpponent, ShowGameResult);
        var last = LastPlayedCard?.DisplayValue;
        var hand = PlayerCards.Select(c => c.DisplayValue).ToList();
        foreach (var c in PlayerCards)
            c.IsPlayable = !rules || MakopaFollowSuit.IsLegalPlay(c.DisplayValue, last, hand);
    }

    private void ApplyGameResultFromHub(Guid? winnerPlayerId) => HandleGameResult(winnerPlayerId);

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

    private void OnHubReconnected()
    {
        if (!string.IsNullOrEmpty(SessionId))
            _ = LoadGameAsync(SessionId);
    }

    private void OnGameStartedReload()
    {
        if (!string.IsNullOrEmpty(SessionId))
            _ = LoadGameAsync(SessionId);
    }
}
