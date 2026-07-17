using Microsoft.AspNetCore.Components;
using Bobeta.Client.Contracts;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Presentation;
using Bobeta.Web.Services;
using Bobeta.Web.Shared.Services.Realtime;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.ViewModels;

namespace Bobeta.Web.ViewModels.Games;

public class GamePlayViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly GamePlayService _gamePlayService;
    private readonly GamePlaySessionSync _sessionSync;
    private readonly AppStateService _appState;
    private readonly NavigationManager _nav;
    private readonly GameHubClient? _hubClient;
    private readonly GamePlayTestService? _testService;
    private readonly I18nService? _i18n;
    private readonly GamePlayTableState _table = new();
    private CancellationTokenSource? _aiTriggerCts;
    private CancellationTokenSource? _inactivityCountdownCts;
    private CancellationTokenSource? _fallbackPollCts;
    private DateTime? _inactivityDeadlineUtc;
    private readonly SemaphoreSlim _moveGate = new(1, 1);
    private bool _leaveInProgress;

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
        _sessionSync = new GamePlaySessionSync(gameService);
        _appState = appState;
        _nav = nav;
        _hubClient = hubClient;
        _testService = testService;
        _i18n = i18n;
    }

    public string SessionId { get; private set; } = "";
    public GameVariant Variant => _table.Variant;
    public KopoStateDto? Kopo => _table.Kopo;
    public NgolaStateDto? Ngola => _table.Ngola;
    public bool IsPlayerTurn => _table.IsPlayerTurn;
    public decimal PotAmount => _table.PotAmount;
    public string? OpponentDisplayName => _table.OpponentDisplayName;
    public bool WaitingForOpponent => _table.WaitingForOpponent;
    public List<CardViewModel> PlayerCards => _table.PlayerCards;
    public List<CardViewModel> OpponentCards { get; private set; } = new();
    public CardViewModel? LastPlayedCard => _table.LastPlayedCard;
    public string? WinnerPlayerName => _table.WinnerPlayerName;
    public bool ShowGameResult => _table.ShowGameResult;
    public Guid? CurrentPlayerId => _table.CurrentPlayerId;
    public Guid? MyPlayerId => _appState.State.CurrentPlayerId;
    public string? TrickOutcomeMessage => _table.TrickOutcomeMessage;
    public bool CanTakeCard => _table.CanTakeCard;
    public int MyRoundWins => _table.MyRoundWins;
    public int OpponentRoundWins => _table.OpponentRoundWins;
    public string? MatchRoundScoreText => _table.MatchRoundScoreText;
    public bool MustFollowLedSuit => _table.MustFollowLedSuit;

    public bool ShowInactivityOverlay { get; private set; }
    public bool InactivityShowButtons { get; private set; }
    public int InactivityCountdownSeconds { get; private set; }
    public bool InactivityActionBusy { get; private set; }
    public bool IsSendingMove => IsLoading && (Variant != GameVariant.Makopa || PlayerCards.Count > 0);

    public bool ShowLoadingShell => GamePlayUiHelper.ShowLoadingShell(
        IsLoading, Variant, Kopo != null || Ngola != null, PlayerCards.Count, WaitingForOpponent);

    private readonly List<KopoSquareDto> _kopoPath = new();
    public IReadOnlyList<KopoSquareDto> KopoSelectionPath => _kopoPath;

    private bool BlockInteraction => ShowInactivityOverlay;

    public async Task LoadGameAsync(string sessionId)
    {
        SessionId = sessionId;
        SetLoading(true);
        ClearError();
        var notifyInactivityReady = false;
        var hubPausedThisLoad = false;
        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                SetError("Invalid game session.");
                return;
            }

            if (_hubClient != null)
            {
                WireInactivityHubEvents();
                try
                {
                    await _hubClient.ConnectAsync(sessionId);
                    await _hubClient.PauseInactivityAsync(sessionId);
                    hubPausedThisLoad = true;
                    _hubClient.OnGameStateUpdated -= OnGameStateFromHub;
                    _hubClient.OnGameStateUpdated += OnGameStateFromHub;
                    _hubClient.OnGameResult -= OnGameResultFromHub;
                    _hubClient.OnGameResult += OnGameResultFromHub;
                    _hubClient.OnGameStarted -= OnGameStartedReload;
                    _hubClient.OnGameStarted += OnGameStartedReload;
                    _hubClient.OnReconnected -= OnHubReconnected;
                    _hubClient.OnReconnected += OnHubReconnected;
                }
                catch
                {
                    // Table loads from HTTP; hub adds live updates when available.
                }
            }

            var sync = await _sessionSync.FetchAndApplyAsync(
                sessionGuid, _table, MyPlayerId, BlockInteraction, UiTrickYou, UiTrickOpponent, UiRoundScore);
            if (sync.StatusCode == 401 && await _appState.HandleUnauthorizedAsync(401, _nav))
                return;
            if (sync.Apply == GamePlayStateApplier.ApplyResult.SessionEnded || sync.StatusCode == 404)
            {
                await LeaveEndedSessionAsync(likelyInactivity: true);
                return;
            }

            if (sync.Apply == null)
            {
                SetError(sync.ErrorMessage ?? "Failed to load game.");
                return;
            }

            OpponentCards = new List<CardViewModel>();
            notifyInactivityReady = !WaitingForOpponent && !ShowGameResult;
            ScheduleAiOpponentIfNeeded(sessionGuid);
            StartSessionLivenessPoll();
            RaiseStateChanged();
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
            if (_hubClient != null && Guid.TryParse(sessionId, out _) && hubPausedThisLoad)
            {
                try
                {
                    await _hubClient.ResumeInactivityAsync(sessionId);
                    if (notifyInactivityReady)
                        await _hubClient.NotifyGameReadyForInactivityAsync(sessionId);
                }
                catch { /* hub optional */ }
            }
        }
    }

    private void OnGameStateFromHub(GameStateViewModel state) { _ = ApplyAuthoritativeStateAsync(state); }

    private void OnGameResultFromHub(Guid? winnerId) { _ = SyncGameStateFromServerAsync(); }

    private async Task ApplyAuthoritativeStateAsync(GameStateViewModel state)
    {
        var result = GamePlayStateApplier.ApplyAuthoritativeState(
            _table, state, MyPlayerId, BlockInteraction, UiTrickYou, UiTrickOpponent, UiRoundScore);
        if (result == GamePlayStateApplier.ApplyResult.SessionEnded)
            await LeaveEndedSessionAsync(likelyInactivity: true);
        else
            RaiseStateChanged();
    }

    private async Task SyncGameStateFromServerAsync()
    {
        if (!Guid.TryParse(SessionId, out var sessionGuid))
            return;
        var sync = await _sessionSync.FetchAndApplyAsync(
            sessionGuid, _table, MyPlayerId, BlockInteraction, UiTrickYou, UiTrickOpponent, UiRoundScore);
        if (sync.Apply == GamePlayStateApplier.ApplyResult.SessionEnded || sync.StatusCode == 404)
            await LeaveEndedSessionAsync(likelyInactivity: true);
        else
            RaiseStateChanged();
    }

    private string UiTrickYou() => _i18n?.T("trick_outcome_you") ?? "You took this trick.";
    private string UiTrickOpponent() => _i18n?.T("trick_outcome_opponent") ?? "Opponent took this trick.";
    private string UiRoundScore(int my, int opp) =>
        _i18n != null ? string.Format(_i18n.T("makopa_round_score"), my, opp) : $"Hands won: {my}\u2013{opp}";

    private void WireInactivityHubEvents()
    {
        if (_hubClient == null) return;
        _hubClient.OnInactivityWarning -= OnInactivityWarningFromHub;
        _hubClient.OnInactivityWarning += OnInactivityWarningFromHub;
        _hubClient.OnInactivityWarningDismissed -= OnInactivityDismissedFromHub;
        _hubClient.OnInactivityWarningDismissed += OnInactivityDismissedFromHub;
        _hubClient.OnGameEndedByInactivity -= OnGameEndedByInactivityFromHub;
        _hubClient.OnGameEndedByInactivity += OnGameEndedByInactivityFromHub;
    }

    private void OnInactivityWarningFromHub(InactivityWarningPayload payload)
    {
        ShowInactivityOverlay = true;
        InactivityShowButtons = payload.ShowButtons;
        _inactivityDeadlineUtc = payload.DecisionDeadlineUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(payload.DecisionDeadlineUtc, DateTimeKind.Utc)
            : payload.DecisionDeadlineUtc.ToUniversalTime();
        StartInactivityCountdown();
        GamePlayStateApplier.RefreshHandPlayability(_table, BlockInteraction);
        RaiseStateChanged();
    }

    private void OnInactivityDismissedFromHub()
    {
        StopInactivityCountdown();
        ShowInactivityOverlay = false;
        _inactivityDeadlineUtc = null;
        GamePlayStateApplier.RefreshHandPlayability(_table, BlockInteraction);
        RaiseStateChanged();
    }

    private void OnGameEndedByInactivityFromHub() => _ = LeaveEndedSessionAsync(likelyInactivity: true);

    private void StartInactivityCountdown()
    {
        StopInactivityCountdown();
        _inactivityCountdownCts = new CancellationTokenSource();
        _ = RunInactivityCountdownAsync(_inactivityCountdownCts.Token);
    }

    private async Task RunInactivityCountdownAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && ShowInactivityOverlay && _inactivityDeadlineUtc is { } d)
        {
            InactivityCountdownSeconds = Math.Max(0, (int)Math.Ceiling((d - DateTime.UtcNow).TotalSeconds));
            if (InactivityCountdownSeconds <= 0 && InactivityShowButtons)
            {
                InactivityShowButtons = false;
                RaiseStateChanged();
                await TryResolveInactivityAfterDeadlineAsync();
                break;
            }

            RaiseStateChanged();
            if (InactivityCountdownSeconds <= 0)
                break;
            try { await Task.Delay(200, token); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void StopInactivityCountdown()
    {
        _inactivityCountdownCts?.Cancel();
        _inactivityCountdownCts = null;
    }

    public async Task ContinueInactivityAsync()
    {
        if (InactivityActionBusy || !Guid.TryParse(SessionId, out var sessionGuid))
            return;
        InactivityActionBusy = true;
        RaiseStateChanged();
        try
        {
            if (_hubClient != null)
                await _hubClient.InactivityContinueAsync(SessionId);
            await _gamePlayService.ContinueInactivityAsync(sessionGuid);
        }
        finally
        {
            InactivityActionBusy = false;
            RaiseStateChanged();
        }
    }

    public async Task CancelGameFromInactivityAsync()
    {
        if (InactivityActionBusy || !Guid.TryParse(SessionId, out var sessionGuid))
            return;
        InactivityActionBusy = true;
        RaiseStateChanged();
        try
        {
            if (_hubClient != null)
                await _hubClient.InactivityCancelGameAsync(SessionId);
            await _gamePlayService.CancelInactivityAsync(sessionGuid);
            await LeaveEndedSessionAsync(likelyInactivity: true);
        }
        finally
        {
            InactivityActionBusy = false;
            RaiseStateChanged();
        }
    }

    private async Task TryResolveInactivityAfterDeadlineAsync()
    {
        await SyncGameStateFromServerAsync();
        if (!ShowInactivityOverlay)
            return;
        await LeaveEndedSessionAsync(likelyInactivity: true);
    }

    private Task LeaveEndedSessionAsync(bool likelyInactivity)
    {
        if (_leaveInProgress)
            return Task.CompletedTask;
        _leaveInProgress = true;

        StopSessionLivenessPoll();
        StopInactivityCountdown();
        ShowInactivityOverlay = false;
        _inactivityDeadlineUtc = null;
        InactivityShowButtons = false;
        InactivityActionBusy = false;
        ClearError();
        var message = likelyInactivity
            ? _i18n?.T("game_cancelled_inactivity") ?? "This game was ended due to inactivity."
            : _i18n?.T("game_session_ended") ?? "This game is no longer in progress.";
        _nav.NavigateTo($"/dashboard?ended={(likelyInactivity ? "inactivity" : "session")}&message={Uri.EscapeDataString(message)}");
        return Task.CompletedTask;
    }

    public Task PlayCardAsync(CardViewModel card) => SubmitCardAsync(card);

    public async Task OnKopoSquareClickedAsync(int row, int col)
    {
        if (Variant != GameVariant.Kopo || !IsPlayerTurn || BlockInteraction || !Guid.TryParse(SessionId, out var sessionGuid))
            return;
        var kopo = Kopo;
        if (kopo == null || MyPlayerId is not { } myId)
            return;

        if (_kopoPath.Count == 0)
        {
            var piece = kopo.Pieces.FirstOrDefault(p => p.Row == row && p.Col == col);
            if (kopo.MustContinueChain && kopo.ChainPieceId is { } chainId)
                piece = kopo.Pieces.FirstOrDefault(p => p.Id == chainId);
            if (piece == null || piece.OwnerId != myId)
                return;
            _kopoPath.Add(new KopoSquareDto { Row = row, Col = col });
            RaiseStateChanged();
            return;
        }

        _kopoPath.Add(new KopoSquareDto { Row = row, Col = col });
        await SubmitKopoPathAsync(sessionGuid);
    }

    private async Task SubmitKopoPathAsync(Guid sessionGuid)
    {
        if (!await _moveGate.WaitAsync(0))
            return;
        SetLoading(true);
        ClearError();
        var path = _kopoPath.ToList();
        _kopoPath.Clear();
        try
        {
            var res = await _gamePlayService.ApplyKopoMoveAsync(sessionGuid, path);
            if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                await HandleMoveFailureAsync(res);
                return;
            }

            if (res.Data != null)
            {
                await ApplyAuthoritativeStateAsync(res.Data);
                if (res.Data.Kopo?.MustContinueChain == true && res.Data.Kopo.ChainPieceId is { } cid)
                {
                    var p = res.Data.Kopo.Pieces.FirstOrDefault(x => x.Id == cid);
                    if (p != null)
                        _kopoPath.Add(new KopoSquareDto { Row = p.Row, Col = p.Col });
                }
            }
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
            await SyncGameStateFromServerAsync();
        }
        finally
        {
            SetLoading(false);
            _moveGate.Release();
            RaiseStateChanged();
        }
    }

    public async Task OnNgolaPitClickedAsync(int pitIndex)
    {
        if (Variant != GameVariant.Ngola || !IsPlayerTurn || BlockInteraction
            || !Guid.TryParse(SessionId, out var sessionGuid) || Ngola == null
            || pitIndex < 0 || pitIndex >= Ngola.MyPits.Count || Ngola.MyPits[pitIndex] < 2)
            return;
        if (!await _moveGate.WaitAsync(0))
            return;

        SetLoading(true);
        ClearError();
        try
        {
            var res = await _gamePlayService.ApplyNgolaMoveAsync(sessionGuid, pitIndex);
            if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                await HandleMoveFailureAsync(res);
                return;
            }
            if (res.Data != null)
                await ApplyAuthoritativeStateAsync(res.Data);
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
            await SyncGameStateFromServerAsync();
        }
        finally
        {
            SetLoading(false);
            _moveGate.Release();
            RaiseStateChanged();
        }
    }

    public async Task TakeCardAsync()
    {
        if (!CanTakeCard || !Guid.TryParse(SessionId, out var sessionGuid))
            return;
        if (!await _moveGate.WaitAsync(0))
            return;

        SetLoading(true);
        ClearError();
        try
        {
            if (string.IsNullOrEmpty(LastPlayedCard?.DisplayValue))
            {
                await SyncGameStateFromServerAsync();
                return;
            }

            var handStr = PlayerCards.Select(c => c.DisplayValue).ToList();
            if (!MakopaFollowSuit.ResponderNeedsVoidFollow(LastPlayedCard.DisplayValue, handStr))
            {
                SetError(_i18n?.T("invalid_move_follow_suit") ?? "You must follow the led suit when you can.");
                return;
            }

            var res = await _gamePlayService.VoidFollowDrawAsync(sessionGuid);
            if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                await HandleMoveFailureAsync(res);
                return;
            }

            if (res.Data != null)
                await ApplyAuthoritativeStateAsync(res.Data);
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
            await SyncGameStateFromServerAsync();
        }
        finally
        {
            SetLoading(false);
            _moveGate.Release();
        }
    }

    private async Task SubmitCardAsync(CardViewModel card)
    {
        if (!IsPlayerTurn || !card.IsPlayable || !Guid.TryParse(SessionId, out var sessionGuid))
            return;
        if (!await _moveGate.WaitAsync(0))
            return;

        var handStr = PlayerCards.Select(c => c.DisplayValue).ToList();
        if (MustFollowLedSuit && string.IsNullOrEmpty(LastPlayedCard?.DisplayValue))
        {
            await SyncGameStateFromServerAsync();
            _moveGate.Release();
            return;
        }

        if (MustFollowLedSuit &&
            !MakopaFollowSuit.IsLegalPlay(card.DisplayValue, LastPlayedCard?.DisplayValue, handStr))
        {
            SetError(_i18n?.T("invalid_move_follow_suit") ?? "You must follow the led suit when you can.");
            _moveGate.Release();
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
                await HandleMoveFailureAsync(res);
                return;
            }

            if (res.Data != null)
                await ApplyAuthoritativeStateAsync(res.Data);
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
            await SyncGameStateFromServerAsync();
        }
        finally
        {
            SetLoading(false);
            _moveGate.Release();
        }
    }

    private async Task HandleMoveFailureAsync<T>(Response<T> res)
    {
        await SyncGameStateFromServerAsync();

        if (res.ErrorCode == GameMoveClientCodes.NotYourTurn && !IsPlayerTurn)
        {
            ClearError();
            return;
        }

        if (res.ErrorCode == GameMoveClientCodes.MustFollowSuit)
        {
            if (MustFollowLedSuit && !string.IsNullOrEmpty(LastPlayedCard?.DisplayValue))
                SetError(_i18n?.T("invalid_move_follow_suit") ?? res.ErrorMessage ?? "Invalid move.");
            else
                ClearError();
            return;
        }

        if (res.ErrorCode == GameMoveClientCodes.MustTake)
        {
            SetError(_i18n?.T("invalid_move_must_take") ?? res.ErrorMessage ?? "Invalid move.");
            return;
        }

        if (res.ErrorCode == GameMoveClientCodes.NotYourTurn)
        {
            SetError(_i18n?.T("invalid_move_not_your_turn") ?? res.ErrorMessage ?? "Invalid move.");
            return;
        }

        if (_i18n != null)
        {
            var kopoMsg = GameMoveErrorMessages.TryGetMessage(res.ErrorCode, _i18n.T);
            if (kopoMsg != null)
            {
                SetError(kopoMsg);
                return;
            }
        }

        if (res.StatusCode == 400)
            SetError(_i18n?.T("invalid_move_stale") ?? res.ErrorMessage ?? "Invalid move.");
        else
            SetError(res.ErrorMessage ?? "Failed to apply move.");
    }

    private void StartSessionLivenessPoll()
    {
        StopSessionLivenessPoll();
        _fallbackPollCts = new CancellationTokenSource();
        _ = RunSessionLivenessPollAsync(_fallbackPollCts.Token);
    }

    private async Task RunSessionLivenessPollAsync(CancellationToken token)
    {
        try
        {
            // Safety net when SignalR events are missed (e.g. reconnect gap). Poll faster when hub is down.
            var interval = _hubClient?.IsConnected == true ? TimeSpan.FromSeconds(15) : TimeSpan.FromSeconds(8);
            using var timer = new PeriodicTimer(interval);
            while (!token.IsCancellationRequested && await timer.WaitForNextTickAsync(token))
            {
                if (ShowGameResult || string.IsNullOrEmpty(SessionId))
                    break;
                await SyncGameStateFromServerAsync();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void StopSessionLivenessPoll()
    {
        _fallbackPollCts?.Cancel();
        _fallbackPollCts = null;
    }

    private async void ScheduleAiOpponentIfNeeded(Guid sessionGuid)
    {
        _aiTriggerCts?.Cancel();
        _aiTriggerCts = new CancellationTokenSource();
        if (Variant != GameVariant.Makopa || ShowGameResult || WaitingForOpponent || IsPlayerTurn || _testService == null)
            return;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), _aiTriggerCts.Token);
            if (_aiTriggerCts.Token.IsCancellationRequested)
                return;
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
        {
            _ = SyncGameStateFromServerAsync();
            _ = LoadGameAsync(SessionId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _aiTriggerCts?.Cancel();
        StopInactivityCountdown();
        StopSessionLivenessPoll();
        if (_hubClient != null)
        {
            _hubClient.OnGameStateUpdated -= OnGameStateFromHub;
            _hubClient.OnGameResult -= OnGameResultFromHub;
            _hubClient.OnGameStarted -= OnGameStartedReload;
            _hubClient.OnReconnected -= OnHubReconnected;
            _hubClient.OnInactivityWarning -= OnInactivityWarningFromHub;
            _hubClient.OnInactivityWarningDismissed -= OnInactivityDismissedFromHub;
            _hubClient.OnGameEndedByInactivity -= OnGameEndedByInactivityFromHub;
            await _hubClient.DisconnectAsync();
        }
    }
}
