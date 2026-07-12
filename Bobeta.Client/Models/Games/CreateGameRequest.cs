using Bobeta.Client.Models.Api;

namespace Bobeta.Client.Models.Games;

/// <summary>Request to create a new game.</summary>
public class CreateGameRequest
{
    public decimal BetAmount { get; set; }
    public GameVariant Variant { get; set; } = GameVariant.Makopa;
}
