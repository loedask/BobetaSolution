using Bobeta.Client.Services;
using Bobeta.Client.Services.Base;

namespace Bobeta.Web.ViewModels.Games;

public class GameHistoryViewModel(HistoryService historyService) : ViewModelBase
{
    private readonly HistoryService _historyService = historyService;

    public List<GameHistoryItemDto> Items { get; private set; } = new();

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var res = await _historyService.GetGameHistoryAsync(0, 50);
            if (res.IsSuccess && res.Data != null)
                Items = res.Data.ToList();
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
