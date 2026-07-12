using Bobeta.Client.Models.Api;
using Bobeta.Client.Services;
using Bobeta.Mobile.Services;
using Microsoft.Maui.Controls;

namespace Bobeta.Mobile.ViewModels.Games;

public class GameHistoryViewModel : ViewModelBase
{
    private readonly HistoryService _historyService;
    private readonly I18nService _i18n;
    private readonly INavigationService _navigation;

    public GameHistoryViewModel(HistoryService historyService, I18nService i18n, INavigationService navigation)
    {
        _historyService = historyService;
        _i18n = i18n;
        _navigation = navigation;
        ContinueGameCommand = new Command<Guid>(id => _ = _navigation.ToGamePlayAsync(id));
    }

    public List<GameHistoryRow> Rows { get; private set; } = new();

    public Command<Guid> ContinueGameCommand { get; }

    public string ContinueButtonText => _i18n.T("history_continue");

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var res = await _historyService.GetGameHistoryAsync(0, 50);
            if (res.IsSuccess && res.Data != null)
            {
                Rows = res.Data.Select(MapRow).ToList();
            }
            else if (!res.IsSuccess)
                SetError(res.ErrorMessage ?? "Failed to load history.");
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

    private GameHistoryRow MapRow(GameHistoryItemDto item)
    {
        var muted = Color.FromArgb("#8a93a8");
        var sid = item.GameSessionId;
        var role = item.IsCreator ? _i18n.T("history_you_hosted") : _i18n.T("history_you_joined");
        var meta = $"{item.VariantName} · {role}";

        switch (item.Status)
        {
            case GameStatus.Finished:
            {
                var won = (item.WonAmount ?? 0) > 0;
                var title = $"{(won ? _i18n.T("won") : _i18n.T("lost"))} — {item.BetAmount:N0} FCFA";
                var amt = (decimal)(item.WonAmount ?? 0);
                var amountText = $"{(won ? "+" : "")}{amt:N0}";
                var color = won ? Color.FromArgb("#2dd48e") : Color.FromArgb("#e85d5d");
                return new GameHistoryRow
                {
                    SessionId = sid,
                    Title = title,
                    Subtitle = meta,
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = amountText,
                    AmountColor = color
                };
            }
            case GameStatus.Waiting:
                return new GameHistoryRow
                {
                    SessionId = sid,
                    Title = $"{_i18n.T("history_waiting")} — {item.BetAmount:N0} FCFA",
                    Subtitle = meta,
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = "—",
                    AmountColor = muted
                };
            case GameStatus.InProgress:
                return new GameHistoryRow
                {
                    SessionId = sid,
                    Title = $"{_i18n.T("history_live_game")} — {item.BetAmount:N0} FCFA",
                    Subtitle = $"{role}. {_i18n.T("history_live_hint")}",
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = "—",
                    AmountColor = muted,
                    ShowContinue = true
                };
            case GameStatus.Cancelled:
                return new GameHistoryRow
                {
                    SessionId = sid,
                    Title = $"{_i18n.T("history_cancelled")} — {item.BetAmount:N0} FCFA",
                    Subtitle = meta,
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = "—",
                    AmountColor = muted
                };
            default:
                return new GameHistoryRow
                {
                    SessionId = sid,
                    Title = $"{item.BetAmount:N0} FCFA",
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = "—",
                    AmountColor = muted
                };
        }
    }
}
