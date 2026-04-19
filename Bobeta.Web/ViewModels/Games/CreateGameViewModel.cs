using Microsoft.AspNetCore.Components;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
using Bobeta.Web.Services;

namespace Bobeta.Web.ViewModels.Games;

public class CreateGameViewModel(IGameService gameService, AppStateService appState, NavigationManager nav) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;

    /// <summary>Platform limits (must match API validation).</summary>
    public const decimal MinBet = 200;
    public const decimal MaxBet = 500;

    public decimal SelectedBet { get; private set; } = 300;

    public bool CanSubmit => SelectedBet is >= MinBet and <= MaxBet && !IsLoading;

    public void SetBet(decimal amount)
    {
        SelectedBet = amount;
        RaiseStateChanged();
    }

    public async Task CreateAsync()
    {
        if (!CanSubmit) return;
        SetLoading(true);
        ClearError();
        try
        {
            var res = await _gameService.CreateGameAsync(new CreateGameRequest { BetAmount = SelectedBet });
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
                SetError(res.ErrorMessage ?? "Failed to create game.");
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
}
