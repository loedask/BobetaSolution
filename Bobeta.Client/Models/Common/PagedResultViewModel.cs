namespace Bobeta.Client.Models.Common;

/// <summary>Paged list view model for API results.</summary>
public class PagedResultViewModel<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
