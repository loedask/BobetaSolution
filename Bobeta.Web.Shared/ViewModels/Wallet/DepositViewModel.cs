using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.ViewModels.Wallet;

public class DepositViewModel(WalletService walletService, AppStateService appState, NavigationManager nav) : ViewModelBase
{
    private readonly WalletService _walletService = walletService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;

    public string Amount { get; set; } = "";
    public decimal CurrentBalance => _appState.State.WalletBalance;
    public string Status { get; set; } = "idle";
    public decimal SuccessAmount { get; set; }

    public bool IsSuccess => Status == "success";
    public bool IsProcessing => Status == "processing";
    public bool CanSubmit => !string.IsNullOrEmpty(Amount) && decimal.TryParse(Amount, out var v) && v >= 100 && !IsProcessing;

    public void SetPresetAmount(int value) { Amount = value.ToString(); RaiseStateChanged(); }

    public async Task SubmitAsync()
    {
        if (!CanSubmit) return;
        Status = "processing";
        ClearError();
        RaiseStateChanged();
        try
        {
            if (!decimal.TryParse(Amount, out var value) || value < 100) return;
            var res = await _walletService.DepositAsync((double)value);
            if (res.IsSuccess)
            {
                var bal = await _walletService.GetBalanceAsync();
                if (await _appState.HandleUnauthorizedAsync(bal.StatusCode, _nav))
                    return;
                if (bal.IsSuccess && bal.Data != null)
                    _appState.SetWalletBalance(bal.Data.Balance, bal.Data.LockedBalance);
                else
                    _appState.SetWalletBalance(_appState.State.WalletBalance + value, _appState.State.LockedBalance);
                await _appState.PersistAsync();
                SuccessAmount = value;
                Status = "success";
                RaiseStateChanged();
                await Task.Delay(1500);
                _nav.NavigateTo("/dashboard");
            }
            else
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Deposit failed.");
            }
        }
        catch (Exception)
        {
            SetError("Something went wrong. Please try again.");
        }
        finally
        {
            Status = "idle";
            RaiseStateChanged();
        }
    }
}
