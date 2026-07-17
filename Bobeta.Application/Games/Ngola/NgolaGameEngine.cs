using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Bobeta.Application.Games.Ngola;

public sealed class NgolaGameEngine(
    IGameSessionRepository sessionRepository,
    IGameMoveRepository moveRepository,
    IGameResultRepository resultRepository,
    IWalletService walletService,
    IPlayerRepository playerRepository,
    IGameRevenueService gameRevenueService,
    IInfluencerAttributionService influencerAttribution,
    INotificationService notificationService,
    ILogger<NgolaGameEngine> logger) : IGameEngine
{
    private const decimal CommissionRate = 0.25m;
    private static JsonSerializerOptions JsonOptions => GameJson.Options;

    public GameVariant Variant => GameVariant.Ngola;

    public async Task StartGameAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        if (session.Status != GameStatus.Waiting || session.OpponentPlayerId == null)
            throw new InvalidOperationException("Game is not ready to start.");

        var first = PickFirstTurn(session.Id, session.CreatorPlayerId, session.OpponentPlayerId.Value);
        session.GameStateJson = JsonSerializer.Serialize(NgolaRules.CreateInitial(first), JsonOptions);
        session.Status = GameStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        await sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task<GameMoveResult> ApplyMoveAsync(
        Guid playerId,
        Guid sessionId,
        int pitIndex,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.GameStateJson == null || session.Status != GameStatus.InProgress
            || session.Variant != GameVariant.Ngola || session.OpponentPlayerId == null)
            return GameMoveResult.Fail(GameMoveErrorCodes.InvalidState);

        var state = JsonSerializer.Deserialize<NgolaGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId.Value;
        if (!NgolaRules.TryApplyMove(state, creatorId, opponentId, playerId, pitIndex, out var errorCode))
            return GameMoveResult.Fail(errorCode ?? GameMoveErrorCodes.InvalidMove);

        var moveOrder = await moveRepository.GetCountByGameSessionIdAsync(sessionId, cancellationToken);
        await moveRepository.AddAsync(new GameMove
        {
            Id = Guid.NewGuid(),
            GameSessionId = sessionId,
            PlayerId = playerId,
            CardSuitRank = $"Ngola:{pitIndex}",
            MoveOrder = moveOrder,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var (winnerId, loserId, isDraw) = NgolaRules.CompleteIfBlocked(state, creatorId, opponentId);
        if (isDraw)
        {
            logger.LogWarning("Ngola draw session={SessionId} — releasing bets.", sessionId);
            await ReleaseBetsAsync(session, cancellationToken);
            session.Status = GameStatus.Finished;
            session.FinishedAt = DateTime.UtcNow;
        }
        else if (winnerId.HasValue && loserId.HasValue)
        {
            await FinalizeGameAsync(session, winnerId.Value, loserId.Value, cancellationToken);
            session.Status = GameStatus.Finished;
            session.FinishedAt = DateTime.UtcNow;
        }

        session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);
        await sessionRepository.UpdateAsync(session, cancellationToken);
        var dto = await GetGameStateAsync(session, playerId, cancellationToken);
        return dto == null ? GameMoveResult.Fail(GameMoveErrorCodes.InvalidState) : GameMoveResult.Ok(dto);
    }

    public async Task<GameStateDto?> GetGameStateAsync(
        GameSession session,
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var isParticipant = playerId == session.CreatorPlayerId
            || session.OpponentPlayerId == playerId;
        if (!isParticipant)
            return null;

        var pot = session.BetAmount * 2m;
        var opponentName = await ResolveOpponentDisplayNameAsync(playerId, session, cancellationToken);
        if (string.IsNullOrEmpty(session.GameStateJson))
        {
            return new GameStateDto(
                session.Id, Array.Empty<string>(), null, null,
                session.Status == GameStatus.Finished, session.GameResult?.WinnerPlayerId,
                session.Status == GameStatus.Waiting, pot, opponentName,
                null, 0, 0, false, GameVariant.Ngola);
        }

        var state = JsonSerializer.Deserialize<NgolaGameState>(session.GameStateJson, JsonOptions)!;
        var viewerIsCreator = playerId == session.CreatorPlayerId;
        var ngola = new NgolaStateDto(
            NgolaRules.PitsPerPlayer,
            NgolaRules.PitsForViewer(state, playerId, session.CreatorPlayerId, ownRow: true),
            NgolaRules.PitsForViewer(state, playerId, session.CreatorPlayerId, ownRow: false),
            viewerIsCreator ? state.CreatorScore : state.OpponentScore,
            viewerIsCreator ? state.OpponentScore : state.CreatorScore);
        return new GameStateDto(
            session.Id, Array.Empty<string>(), null, state.CurrentTurnPlayerId,
            session.Status == GameStatus.Finished, session.GameResult?.WinnerPlayerId,
            false, pot, opponentName, null, 0, 0, false,
            GameVariant.Ngola, null, ngola);
    }

    private async Task<string?> ResolveOpponentDisplayNameAsync(
        Guid viewerPlayerId,
        GameSession session,
        CancellationToken cancellationToken)
    {
        var opponentId = viewerPlayerId == session.CreatorPlayerId
            ? session.OpponentPlayerId
            : session.CreatorPlayerId;
        if (opponentId == null)
            return null;
        var opponent = await playerRepository.GetByIdAsync(opponentId.Value, cancellationToken);
        return string.IsNullOrWhiteSpace(opponent?.PlayerName) ? null : opponent.PlayerName.Trim();
    }

    private async Task FinalizeGameAsync(
        GameSession session,
        Guid winnerId,
        Guid loserId,
        CancellationToken cancellationToken)
    {
        var totalPot = session.BetAmount * 2;
        var commission = totalPot * CommissionRate;
        var winnerAmount = totalPot - commission;
        await walletService.SettleGameAsync(
            winnerId, loserId, session.BetAmount,
            GameSessionService.ChargedAmount(session, winnerId),
            GameSessionService.ChargedAmount(session, loserId),
            cancellationToken);
        var result = new GameResult
        {
            Id = Guid.NewGuid(),
            GameSessionId = session.Id,
            WinnerPlayerId = winnerId,
            LoserPlayerId = loserId,
            TotalPot = totalPot,
            WinnerAmount = winnerAmount,
            PlatformCommission = commission,
            CreatedAt = DateTime.UtcNow
        };
        await gameRevenueService.EnrichWithPartnerShareAsync(result, winnerId, cancellationToken);
        await resultRepository.AddAsync(result, cancellationToken);
        session.GameResult = result;
        await notificationService.NotifyGameResultAsync(winnerId, session.Id, true, winnerAmount, cancellationToken);
        await notificationService.NotifyGameResultAsync(loserId, session.Id, false, session.BetAmount, cancellationToken);
    }

    private async Task ReleaseBetsAsync(GameSession session, CancellationToken cancellationToken)
    {
        if (session.OpponentPlayerId is not { } opponentId)
            return;
        await walletService.ReleaseBetAsync(
            session.CreatorPlayerId,
            GameSessionService.ChargedAmount(session, session.CreatorPlayerId),
            cancellationToken);
        await walletService.ReleaseBetAsync(
            opponentId,
            GameSessionService.ChargedAmount(session, opponentId),
            cancellationToken);
        await influencerAttribution.DetachGameRedemptionsAsync(session.Id, cancellationToken);
    }

    private static Guid PickFirstTurn(Guid sessionId, Guid creatorId, Guid opponentId)
    {
        var seed = sessionId.GetHashCode() ^ 0x4E_47_4F_4C;
        return new Random(seed).Next(2) == 0 ? creatorId : opponentId;
    }
}
