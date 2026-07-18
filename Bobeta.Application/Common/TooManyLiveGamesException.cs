namespace Bobeta.Application.Common;

/// <summary>Thrown when a player already holds the max number of live matches and tries to join another.</summary>
public sealed class TooManyLiveGamesException(int maxGames) : InvalidOperationException(
    $"You can play up to {maxGames} matches at once. Finish one from History before joining another.")
{
    public int MaxGames { get; } = maxGames;
}
