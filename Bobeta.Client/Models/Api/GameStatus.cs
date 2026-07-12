namespace Bobeta.Client.Models.Api;

/// <summary>Matches <see cref="Bobeta.Domain.Enums.GameStatus"/> JSON values.</summary>
public enum GameStatus
{
    Waiting = 0,
    InProgress = 1,
    Finished = 2,
    Cancelled = 3
}
