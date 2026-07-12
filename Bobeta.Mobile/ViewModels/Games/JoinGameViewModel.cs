using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Games;

public class JoinGameViewModel(IGameService gameService, AppStateService appState, INavigationService nav) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;

    private bool _joinBusy;

    public GameVariant? VariantFilter { get; private set; }

    public List<GameSessionViewModel> OpenGames { get; private set; } = new();

    public void SetVariantFilter(GameVariant? variant)
    {
        VariantFilter = variant;
        RaiseStateChanged();
        _ = LoadGamesAsync();
    }

    public async Task LoadGamesAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var res = await _gameService.GetOpenGamesAsync(VariantFilter);
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
                await _nav.ToGamePlayAsync(res.Data.Id);
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
