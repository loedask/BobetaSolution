namespace Bobeta.Web.ViewModels.Games;

public class CardViewModel
{
    public string Suit { get; set; } = "";
    public string Rank { get; set; } = "";
    public string DisplayValue { get; set; } = "";
    public string CssClass { get; set; } = "";

    /// <summary>False when follow-suit requires a different card (server would reject the play).</summary>
    public bool IsPlayable { get; set; } = true;
}
