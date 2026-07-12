using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

/// <summary>Application service for game session lifecycle: create game (lock bet), join game, propose/accept bet changes.</summary>
public class GameSessionService(
    IGameSessionRepository sessionRepository,
    IPlayerRepository playerRepository,
    IWalletService walletService,
    IGameEngineService gameEngine,
    IGameSessionNotifier sessionNotifier) : IGameSessionService
{
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IPlayerRepository _playerRepository = playerRepository;
    private readonly IWalletService _walletService = walletService;
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly IGameSessionNotifier _sessionNotifier = sessionNotifier;

    public async Task<GameSessionDto> CreateGameAsync(Guid playerId, decimal betAmount, GameVariant variant = GameVariant.Makopa, CancellationToken cancellationToken = default)
    {
        await _walletService.LockBetAsync(playerId, betAmount, cancellationToken);
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = playerId,
            BetAmount = betAmount,
            Variant = variant,
            Status = GameStatus.Waiting,
            CreatedAt = DateTime.UtcNow
        };
        await _sessionRepository.AddAsync(session, cancellationToken);
        return Map(session);
    }

    public async Task<GameSessionDto?> JoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(gameId, cancellationToken);
        if (session == null || session.Status != GameStatus.Waiting || session.OpponentPlayerId != null)
            return null;
        if (session.CreatorPlayerId == playerId)
            return Map(session);

        await _walletService.LockBetAsync(playerId, session.BetAmount, cancellationToken);
        session.OpponentPlayerId = playerId;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        try
        {
            await _gameEngine.StartGameAsync(gameId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Already dealing / race; still return current session.
        }

        session = await _sessionRepository.GetByIdAsync(gameId, cancellationToken);
        if (session == null)
            return null;

        await _sessionNotifier.NotifySessionAsync(gameId, cancellationToken);
        return Map(session);
    }

    public async Task<IReadOnlyList<GameSessionDto>> ListOpenJoinableGamesAsync(Guid playerId, int skip = 0, int take = 50, GameVariant? variant = null, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);
        var sessions = await _sessionRepository.GetJoinableWaitingSessionsAsync(playerId, skip, take, variant, cancellationToken);
        return sessions.Select(Map).ToList();
    }

    public Task ProposeNewBetAsync(Guid playerId, Guid gameId, decimal amount, CancellationToken cancellationToken = default)
    {
        // Bet change proposal is communicated via SignalR/notification; persistence of proposed amount can be added here if needed.
        return Task.CompletedTask;
    }

    public Task AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> CancelInProgressGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null || session.Status != GameStatus.InProgress || session.OpponentPlayerId == null)
            return false;

        await _walletService.ReleaseBetAsync(session.CreatorPlayerId, session.BetAmount, cancellationToken);
        await _walletService.ReleaseBetAsync(session.OpponentPlayerId.Value, session.BetAmount, cancellationToken);

        session.Status = GameStatus.Cancelled;
        session.GameStateJson = null;
        session.FinishedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        return true;
    }

    private static GameSessionDto Map(GameSession s) =>
        new(s.Id, s.CreatorPlayerId, s.OpponentPlayerId, s.BetAmount, s.Status, s.Variant, s.CreatedAt, s.StartedAt, s.FinishedAt);
}
