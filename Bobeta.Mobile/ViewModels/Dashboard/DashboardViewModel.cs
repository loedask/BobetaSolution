using Bobeta.Client.Models.Influencer;
using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Dashboard;

public record TransactionItemDto(string Description, string Time, decimal Amount);

public class DashboardViewModel(
    WalletService walletService,
    AppStateService appState,
    I18nService i18n,
    INavigationService nav,
    InfluencerService influencerService) : ViewModelBase
{
    private readonly WalletService _walletService = walletService;
    private readonly AppStateService _appState = appState;
    private readonly I18nService _i18n = i18n;
    private readonly INavigationService _nav = nav;
    private readonly InfluencerService _influencerService = influencerService;

    public string PlayerName => _appState.State.CurrentPlayerName ?? "Player";
    public decimal Balance => _appState.State.WalletBalance;
    public List<TransactionItemDto> Transactions { get; private set; } = new();

    public InfluencerCodeStatusViewModel? InviteStatus { get; private set; }
    public string InviteCodeInput { get; set; } = "";
    public string? InviteSuccessMessage { get; private set; }

    public bool ShowInvitePrompt =>
        !_appState.State.InvitePromptDismissed
        && InviteStatus is not { HasPendingCode: true };

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var balanceTask = _walletService.GetBalanceAsync();
            var txTask = _walletService.GetTransactionsAsync(0, 10);
            var inviteTask = _influencerService.GetStatusAsync();
            await Task.WhenAll(balanceTask, txTask, inviteTask);

            var balanceRes = balanceTask.Result;
            if (balanceRes.IsSuccess && balanceRes.Data != null)
            {
                _appState.SetWalletBalance(balanceRes.Data.Balance, balanceRes.Data.LockedBalance);
                await _appState.PersistAsync();
            }
            else if (!balanceRes.IsSuccess)
                SetError(balanceRes.ErrorMessage ?? "Failed to load balance.");

            var txRes = txTask.Result;
            if (txRes.IsSuccess && txRes.Data != null)
                Transactions = txRes.Data.Select(t => new TransactionItemDto(
                    TransactionDescription(t.Type),
                    t.CreatedAt.ToString("g"),
                    t.Amount)).ToList();

            var inviteRes = inviteTask.Result;
            InviteStatus = inviteRes.IsSuccess ? inviteRes.Data : null;
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
            RaiseStateChanged();
        }
    }

    public async Task ApplyInviteCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(InviteCodeInput) || IsLoading) return;
        SetLoading(true);
        ClearError();
        InviteSuccessMessage = null;
        try
        {
            var res = await _influencerService.ApplyCodeAsync(InviteCodeInput);
            if (res.IsSuccess && res.Data != null)
            {
                InviteStatus = res.Data;
                InviteCodeInput = "";
                InviteSuccessMessage = "Invite code applied for your next game.";
                _appState.SetPendingInviteCode(null);
                _appState.SetInvitePromptDismissed(true);
                await _appState.PersistAsync();
            }
            else
                SetError(res.ErrorMessage ?? "Could not apply invite code.");
        }
        catch
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            SetLoading(false);
            RaiseStateChanged();
        }
    }

    public async Task DismissInvitePromptAsync()
    {
        _appState.SetInvitePromptDismissed(true);
        await _appState.PersistAsync();
        RaiseStateChanged();
    }

    public Task GoToDepositAsync() => _nav.ToDepositAsync();
    public Task GoToWithdrawAsync() => _nav.ToWithdrawAsync();

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
