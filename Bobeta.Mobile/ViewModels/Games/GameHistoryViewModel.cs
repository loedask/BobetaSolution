using Bobeta.Client.Services;
using Bobeta.Client.Services.Base;
using Bobeta.Mobile.Services;
using GameStatusDto = Bobeta.Client.Services.Base.GameStatus;

namespace Bobeta.Mobile.ViewModels.Games;

public class GameHistoryViewModel(HistoryService historyService, I18nService i18n) : ViewModelBase
{
    private readonly HistoryService _historyService = historyService;
    private readonly I18nService _i18n = i18n;

    public List<GameHistoryRow> Rows { get; private set; } = new();

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
        switch (item.Status)
        {
            case GameStatusDto._2:
            {
                var won = (item.WonAmount ?? 0) > 0;
                var title = $"{(won ? _i18n.T("won") : _i18n.T("lost"))} — {item.BetAmount:N0} FCFA";
                var amt = (decimal)(item.WonAmount ?? 0);
                var amountText = $"{(won ? "+" : "")}{amt:N0}";
                var color = won ? Color.FromArgb("#2dd48e") : Color.FromArgb("#e85d5d");
                return new GameHistoryRow { Title = title, Time = item.CreatedAt.ToString("g"), AmountText = amountText, AmountColor = color };
            }
            case GameStatusDto._0:
                return new GameHistoryRow
                {
                    Title = $"{_i18n.T("history_waiting")} — {item.BetAmount:N0} FCFA",
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = "—",
                    AmountColor = muted
                };
            case GameStatusDto._1:
                return new GameHistoryRow
                {
                    Title = $"{_i18n.T("history_in_progress")} — {item.BetAmount:N0} FCFA",
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = "—",
                    AmountColor = muted
                };
            case GameStatusDto._3:
                return new GameHistoryRow
                {
                    Title = $"{_i18n.T("history_cancelled")} — {item.BetAmount:N0} FCFA",
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = "—",
                    AmountColor = muted
                };
            default:
                return new GameHistoryRow
                {
                    Title = $"{item.BetAmount:N0} FCFA",
                    Time = item.CreatedAt.ToString("g"),
                    AmountText = "—",
                    AmountColor = muted
                };
        }
    }
}
