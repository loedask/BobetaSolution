using Microsoft.AspNetCore.Components;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Models.Influencer;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;

namespace Bobeta.Web.Shared.ViewModels.Games;

public class JoinGameViewModel(
    IGameService gameService,
    AppStateService appState,
    NavigationManager nav,
    InfluencerService influencerService,
    I18nService i18n) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;
    private readonly InfluencerService _influencerService = influencerService;
    private readonly I18nService _i18n = i18n;

    private bool _joinBusy;
    private readonly object _loadSync = new();
    private Task? _loadInFlight;
    private DateTimeOffset _lastSuccessfulLoadUtc = DateTimeOffset.MinValue;
    private static readonly TimeSpan DuplicateSuppressionWindow = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LiveRefreshInterval = TimeSpan.FromSeconds(12);
    private CancellationTokenSource? _liveRefreshCts;

    /// <summary>null = show all game types.</summary>
    public GameVariant? VariantFilter { get; private set; }

    public List<GameSessionViewModel> OpenGames { get; private set; } = new();
    public InfluencerCodeStatusViewModel? InviteStatus { get; private set; }
    public string InviteCodeInput { get; set; } = "";
    public string? InviteSuccessMessage { get; private set; }

    public void SetVariantFilter(GameVariant? variant)
    {
        VariantFilter = variant;
        RaiseStateChanged();
        _ = LoadGamesAsync(forceRefresh: true);
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
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Could not apply invite code.");
            }
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
                await LoadGamesAsync(forceRefresh: true, quiet: true);
        }
        catch (OperationCanceledException)
        {
            // Page left / refresh stopped.
        }
    }

    public Task LoadGamesAsync(bool forceRefresh = false, bool quiet = false)
    {
        lock (_loadSync)
        {
            if (_loadInFlight != null)
                return _loadInFlight;

            if (!forceRefresh
                && _lastSuccessfulLoadUtc != DateTimeOffset.MinValue
                && DateTimeOffset.UtcNow - _lastSuccessfulLoadUtc < DuplicateSuppressionWindow)
            {
                return Task.CompletedTask;
            }

            _loadInFlight = LoadGamesCoreAsync(quiet);
            return _loadInFlight;
        }
    }

    private async Task LoadGamesCoreAsync(bool quiet)
    {
        if (!quiet)
        {
            SetLoading(true);
            ClearError();
        }
        try
        {
            var res = await _gameService.GetOpenGamesAsync(VariantFilter);
            if (res.IsSuccess && res.Data != null)
            {
                OpenGames = res.Data.Where(x => x.OpponentPlayerId == null).ToList();
                _lastSuccessfulLoadUtc = DateTimeOffset.UtcNow;
                if (quiet)
                    RaiseStateChanged();
            }
            else if (!res.IsSuccess && !quiet)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Failed to load games.");
            }
            else if (!res.IsSuccess && quiet)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
            }
        }
        catch (Exception)
        {
            if (!quiet)
                SetError("Something went wrong. Please try again.");
        }
        finally
        {
            lock (_loadSync)
                _loadInFlight = null;
            if (!quiet)
                SetLoading(false);
        }
    }

    public async Task JoinGameAsync(Guid gameId)
    {
        if (_joinBusy) return;
        _joinBusy = true;
        SetLoading(true);
        try
        {
            var res = await _gameService.JoinGameAsync(new JoinGameRequest { GameId = gameId });
            if (res.IsSuccess && res.Data != null)
            {
                _appState.SetActiveGameSession(res.Data.Id);
                await _appState.PersistAsync();
                _nav.NavigateTo($"/game/{res.Data.Id}");
            }
            else
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Failed to join game.");
            }
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
