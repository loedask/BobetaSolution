using Bobeta.Client.Models.Influencer;
using Bobeta.Client.Services;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.ViewModels.Profile;

public class ProfileViewModel(
    AppStateService appState,
    INavigationService nav,
    InfluencerService influencerService,
    I18nService i18n) : ViewModelBase
{
    private readonly AppStateService _appState = appState;
    private readonly INavigationService _nav = nav;
    private readonly InfluencerService _influencerService = influencerService;
    private readonly I18nService _i18n = i18n;

    public string PlayerName => _appState.State.CurrentPlayerName ?? "Player";
    public string? PhoneNumber => _appState.State.PhoneNumber ?? "—";
    public bool ShowSignOutModal { get; set; }
    public string InviteCodeInput { get; set; } = "";
    public InfluencerCodeStatusViewModel? InviteStatus { get; private set; }
    public string? InviteSuccessMessage { get; private set; }

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

    public void ShowSignOut()
    {
        ShowSignOutModal = true;
        RaiseStateChanged();
    }

    public void HideSignOut()
    {
        ShowSignOutModal = false;
        RaiseStateChanged();
    }

    public async Task SignOutAsync()
    {
        _appState.ClearSession();
        await _appState.PersistAsync();
        ShowSignOutModal = false;
        await _nav.ToWelcomeAsync();
        RaiseStateChanged();
    }
}
