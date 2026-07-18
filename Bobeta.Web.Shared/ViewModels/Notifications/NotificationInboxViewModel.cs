using Bobeta.Client.Models.Notifications;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.Services.Realtime;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.ViewModels.Notifications;

public class NotificationInboxViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly NotificationApiService _api;
    private readonly NotificationHubClient _hub;
    private readonly I18nService _i18n;
    private readonly NavigationManager _nav;
    private readonly AppStateService _appState;
    private bool _hubSubscribed;

    public NotificationInboxViewModel(
        NotificationApiService api,
        NotificationHubClient hub,
        I18nService i18n,
        NavigationManager nav,
        AppStateService appState)
    {
        _api = api;
        _hub = hub;
        _i18n = i18n;
        _nav = nav;
        _appState = appState;
    }

    public bool IsOpen { get; private set; }
    public int UnreadCount { get; private set; }
    public List<NotificationViewModel> Items { get; private set; } = new();

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_appState.State.AccessToken))
            return;

        if (!_hubSubscribed)
        {
            _hub.OnNotificationReceived += OnPushed;
            _hubSubscribed = true;
        }

        // Hub connect is best-effort; unread count still loads over REST if realtime fails.
        await _hub.ConnectAsync();
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var countRes = await _api.GetUnreadCountAsync();
        if (countRes.IsSuccess)
            UnreadCount = countRes.Data;

        if (IsOpen)
            await LoadItemsAsync();

        RaiseStateChanged();
    }

    public async Task OpenAsync()
    {
        IsOpen = true;
        ClearError();
        await LoadItemsAsync();
        RaiseStateChanged();
    }

    public void Close()
    {
        IsOpen = false;
        RaiseStateChanged();
    }

    public async Task MarkAllReadAsync()
    {
        var res = await _api.MarkAllReadAsync();
        if (!res.IsSuccess)
        {
            SetError(res.ErrorMessage ?? "Could not mark notifications as read.");
            RaiseStateChanged();
            return;
        }

        foreach (var item in Items)
            item.IsRead = true;
        UnreadCount = 0;
        RaiseStateChanged();
    }

    public async Task OpenItemAsync(NotificationViewModel item)
    {
        if (!item.IsRead)
        {
            await _api.MarkReadAsync(item.Id);
            item.IsRead = true;
            UnreadCount = Math.Max(0, UnreadCount - 1);
        }

        IsOpen = false;
        RaiseStateChanged();

        if (!string.IsNullOrWhiteSpace(item.DeepLink))
            _nav.NavigateTo(item.DeepLink);
    }

    public string TitleFor(NotificationViewModel item) => item.Type switch
    {
        "OpponentJoined" => _i18n.T("notif_opponent_joined_title"),
        "GameWon" => _i18n.T("notif_game_won_title"),
        "GameLost" => _i18n.T("notif_game_lost_title"),
        "DepositSuccess" => _i18n.T("notif_deposit_ok_title"),
        "DepositFailed" => _i18n.T("notif_deposit_fail_title"),
        "WithdrawSuccess" => _i18n.T("notif_withdraw_ok_title"),
        "WithdrawFailed" => _i18n.T("notif_withdraw_fail_title"),
        "GameInvite" => _i18n.T("notif_game_invite_title"),
        "BetProposal" => _i18n.T("notif_bet_proposal_title"),
        _ => _i18n.T("notifications")
    };

    public string BodyFor(NotificationViewModel item)
    {
        var amount = item.Amount?.ToString("N0") ?? "0";
        var actor = item.ActorName ?? _i18n.T("notif_opponent_fallback");
        return item.Type switch
        {
            "OpponentJoined" => string.Format(_i18n.T("notif_opponent_joined_body"), actor, amount),
            "GameWon" => string.Format(_i18n.T("notif_game_won_body"), amount),
            "GameLost" => string.Format(_i18n.T("notif_game_lost_body"), amount),
            "DepositSuccess" => string.Format(_i18n.T("notif_deposit_ok_body"), amount),
            "DepositFailed" => string.Format(_i18n.T("notif_deposit_fail_body"), amount),
            "WithdrawSuccess" => string.Format(_i18n.T("notif_withdraw_ok_body"), amount),
            "WithdrawFailed" => string.Format(_i18n.T("notif_withdraw_fail_body"), amount),
            "GameInvite" => string.Format(_i18n.T("notif_game_invite_body"), actor, amount),
            "BetProposal" => string.Format(_i18n.T("notif_bet_proposal_body"), amount),
            _ => ""
        };
    }

    private async Task LoadItemsAsync()
    {
        SetLoading(true);
        var res = await _api.GetInboxAsync();
        SetLoading(false);
        if (!res.IsSuccess || res.Data is null)
        {
            if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                return;
            SetError(res.ErrorMessage ?? "Failed to load notifications.");
            RaiseStateChanged();
            return;
        }

        Items = res.Data.ToList();
        UnreadCount = Items.Count(i => !i.IsRead);
        RaiseStateChanged();
    }

    private void OnPushed(NotificationViewModel item)
    {
        Items.Insert(0, item);
        if (!item.IsRead)
            UnreadCount++;
        RaiseStateChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubSubscribed)
        {
            _hub.OnNotificationReceived -= OnPushed;
            _hubSubscribed = false;
        }

        await _hub.DisconnectAsync();
    }
}
