using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Influencer;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.Services.Realtime;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.ViewModels.Profile;

public class ProfileViewModel(
    AppStateService appState,
    NavigationManager nav,
    GameHubClient hub,
    HistoryService historyService,
    InfluencerService influencerService) : ViewModelBase
{
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;
    private readonly GameHubClient _hub = hub;
    private readonly HistoryService _historyService = historyService;
    private readonly InfluencerService _influencerService = influencerService;

    public string PlayerName => _appState.State.CurrentPlayerName ?? "Player";
    public string? PhoneNumber => _appState.State.PhoneNumber ?? "—";
    public bool ShowSignOutModal { get; set; }
    public int TotalGames { get; private set; }
    public int Wins { get; private set; }
    public string InviteCodeInput { get; set; } = "";
    public InfluencerCodeStatusViewModel? InviteStatus { get; private set; }
    public string? InviteSuccessMessage { get; private set; }

    public string WinRateDisplay => TotalGames == 0 ? "—" : $"{Math.Round(100.0 * Wins / TotalGames)}%";

    public string Initials
    {
        get
        {
            var parts = PlayerName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            return parts.Length == 1 && parts[0].Length > 0 ? parts[0][..1].ToUpperInvariant() : "P";
        }
    }

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();
        InviteSuccessMessage = null;
        try
        {
            var historyTask = _historyService.GetGameHistoryAsync(0, 100);
            var inviteTask = _influencerService.GetStatusAsync();
            await Task.WhenAll(historyTask, inviteTask);

            var res = historyTask.Result;
            if (res.IsSuccess && res.Data != null)
            {
                var finished = res.Data.Where(i => i.Status == GameStatus.Finished).ToList();
                TotalGames = finished.Count;
                Wins = finished.Count(i => (i.WonAmount ?? 0) > 0);
            }
            else if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
            }

            var inviteRes = inviteTask.Result;
            InviteStatus = inviteRes.IsSuccess ? inviteRes.Data : null;
        }
        catch (Exception)
        {
            /* stats are optional on profile */
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
                await _appState.PersistAsync();
            }
            else
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Could not apply invite code.");
            }
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

    public void ShowSignOut() { ShowSignOutModal = true; RaiseStateChanged(); }
    public void HideSignOut() { ShowSignOutModal = false; RaiseStateChanged(); }

    public async Task SignOutAsync()
    {
        try
        {
            await _hub.DisconnectAsync();
        }
        catch
        {
            /* best-effort: still clear local session */
        }

        _appState.ClearSession();
        await _appState.PersistAsync();
        ShowSignOutModal = false;
        _nav.NavigateTo("/", replace: true);
        RaiseStateChanged();
    }
}
