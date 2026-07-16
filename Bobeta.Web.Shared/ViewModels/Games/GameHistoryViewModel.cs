using Bobeta.Client.Models.Api;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.ViewModels.Games;

public class GameHistoryViewModel(HistoryService historyService, AppStateService appState, NavigationManager nav) : ViewModelBase
{
    private const int PageSize = 50;

    private readonly HistoryService _historyService = historyService;
    private readonly AppStateService _appState = appState;
    private readonly NavigationManager _nav = nav;

    public List<GameHistoryItemDto> Items { get; private set; } = new();

    public async Task LoadAsync()
    {
        SetLoading(true);
        ClearError();
        try
        {
            var all = new List<GameHistoryItemDto>();
            var skip = 0;
            while (true)
            {
                var res = await _historyService.GetGameHistoryAsync(skip, PageSize);
                if (!res.IsSuccess)
                {
                    if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                        return;
                    if (all.Count == 0)
                        SetError(res.ErrorMessage ?? "Failed to load history.");
                    break;
                }

                if (res.Data == null || res.Data.Count == 0)
                    break;

                all.AddRange(res.Data);
                if (res.Data.Count < PageSize)
                    break;
                skip += PageSize;
            }

            Items = all;
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
