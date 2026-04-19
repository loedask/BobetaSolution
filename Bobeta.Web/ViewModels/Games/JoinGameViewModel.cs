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

    public List<GameSessionViewModel> OpenGames { get; private set; } = new();

    public async Task LoadGamesAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var res = await _gameService.GetOpenGamesAsync();
            if (res.IsSuccess && res.Data != null)
                OpenGames = res.Data.Where(x => x.OpponentPlayerId == null).ToList();
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
