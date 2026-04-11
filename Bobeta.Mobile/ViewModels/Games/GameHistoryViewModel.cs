using Bobeta.Client.Services;
using Bobeta.Client.Services.Base;
using Bobeta.Mobile.Services;

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
                Rows = res.Data.Select(item =>
                {
                    var won = (item.WonAmount ?? 0) > 0;
                    var title = $"{(won ? _i18n.T("won") : _i18n.T("lost"))} — {item.BetAmount:N0} FCFA";
                    var amt = (decimal)(item.WonAmount ?? 0);
                    var amountText = $"{(won ? "+" : "")}{amt:N0}";
                    var color = won ? Color.FromArgb("#2dd48e") : Color.FromArgb("#e85d5d");
                    return new GameHistoryRow
                    {
                        Title = title,
                        Time = item.CreatedAt.ToString("g"),
                        AmountText = amountText,
                        AmountColor = color
                    };
                }).ToList();
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
}
