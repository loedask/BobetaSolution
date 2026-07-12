using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Games;
using Bobeta.Application.Games.Kopo;
using Bobeta.Application.Games.Makopa;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Services;

/// <summary>Routes gameplay to the engine for the session&apos;s <see cref="GameVariant"/>.</summary>
public class GameEngineService(
    IGameSessionRepository sessionRepository,
    MakopaGameEngine makopa,
    KopoGameEngine kopo) : IGameEngineService
{
    public Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        WithSessionAsync(sessionId, async (session, engine) =>
        {
            await engine.StartGameAsync(session, cancellationToken);
        }, cancellationToken);

    public async Task<GameMoveResult> PlayCardAsync(Guid playerId, Guid sessionId, Card card, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.Variant != GameVariant.Makopa)
            return GameMoveResult.Fail(GameMoveErrorCodes.InvalidState);
        return await makopa.PlayCardAsync(playerId, sessionId, card, cancellationToken);
    }

    public async Task<GameMoveResult> VoidFollowDrawAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.Variant != GameVariant.Makopa)
            return GameMoveResult.Fail(GameMoveErrorCodes.InvalidState);
        return await makopa.VoidFollowDrawAsync(playerId, sessionId, cancellationToken);
    }

    public async Task<GameMoveResult> ApplyKopoMoveAsync(
        Guid playerId,
        Guid sessionId,
        IReadOnlyList<(int Row, int Col)> path,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.Variant != GameVariant.Kopo)
            return GameMoveResult.Fail(GameMoveErrorCodes.InvalidState);
        return await kopo.ApplyMoveAsync(playerId, sessionId, path, cancellationToken);
    }

    public async Task<GameStateDto?> GetGameStateAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
            return null;
        return await Resolve(session).GetGameStateAsync(session, playerId, cancellationToken);
    }

    private IGameEngine Resolve(GameSession session) =>
        session.Variant switch
        {
            GameVariant.Kopo => kopo,
            _ => makopa
        };

    private async Task WithSessionAsync(
        Guid sessionId,
        Func<GameSession, IGameEngine, Task> action,
        CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Game session not found.");
        await action(session, Resolve(session));
    }
}
