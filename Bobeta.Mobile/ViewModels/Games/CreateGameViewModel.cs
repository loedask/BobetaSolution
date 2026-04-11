using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Games;

public class CreateGameViewModel(IGameService gameService, AppStateService appState, INavigationService nav) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;

    private string _betAmount = "1000";

    public string BetAmount
    {
        get => _betAmount;
        set
        {
            if (_betAmount == value) return;
            _betAmount = value;
            RaiseStateChanged();
        }
    }

    public bool CanSubmit =>
        decimal.TryParse(BetAmount, out var v) && v >= 100 && !IsLoading;

    public void SetPresetAmount(int value)
    {
        BetAmount = value.ToString();
        RaiseStateChanged();
    }

    public async Task CreateAsync()
    {
        if (!CanSubmit) return;
        SetLoading(true);
        ClearError();
        try
        {
            var amount = decimal.Parse(BetAmount);
            var res = await _gameService.CreateGameAsync(new CreateGameRequest { BetAmount = amount });
            if (res.IsSuccess && res.Data != null)
            {
                _appState.SetActiveGameSession(res.Data.Id);
                await _appState.PersistAsync();
                await _nav.ToGamePlayAsync(res.Data.Id);
            }
            else
                SetError(res.ErrorMessage ?? "Failed to create game.");
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
