using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for game result records (created when a game finishes).</summary>
public interface IGameResultRepository
{
    /// <summary>Stores the result of a finished game (winner, loser, amounts, commission).</summary>
    Task<GameResult> AddAsync(GameResult result, CancellationToken cancellationToken = default);
}
