using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Wallet;

public class WithdrawViewModel(WalletService walletService, AppStateService appState, INavigationService nav) : ViewModelBase
{
    private readonly WalletService _walletService = walletService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;

    private string _amount = "";

    public string Amount
    {
        get => _amount;
        set
        {
            if (_amount == value) return;
            _amount = value;
            RaiseStateChanged();
        }
    }

    private string _momoNumber = "6XXXXXXXX";

    public string MomoNumber
    {
        get => _momoNumber;
        set
        {
            if (_momoNumber == value) return;
            _momoNumber = value;
            RaiseStateChanged();
        }
    }
    public decimal AvailableBalance => _appState.State.WalletBalance;
    public string Status { get; set; } = "idle";
    public decimal SuccessAmount { get; set; }

    public bool IsSuccess => Status == "success";
    public bool IsProcessing => Status == "processing";
    public bool CanSubmit => !string.IsNullOrEmpty(Amount) && decimal.TryParse(Amount, out var v) && v >= 200 && v <= AvailableBalance && !IsProcessing;

    public async Task SubmitAsync()
    {
        if (!CanSubmit) return;
        Status = "processing";
        ClearError();
        RaiseStateChanged();
        try
        {
            if (!decimal.TryParse(Amount, out var value) || value < 200 || value > AvailableBalance) return;
            var res = await _walletService.WithdrawAsync((double)value);
            if (res.IsSuccess)
            {
                _appState.SetWalletBalance(_appState.State.WalletBalance - value, _appState.State.LockedBalance);
                await _appState.PersistAsync();
                SuccessAmount = value;
                Status = "success";
                RaiseStateChanged();
                await Task.Delay(1500);
                await _nav.ToMainTabsAsync("Dashboard");
            }
            else
                SetError(res.ErrorMessage ?? "Withdrawal failed.");
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
