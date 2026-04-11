using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Dashboard;

public record TransactionItemDto(string Description, string Time, decimal Amount);

public class DashboardViewModel(WalletService walletService, AppStateService appState, I18nService i18n, INavigationService nav) : ViewModelBase
{
    private readonly WalletService _walletService = walletService;
    private readonly AppStateService _appState = appState;
    private readonly I18nService _i18n = i18n;
    private readonly INavigationService _nav = nav;

    public string PlayerName => _appState.State.CurrentPlayerName ?? "Player";
    public decimal Balance => _appState.State.WalletBalance;
    public List<TransactionItemDto> Transactions { get; private set; } = new();

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var balanceRes = await _walletService.GetBalanceAsync();
            if (balanceRes.IsSuccess && balanceRes.Data != null)
            {
                _appState.SetWalletBalance(balanceRes.Data.Balance, balanceRes.Data.LockedBalance);
                await _appState.PersistAsync();
            }
            else if (!balanceRes.IsSuccess)
                SetError(balanceRes.ErrorMessage ?? "Failed to load balance.");

            var txRes = await _walletService.GetTransactionsAsync(0, 10);
            if (txRes.IsSuccess && txRes.Data != null)
                Transactions = txRes.Data.Select(t => new TransactionItemDto(
                    t.Type == "Deposit" ? _i18n.T("deposit_label") : t.Type == "Withdraw" ? "Withdraw" : t.Type,
                    t.CreatedAt.ToString("g"),
                    t.Amount)).ToList();
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

    public Task GoToDepositAsync() => _nav.ToDepositAsync();
    public Task GoToWithdrawAsync() => _nav.ToWithdrawAsync();
}
