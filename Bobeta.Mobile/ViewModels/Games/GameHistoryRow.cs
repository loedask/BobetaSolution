namespace Bobeta.Mobile.ViewModels.Games;

public class GameHistoryRow
{
    public Guid SessionId { get; init; }
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
    public string Time { get; init; } = "";
    public string AmountText { get; init; } = "";
    public Color AmountColor { get; init; } = Colors.Gray;
    public bool ShowContinue { get; init; }
}
