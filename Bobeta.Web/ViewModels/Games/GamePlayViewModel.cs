using Microsoft.AspNetCore.Components;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
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
    private CancellationTokenSource? _aiTriggerCts;

    public GamePlayViewModel(
        GamePlayService gamePlayService,
        IGameService gameService,
        AppStateService appState,
        NavigationManager nav,
        GameHubClient? hubClient = null,
        GamePlayTestService? testService = null)
    {
        _gamePlayService = gamePlayService;
        _gameService = gameService;
        _appState = appState;
        _nav = nav;
        _hubClient = hubClient;
        _testService = testService;
    }

    public string SessionId { get; private set; } = "";
    public bool IsPlayerTurn { get; private set; }
    public decimal PotAmount { get; private set; }

    public List<CardViewModel> PlayerCards { get; private set; } = new();
    public List<CardViewModel> OpponentCards { get; private set; } = new();

    public CardViewModel? LastPlayedCard { get; private set; }

    public string? WinnerPlayerName { get; private set; }
    public bool ShowGameResult { get; private set; }

    public Guid? CurrentPlayerId { get; private set; }

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
            CurrentPlayerId = state.CurrentTurnPlayerId;
            IsPlayerTurn = state.CurrentTurnPlayerId == _appState.State.CurrentPlayerId;
            PlayerCards = ParseCards(state.MyCards ?? new List<string>());
            LastPlayedCard = string.IsNullOrEmpty(state.LastPlayedCard) ? null : ParseCard(state.LastPlayedCard);
            OpponentCards = new List<CardViewModel>();

            if (state.GameOver)
                HandleGameResult(state.WinnerPlayerId);
            else
                ShowGameResult = false;

            if (_hubClient != null)
            {
                await _hubClient.ConnectAsync(sessionId);
                _hubClient.OnGameStateUpdated += ApplyGameStateFromHub;
                _hubClient.OnOpponentMove += ApplyOpponentMoveFromHub;
                _hubClient.OnGameResult += ApplyGameResultFromHub;
                _hubClient.OnReconnected += () => _ = LoadGameAsync(sessionId);
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
                SetError(res.ErrorMessage ?? "Failed to play card.");
                return;
            }

            if (res.Data != null)
            {
                CurrentPlayerId = res.Data.CurrentTurnPlayerId;
                IsPlayerTurn = res.Data.CurrentTurnPlayerId == _appState.State.CurrentPlayerId;
                PlayerCards = ParseCards(res.Data.MyCards ?? new List<string>());
                LastPlayedCard = string.IsNullOrEmpty(res.Data.LastPlayedCard) ? null : ParseCard(res.Data.LastPlayedCard);
                if (res.Data.GameOver)
                    HandleGameResult(res.Data.WinnerPlayerId);
            }

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

    public void HandleOpponentMove(Guid? currentTurnPlayerId, string? lastPlayedCard, bool gameOver, Guid? winnerPlayerId)
    {
        CurrentPlayerId = currentTurnPlayerId;
        IsPlayerTurn = currentTurnPlayerId == _appState.State.CurrentPlayerId;
        if (!string.IsNullOrEmpty(lastPlayedCard))
            LastPlayedCard = ParseCard(lastPlayedCard);
        if (gameOver)
            HandleGameResult(winnerPlayerId);
        RaiseStateChanged();
    }

    public void HandleGameResult(Guid? winnerPlayerId)
    {
        ShowGameResult = true;
        WinnerPlayerName = winnerPlayerId == _appState.State.CurrentPlayerId ? "You" : "Opponent";
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
            CssClass = "rounded-lg border border-border bg-card p-2 text-foreground"
        };
    }

    private void ApplyGameStateFromHub(GameStateViewModel state)
    {
        PlayerCards = ParseCards(state.MyCards ?? new List<string>());
        LastPlayedCard = string.IsNullOrEmpty(state.LastPlayedCard) ? null : ParseCard(state.LastPlayedCard);
        CurrentPlayerId = state.CurrentTurnPlayerId;
        IsPlayerTurn = state.CurrentTurnPlayerId == _appState.State.CurrentPlayerId;
        if (state.GameOver)
            HandleGameResult(state.WinnerPlayerId);
        else
            ShowGameResult = false;
        RaiseStateChanged();
    }

    private void ApplyOpponentMoveFromHub(string cardSuitRank)
    {
        _aiTriggerCts?.Cancel();
        LastPlayedCard = ParseCard(cardSuitRank);
        IsPlayerTurn = true;
        RaiseStateChanged();
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
        if (ShowGameResult || IsPlayerTurn || _testService == null) return;
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
}
