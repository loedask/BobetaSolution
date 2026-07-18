using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Games;

public class MyWaitingTablesViewModel(
    IGameService gameService,
    AppStateService appState,
    INavigationService nav,
    WalletService walletService) : ViewModelBase
{
    private Guid? _busyGameId;

    public List<GameSessionViewModel> WaitingTables { get; private set; } = new();
    public Guid? BusyGameId => _busyGameId;

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var res = await gameService.GetMyWaitingGamesAsync();
            if (!res.IsSuccess)
            {
                SetError(res.ErrorMessage ?? "Failed to load waiting tables.");
                WaitingTables = new();
            }
            else
                WaitingTables = res.Data?.ToList() ?? new();
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

    public Task OpenAsync(Guid gameId) => nav.ToGamePlayAsync(gameId);

    public async Task CancelAsync(Guid gameId)
    {
        if (_busyGameId is not null) return;
        _busyGameId = gameId;
        ClearError();
        RaiseStateChanged();
        try
        {
            var res = await gameService.CancelWaitingGameAsync(gameId);
            if (!res.IsSuccess)
            {
                SetError(res.ErrorMessage ?? "Could not cancel this table.");
                return;
            }

            WaitingTables.RemoveAll(g => g.Id == gameId);
            var balanceRes = await walletService.GetBalanceAsync();
            if (balanceRes.IsSuccess && balanceRes.Data != null)
            {
                appState.SetWalletBalance(balanceRes.Data.Balance, balanceRes.Data.LockedBalance);
                await appState.PersistAsync();
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
