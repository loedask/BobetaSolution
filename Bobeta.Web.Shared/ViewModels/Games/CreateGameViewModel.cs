using Microsoft.AspNetCore.Components;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Models.Influencer;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;

namespace Bobeta.Web.Shared.ViewModels.Games;

public class CreateGameViewModel(
    IGameService gameService,
    AppStateService appState,
    NavigationManager nav,
    InfluencerService influencerService) : ViewModelBase
{
    private readonly IGameService _gameService = gameService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;
    private readonly InfluencerService _influencerService = influencerService;

    /// <summary>Platform limits (must match API validation).</summary>
    public const decimal MinBet = 200;
    public const decimal MaxBet = 500;

    public decimal SelectedBet { get; private set; } = 300;
    public GameVariant SelectedVariant { get; private set; } = GameVariant.Makopa;
    public InfluencerCodeStatusViewModel? InviteStatus { get; private set; }

    public bool CanSubmit => SelectedBet is >= MinBet and <= MaxBet && !IsLoading;

    public decimal EffectiveCharge =>
        InviteStatus is { HasPendingCode: true, DiscountPercent: > 0 }
            ? Math.Round(SelectedBet * (100m - InviteStatus.DiscountPercent) / 100m, 2, MidpointRounding.AwayFromZero)
            : SelectedBet;

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

    public void SetVariant(GameVariant variant)
    {
        SelectedVariant = variant;
        RaiseStateChanged();
    }

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
            var res = await _gameService.CreateGameAsync(new CreateGameRequest { BetAmount = SelectedBet, Variant = SelectedVariant });
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
