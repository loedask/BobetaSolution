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
    IGameSessionNotifier sessionNotifier,
    IInfluencerAttributionService influencerAttribution,
    INotificationService notificationService) : IGameSessionService
{
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IPlayerRepository _playerRepository = playerRepository;
    private readonly IWalletService _walletService = walletService;
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly IGameSessionNotifier _sessionNotifier = sessionNotifier;
    private readonly IInfluencerAttributionService _influencerAttribution = influencerAttribution;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<GameSessionDto> CreateGameAsync(Guid playerId, decimal betAmount, GameVariant variant = GameVariant.Makopa, CancellationToken cancellationToken = default)
    {
        if (await _sessionRepository.HasOpenWaitingSeatAsync(playerId, cancellationToken))
            throw new InvalidOperationException(
                "You already have an open table waiting for an opponent. Cancel it or wait for a join before creating another.");

        var chargeAmount = await _influencerAttribution.GetChargeAmountAsync(playerId, betAmount, cancellationToken);
        await _walletService.LockBetAsync(playerId, chargeAmount, cancellationToken);

        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = playerId,
            BetAmount = betAmount,
            CreatorChargedAmount = chargeAmount,
            Variant = variant,
            Status = GameStatus.Waiting,
            CreatedAt = DateTime.UtcNow
        };
        await _sessionRepository.AddAsync(session, cancellationToken);

        try
        {
            await _influencerAttribution.AttachPendingCodeToGameAsync(playerId, session.Id, cancellationToken);
        }
        catch
        {
            await _walletService.ReleaseBetAsync(playerId, chargeAmount, cancellationToken);
            throw;
        }

        return Map(session);
    }

    public async Task<GameSessionDto?> JoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(gameId, cancellationToken);
        if (session == null || session.Status != GameStatus.Waiting || session.OpponentPlayerId != null)
            return null;
        if (session.CreatorPlayerId == playerId)
            return Map(session);

        if (await _sessionRepository.HasInProgressGameAsync(playerId, cancellationToken))
            throw new InvalidOperationException(
                "Finish your current match before joining another game.");

        var chargeAmount = await _influencerAttribution.GetChargeAmountAsync(playerId, session.BetAmount, cancellationToken);
        await _walletService.LockBetAsync(playerId, chargeAmount, cancellationToken);
        session.OpponentPlayerId = playerId;
        session.OpponentChargedAmount = chargeAmount;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        try
        {
            await _influencerAttribution.AttachPendingCodeToGameAsync(playerId, session.Id, cancellationToken);
        }
        catch
        {
            await _walletService.ReleaseBetAsync(playerId, chargeAmount, cancellationToken);
            session.OpponentPlayerId = null;
            session.OpponentChargedAmount = null;
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            throw;
        }

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

        var joiner = await _playerRepository.GetByIdAsync(playerId, cancellationToken);
        var joinerName = string.IsNullOrWhiteSpace(joiner?.PlayerName) ? "Opponent" : joiner!.PlayerName.Trim();
        await _notificationService.NotifyOpponentJoinedAsync(
            session.CreatorPlayerId,
            gameId,
            joinerName,
            session.BetAmount,
            cancellationToken);

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

        var creatorCharged = ChargedAmount(session, session.CreatorPlayerId);
        var opponentCharged = ChargedAmount(session, session.OpponentPlayerId.Value);

        await _walletService.ReleaseBetAsync(session.CreatorPlayerId, creatorCharged, cancellationToken);
        await _walletService.ReleaseBetAsync(session.OpponentPlayerId.Value, opponentCharged, cancellationToken);
        await _influencerAttribution.DetachGameRedemptionsAsync(sessionId, cancellationToken);

        session.Status = GameStatus.Cancelled;
        session.GameStateJson = null;
        session.FinishedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CancelWaitingGameAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null
            || session.Status != GameStatus.Waiting
            || session.OpponentPlayerId != null
            || session.CreatorPlayerId != playerId)
            return false;

        var creatorCharged = ChargedAmount(session, session.CreatorPlayerId);
        await _walletService.ReleaseBetAsync(session.CreatorPlayerId, creatorCharged, cancellationToken);
        await _influencerAttribution.DetachGameRedemptionsAsync(sessionId, cancellationToken);

        session.Status = GameStatus.Cancelled;
        session.GameStateJson = null;
        session.FinishedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        return true;
    }

    internal static decimal ChargedAmount(GameSession session, Guid playerId)
    {
        if (playerId == session.CreatorPlayerId)
            return session.CreatorChargedAmount > 0 ? session.CreatorChargedAmount : session.BetAmount;
        if (playerId == session.OpponentPlayerId)
            return session.OpponentChargedAmount is > 0 ? session.OpponentChargedAmount.Value : session.BetAmount;
        throw new InvalidOperationException("Player is not part of this session.");
    }

    private static GameSessionDto Map(GameSession s) =>
        new(s.Id, s.CreatorPlayerId, s.OpponentPlayerId, s.BetAmount, s.Status, s.Variant, s.CreatedAt, s.StartedAt, s.FinishedAt);
}
