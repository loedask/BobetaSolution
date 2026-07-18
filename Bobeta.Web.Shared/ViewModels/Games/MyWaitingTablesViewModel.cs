using Bobeta.Client.Models.Games;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.ViewModels.Games;

public class MyWaitingTablesViewModel(
    IGameService gameService,
    AppStateService appState,
    NavigationManager nav,
    WalletService walletService) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;
    private readonly WalletService _walletService = walletService;
    private Guid? _busyGameId;

    public List<GameSessionViewModel> WaitingTables { get; private set; } = new();
    public Guid? BusyGameId => _busyGameId;

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var res = await _gameService.GetMyWaitingGamesAsync();
            if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Failed to load waiting tables.");
                WaitingTables = new();
            }
            else
            {
                WaitingTables = res.Data?.ToList() ?? new();
            }
        }
        catch
        {
            SetError("Something went wrong. Please try again.");
            WaitingTables = new();
        }
        finally
        {
            SetLoading(false);
            RaiseStateChanged();
        }
    }

    public void OpenTable(Guid gameId) => _nav.NavigateTo($"/game/{gameId}");

    public async Task CancelAsync(Guid gameId)
    {
        if (_busyGameId is not null) return;
        _busyGameId = gameId;
        ClearError();
        RaiseStateChanged();
        try
        {
            var res = await _gameService.CancelWaitingGameAsync(gameId);
            if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Could not cancel this table.");
                return;
            }

            WaitingTables.RemoveAll(g => g.Id == gameId);
            var balanceRes = await _walletService.GetBalanceAsync();
            if (balanceRes.IsSuccess && balanceRes.Data != null)
            {
                _appState.SetWalletBalance(balanceRes.Data.Balance, balanceRes.Data.LockedBalance);
                await _appState.PersistAsync();
            }
        }
        catch
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            _busyGameId = null;
            RaiseStateChanged();
        }
    }
}
