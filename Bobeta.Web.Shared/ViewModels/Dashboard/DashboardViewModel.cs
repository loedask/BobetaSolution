using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.ViewModels.Dashboard;

public record TransactionItemDto(string Description, string Time, decimal Amount);

public class DashboardViewModel(WalletService walletService, AppStateService appState, I18nService i18n, NavigationManager nav) : ViewModelBase
{
    private readonly WalletService _walletService = walletService;
    private readonly AppStateService _appState = appState;
    private readonly I18nService _i18n = i18n;
    private readonly NavigationManager _nav = nav;

    public string PlayerName => _appState.State.CurrentPlayerName ?? "Player";
    public decimal Balance => _appState.State.WalletBalance;
    public List<TransactionItemDto> Transactions { get; private set; } = new();

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();

        var balanceRes = await _walletService.GetBalanceAsync();
        if (balanceRes.IsSuccess && balanceRes.Data != null)
        {
            _appState.SetWalletBalance(balanceRes.Data.Balance, balanceRes.Data.LockedBalance);
            await _appState.PersistAsync();
        }
        else if (!balanceRes.IsSuccess)
        {
            if (await _appState.HandleUnauthorizedAsync(balanceRes.StatusCode, _nav))
                return;
            SetError(balanceRes.ErrorMessage ?? "Failed to load balance.");
        }

        var txRes = await _walletService.GetTransactionsAsync(0, 10);
        if (!txRes.IsSuccess && await _appState.HandleUnauthorizedAsync(txRes.StatusCode, _nav))
            return;
        if (txRes.IsSuccess && txRes.Data != null)
        {
            Transactions = txRes.Data.Select(t => new TransactionItemDto(
                TransactionDescription(t.Type),
                t.CreatedAt.ToString("g"),
                t.Amount)).ToList();
        }
        else if (!txRes.IsSuccess && string.IsNullOrEmpty(ErrorMessage))
        {
            SetError(txRes.ErrorMessage ?? "Failed to load recent activity.");
        }

        SetLoading(false);
    }

    public void GoToDeposit() => _nav.NavigateTo("/deposit");
    public void GoToWithdraw() => _nav.NavigateTo("/withdraw");

    private string TransactionDescription(string type) => type switch
    {
        "Deposit" => _i18n.T("deposit_label"),
        "Withdrawal" or "Withdraw" => _i18n.T("withdraw"),
        "BetLock" => _i18n.T("tx_bet_lock"),
        "BetRelease" => _i18n.T("tx_bet_release"),
        "Win" => _i18n.T("tx_winnings"),
        "Commission" => _i18n.T("tx_commission"),
        _ => type
    };
}
