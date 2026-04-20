using Microsoft.AspNetCore.Components;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Web.Services;

namespace Bobeta.Web.ViewModels.Games;

public class JoinGameViewModel(IGameService gameService, AppStateService appState, NavigationManager nav) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;

    /// <summary>Prevents double-submit; must not use <see cref="ViewModelBase.IsLoading"/> — that is shared with <see cref="LoadGamesAsync"/> and blocks join while the list is still loading.</summary>
    private bool _joinBusy;

    private readonly object _loadSync = new();
    private Task? _loadInFlight;
    private DateTimeOffset _lastSuccessfulLoadUtc = DateTimeOffset.MinValue;
    private static readonly TimeSpan DuplicateSuppressionWindow = TimeSpan.FromSeconds(2);

    public List<GameSessionViewModel> OpenGames { get; private set; } = new();

    /// <summary>Loads joinable games. Coalesces concurrent calls and suppresses duplicate loads shortly after a success (Blazor can trigger lifecycle twice). Use <paramref name="forceRefresh"/> for the Refresh button.</summary>
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
            var res = await _gameService.GetOpenGamesAsync();
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
