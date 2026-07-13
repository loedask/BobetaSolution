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
    InfluencerService influencerService) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;
    private readonly InfluencerService _influencerService = influencerService;

    private bool _joinBusy;
    private readonly object _loadSync = new();
    private Task? _loadInFlight;
    private DateTimeOffset _lastSuccessfulLoadUtc = DateTimeOffset.MinValue;
    private static readonly TimeSpan DuplicateSuppressionWindow = TimeSpan.FromSeconds(2);

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
                InviteSuccessMessage = "Invite code applied for your next game.";
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

    public Task LoadGamesAsync(bool forceRefresh = false)
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

            _loadInFlight = LoadGamesCoreAsync();
            return _loadInFlight;
        }
    }

    private async Task LoadGamesCoreAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var res = await _gameService.GetOpenGamesAsync(VariantFilter);
            if (res.IsSuccess && res.Data != null)
            {
                OpenGames = res.Data.Where(x => x.OpponentPlayerId == null).ToList();
                _lastSuccessfulLoadUtc = DateTimeOffset.UtcNow;
            }
            else if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Failed to load games.");
            }
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            lock (_loadSync)
                _loadInFlight = null;
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
