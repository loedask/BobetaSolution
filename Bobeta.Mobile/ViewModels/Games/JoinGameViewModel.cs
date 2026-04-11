using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Games;

public class JoinGameViewModel(IGameService gameService, AppStateService appState, INavigationService nav) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;

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
                SetError(res.ErrorMessage ?? "Failed to load games.");
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
        if (IsLoading) return;
        SetLoading(true);
        try
        {
            var res = await _gameService.JoinGameAsync(new JoinGameRequest { GameId = gameId });
            if (res.IsSuccess && res.Data != null)
            {
                _appState.SetActiveGameSession(res.Data.Id);
                await _appState.PersistAsync();
                await _nav.ToMainTabsAsync("Dashboard");
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
            SetLoading(false);
        }
    }
}
