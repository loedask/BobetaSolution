using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public class GameSessionService(
    IGameSessionRepository sessionRepository,
    IPlayerRepository playerRepository,
    IWalletService walletService) : IGameSessionService
{
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IPlayerRepository _playerRepository = playerRepository;
    private readonly IWalletService _walletService = walletService;

    public async Task<GameSessionDto> CreateGameAsync(Guid playerId, decimal betAmount, CancellationToken cancellationToken = default)
    {
        await _walletService.LockBetAsync(playerId, betAmount, cancellationToken);
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = playerId,
            BetAmount = betAmount,
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
        return Map(session);
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

    private static GameSessionDto Map(GameSession s) =>
        new(s.Id, s.CreatorPlayerId, s.OpponentPlayerId, s.BetAmount, s.Status, s.CreatedAt, s.StartedAt, s.FinishedAt);
}
