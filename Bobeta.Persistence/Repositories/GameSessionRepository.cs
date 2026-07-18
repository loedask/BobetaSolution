using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>Repository implementation for GameSession (get by id, waiting list, player history, add, update).</summary>
public class GameSessionRepository : IGameSessionRepository
{
    private readonly BobetaDbContext _db;

    public GameSessionRepository(BobetaDbContext db) => _db = db;

    public async Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.GameSessions
            .Include(s => s.GameResult)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GameSession>> GetWaitingSessionsAsync(decimal betAmount, CancellationToken cancellationToken = default) =>
        await _db.GameSessions
            .Where(s => s.Status == GameStatus.Waiting && s.BetAmount == betAmount && s.OpponentPlayerId == null)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GameSession>> GetJoinableWaitingSessionsAsync(Guid forPlayerId, int skip, int take, GameVariant? variant = null, CancellationToken cancellationToken = default)
    {
        var q = _db.GameSessions
            .Where(s => s.Status == GameStatus.Waiting && s.OpponentPlayerId == null && s.CreatorPlayerId != forPlayerId);
        if (variant.HasValue)
            q = q.Where(s => s.Variant == variant.Value);
        return await q.OrderByDescending(s => s.CreatedAt).Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default) =>
        await _db.GameSessions
            .Include(s => s.GameResult)
            .Where(s => s.CreatorPlayerId == playerId || s.OpponentPlayerId == playerId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);

    public Task<bool> HasOpenWaitingSeatAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GameSessions.AnyAsync(
            s => s.CreatorPlayerId == playerId
                 && s.Status == GameStatus.Waiting
                 && s.OpponentPlayerId == null,
            cancellationToken);

    public Task<bool> HasInProgressGameAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GameSessions.AnyAsync(
            s => s.Status == GameStatus.InProgress
                 && (s.CreatorPlayerId == playerId || s.OpponentPlayerId == playerId),
            cancellationToken);

    public async Task<GameSession> AddAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        _db.GameSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task UpdateAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        _db.GameSessions.Update(session);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
