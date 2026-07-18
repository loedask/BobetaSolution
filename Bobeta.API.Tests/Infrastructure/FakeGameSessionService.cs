using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;

namespace Bobeta.API.Tests.Infrastructure;

internal sealed class FakeGameSessionService : IGameSessionService
{
  public static Guid? LastCreatePlayerId { get; private set; }
  public static decimal? LastCreateBetAmount { get; private set; }
  public static GameVariant? LastCreateVariant { get; private set; }

  public static Guid? LastForfeitLoserId { get; private set; }
  public static Guid? LastForfeitSessionId { get; private set; }
  public static ForfeitGameOutcome? NextForfeitOutcome { get; set; }

  public static void ResetForfeitTracking()
  {
    LastForfeitLoserId = null;
    LastForfeitSessionId = null;
    NextForfeitOutcome = null;
  }

  public Task<GameSessionDto> CreateGameAsync(Guid playerId, decimal betAmount, GameVariant variant = GameVariant.Makopa, CancellationToken cancellationToken = default)
  {
    LastCreatePlayerId = playerId;
    LastCreateBetAmount = betAmount;
    LastCreateVariant = variant;

    return Task.FromResult(new GameSessionDto(
      Guid.NewGuid(),
      playerId,
      null,
      betAmount,
      GameStatus.Waiting,
      variant,
      DateTime.UtcNow,
      null,
      null));
  }

  public Task<GameSessionDto?> JoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();

  public Task<IReadOnlyList<GameSessionDto>> ListOpenJoinableGamesAsync(Guid playerId, int skip = 0, int take = 50, GameVariant? variant = null, CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();

  public Task<IReadOnlyList<GameSessionDto>> ListMyWaitingGamesAsync(Guid playerId, int skip = 0, int take = 50, GameVariant? variant = null, CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();

  public Task ProposeNewBetAsync(Guid playerId, Guid gameId, decimal amount, CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();

  public Task AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();

  public Task<bool> CancelInProgressGameAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();

  public Task<ForfeitGameOutcome?> ForfeitGameAsync(Guid loserPlayerId, Guid sessionId, CancellationToken cancellationToken = default)
  {
    LastForfeitLoserId = loserPlayerId;
    LastForfeitSessionId = sessionId;
    return Task.FromResult(NextForfeitOutcome);
  }

  public Task<bool> CancelWaitingGameAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();
}
