using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Models.Influencer;
using Bobeta.Client.Presentation;
using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Games;

public class JoinGameViewModel(
    IGameService gameService,
    AppStateService appState,
    INavigationService nav,
    InfluencerService influencerService,
    I18nService i18n) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;
    private readonly InfluencerService _influencerService = influencerService;
    private readonly I18nService _i18n = i18n;

    private bool _joinBusy;
    private static readonly TimeSpan LiveRefreshInterval = TimeSpan.FromSeconds(12);
    private CancellationTokenSource? _liveRefreshCts;

    public GameVariant? VariantFilter { get; private set; }

    public List<GameSessionViewModel> OpenGames { get; private set; } = new();
    public InfluencerCodeStatusViewModel? InviteStatus { get; private set; }
    public string InviteCodeInput { get; set; } = "";
    public string? InviteSuccessMessage { get; private set; }
    public bool ShowLiveGamesAction { get; private set; }

    public void SetVariantFilter(GameVariant? variant)
    {
        VariantFilter = variant;
        RaiseStateChanged();
        _ = LoadGamesAsync();
    }

    public async Task LoadInviteStatusAsync()
    {
        try
        {
            var res = await _influencerService.GetStatusAsync();
            InviteStatus = res.IsSuccess ? res.Data : null;
        }
        catch
        {
            InviteStatus = null;
        }
        RaiseStateChanged();
    }

    public async Task ApplyInviteCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(InviteCodeInput) || IsLoading) return;
        SetLoading(true);
        ClearError();
        ShowLiveGamesAction = false;
        InviteSuccessMessage = null;
        try
        {
            var res = await _influencerService.ApplyCodeAsync(InviteCodeInput);
            if (res.IsSuccess && res.Data != null)
            {
                InviteStatus = res.Data;
                InviteCodeInput = "";
                InviteSuccessMessage = _i18n.T("invite_applied");
                _appState.SetPendingInviteCode(null);
                await _appState.PersistAsync();
            }
            else
                SetError(res.ErrorMessage ?? "Could not apply invite code.");
        }
        catch
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
            RaiseStateChanged();
        }
    }

    public void StartLiveRefresh()
    {
        StopLiveRefresh();
        _liveRefreshCts = new CancellationTokenSource();
        _ = RunLiveRefreshAsync(_liveRefreshCts.Token);
    }

    public void StopLiveRefresh()
    {
        _liveRefreshCts?.Cancel();
        _liveRefreshCts?.Dispose();
        _liveRefreshCts = null;
    }

    private async Task RunLiveRefreshAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(LiveRefreshInterval);
            while (await timer.WaitForNextTickAsync(token))
                await LoadGamesAsync(quiet: true);
        }
        catch (OperationCanceledException)
        {
            // Page left / refresh stopped.
        }
    }

    public async Task LoadGamesAsync(bool quiet = false)
    {
        if (!quiet)
        {
            SetLoading(true);
            ClearError();
            ShowLiveGamesAction = false;
        }
        try
        {
            var variantFilter = VariantFilter;
            var res = await _gameService.GetOpenGamesAsync(variantFilter);
            if (res.IsSuccess && res.Data != null)
            {
                OpenGames = res.Data.Where(x => x.OpponentPlayerId == null).ToList();
                RaiseStateChanged();
            }
            else if (!res.IsSuccess && !quiet)
                SetError(res.ErrorMessage ?? "Failed to load games.");
        }
        catch (Exception)
        {
            if (!quiet)
                SetError("Something went wrong. Please try again.");
        }
        finally
        {
            if (!quiet)
                SetLoading(false);
        }
    }

    public async Task JoinGameAsync(Guid gameId)
    {
        if (_joinBusy) return;
        _joinBusy = true;
        ShowLiveGamesAction = false;
        SetLoading(true);
        try
        {
            var res = await _gameService.JoinGameAsync(new JoinGameRequest { GameId = gameId });
            if (res.IsSuccess && res.Data != null)
            {
                _appState.SetActiveGameSession(res.Data.Id);
                await _appState.PersistAsync();
                await _nav.ToGamePlayAsync(res.Data.Id);
            }
            else if (res.ErrorCode == GameSessionClientCodes.TooManyLiveGames)
            {
                ShowLiveGamesAction = true;
                SetError(string.Format(
                    _i18n.T("join_too_many_live_games"),
                    GameSessionClientCodes.MaxConcurrentInProgressGames));
            }
            else
                SetError(res.ErrorMessage ?? "Failed to join game.");
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            _joinBusy = false;
            SetLoading(false);
        }
    }
}
