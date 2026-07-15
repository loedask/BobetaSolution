using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Models.Influencer;
using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Games;

public class CreateGameViewModel(
    IGameService gameService,
    AppStateService appState,
    INavigationService nav,
    InfluencerService influencerService,
    I18nService i18n) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;
    private readonly InfluencerService _influencerService = influencerService;
    private readonly I18nService _i18n = i18n;

    private string _betAmount = "300";
    private GameVariant _variant = GameVariant.Makopa;

    public GameVariant SelectedVariant => _variant;
    public InfluencerCodeStatusViewModel? InviteStatus { get; private set; }
    public string InviteCodeInput { get; set; } = "";
    public string? InviteSuccessMessage { get; private set; }

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
        decimal.TryParse(BetAmount, out var v) && v >= 200 && v <= 500 && !IsLoading;

    public decimal EffectiveCharge
    {
        get
        {
            if (!decimal.TryParse(BetAmount, out var bet)) return 0;
            if (InviteStatus is { HasPendingCode: true, DiscountPercent: > 0 })
                return Math.Round(bet * (100m - InviteStatus.DiscountPercent) / 100m, 2, MidpointRounding.AwayFromZero);
            return bet;
        }
    }

    public async Task LoadInviteStatusAsync()
    {
        try
        {
            var res = await _influencerService.GetStatusAsync();
            InviteStatus = res.IsSuccess ? res.Data : null;
        }
        catch
        {
            InviteStatus = null;
        }
        RaiseStateChanged();
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
                InviteSuccessMessage = _i18n.T("invite_applied");
                _appState.SetPendingInviteCode(null);
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

    public void SetPresetAmount(int value)
    {
        BetAmount = value.ToString();
        RaiseStateChanged();
    }

    public void SetVariant(GameVariant variant)
    {
        if (_variant == variant) return;
        _variant = variant;
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
            var res = await _gameService.CreateGameAsync(new CreateGameRequest { BetAmount = amount, Variant = _variant });
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
