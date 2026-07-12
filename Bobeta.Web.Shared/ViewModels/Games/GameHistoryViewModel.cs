using Bobeta.Client.Models.Api;
using Bobeta.Client.Services;
using Bobeta.Web.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Bobeta.Web.Shared.ViewModels.Games;

public class GameHistoryViewModel(HistoryService historyService, AppStateService appState, NavigationManager nav) : ViewModelBase
{
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
            var res = await _historyService.GetGameHistoryAsync(0, 50);
            if (res.IsSuccess && res.Data != null)
                Items = res.Data.ToList();
            else if (!res.IsSuccess)
            {
                if (await _appState.HandleUnauthorizedAsync(res.StatusCode, _nav))
                    return;
                SetError(res.ErrorMessage ?? "Failed to load history.");
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
