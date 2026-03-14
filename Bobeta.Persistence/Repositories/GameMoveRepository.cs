using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>Repository implementation for GameMove (add, list by session, count).</summary>
public class GameMoveRepository : IGameMoveRepository
{
    private readonly BobetaDbContext _db;

    public GameMoveRepository(BobetaDbContext db) => _db = db;

    public async Task<GameMove> AddAsync(GameMove move, CancellationToken cancellationToken = default)
    {
        _db.GameMoves.Add(move);
        await _db.SaveChangesAsync(cancellationToken);
        return move;
    }

    public async Task<IReadOnlyList<GameMove>> GetByGameSessionIdAsync(Guid gameSessionId, CancellationToken cancellationToken = default) =>
        await _db.GameMoves
            .Where(m => m.GameSessionId == gameSessionId)
            .OrderBy(m => m.MoveOrder)
            .ToListAsync(cancellationToken);

    public async Task<int> GetCountByGameSessionIdAsync(Guid gameSessionId, CancellationToken cancellationToken = default) =>
        await _db.GameMoves.CountAsync(m => m.GameSessionId == gameSessionId, cancellationToken);
}
