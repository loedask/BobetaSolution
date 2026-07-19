using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Games;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Bobeta.Application.Games.Kopo;

public class KopoGameEngine(
    IGameSessionRepository sessionRepository,
    IGameMoveRepository moveRepository,
    IGameResultRepository resultRepository,
    IWalletService walletService,
    IPlayerRepository playerRepository,
    IGameRevenueService gameRevenueService,
    IInfluencerAttributionService influencerAttribution,
    INotificationService notificationService,
    ILogger<KopoGameEngine> logger) : IGameEngine
{
    private const decimal CommissionRate = 0.25m;
    private static JsonSerializerOptions JsonOptions => GameJson.Options;

    public GameVariant Variant => GameVariant.Kopo;

    public async Task StartGameAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        if (session.Status != GameStatus.Waiting || session.OpponentPlayerId == null)
            throw new InvalidOperationException("Game is not ready to start.");
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId.Value;
        var first = PickFirstTurn(session.Id, creatorId, opponentId);
        var state = KopoRules.CreateInitial(creatorId, opponentId, first);
        session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);
        session.Status = GameStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        await sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task<GameMoveResult> ApplyMoveAsync(
        Guid playerId,
        Guid sessionId,
        IReadOnlyList<(int Row, int Col)> path,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.GameStateJson == null || session.Status != GameStatus.InProgress || session.Variant != GameVariant.Kopo)
            return GameMoveResult.Fail(GameMoveErrorCodes.InvalidState);

        var state = JsonSerializer.Deserialize<KopoGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId!.Value;

        if (!KopoRules.TryApplyMove(state, creatorId, opponentId, playerId, path, out var errorCode))
            return GameMoveResult.Fail(errorCode ?? GameMoveErrorCodes.InvalidMove);

        var moveOrder = await moveRepository.GetCountByGameSessionIdAsync(sessionId, cancellationToken);
        var moveNotation = string.Join(">", path.Select(p => $"{p.Row},{p.Col}"));
        await moveRepository.AddAsync(new GameMove
        {
            Id = Guid.NewGuid(),
            GameSessionId = sessionId,
            PlayerId = playerId,
            CardSuitRank = $"Kopo:{moveNotation}",
            MoveOrder = moveOrder,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var (winnerId, loserId, isDraw) = KopoRules.CheckOutcome(state, creatorId, opponentId);
        if (isDraw)
        {
            logger.LogWarning("Kopo draw session={SessionId} — releasing bets (no winner settlement).", sessionId);
            await ReleaseBetsAsync(session, cancellationToken);
            session.Status = GameStatus.Finished;
            session.GameStateJson = null;
            session.FinishedAt = DateTime.UtcNow;
        }
        else if (winnerId.HasValue && loserId.HasValue)
        {
            await FinalizeGameAsync(session, winnerId.Value, loserId.Value, cancellationToken);
            session.GameStateJson = null;
            session.Status = GameStatus.Finished;
            session.FinishedAt = DateTime.UtcNow;
        }
        else
        {
            session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);
        }

        await sessionRepository.UpdateAsync(session, cancellationToken);
        var dto = await GetGameStateAsync(session, playerId, cancellationToken);
        return dto == null ? GameMoveResult.Fail(GameMoveErrorCodes.InvalidState) : GameMoveResult.Ok(dto);
    }

    public async Task<GameStateDto?> GetGameStateAsync(GameSession session, Guid playerId, CancellationToken cancellationToken = default)
    {
        var sessionId = session.Id;
        var isParticipant = playerId == session.CreatorPlayerId
            || (session.OpponentPlayerId.HasValue && session.OpponentPlayerId.Value == playerId);
        if (!isParticipant)
            return null;

        var lobbyPot = session.BetAmount * 2m;
        var opponentName = await ResolveOpponentDisplayNameAsync(playerId, session, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(session.GameStateJson))
        {
            if (session.Status == GameStatus.Waiting)
                return LobbyState(sessionId, lobbyPot, opponentName, waiting: true);
            if (session.Status == GameStatus.Finished)
                return LobbyState(sessionId, lobbyPot, opponentName, waiting: false, gameOver: true, winner: session.GameResult?.WinnerPlayerId, isDraw: session.GameResult?.WinnerPlayerId == null);
            if (session.Status == GameStatus.Cancelled)
                return LobbyState(sessionId, lobbyPot, opponentName, waiting: false, gameOver: true, winner: null);
            return null;
        }

        var state = JsonSerializer.Deserialize<KopoGameState>(session.GameStateJson, JsonOptions)!;
        var kopo = new KopoStateDto(
            KopoBoard.Size,
            state.Pieces.Select(p => new KopoPieceDto(p.Id, p.OwnerId, p.Row, p.Col, p.IsKing)).ToList(),
            state.ChainPieceId.HasValue,
            state.ChainPieceId);

        var gameOver = session.Status == GameStatus.Finished;
        return new GameStateDto(
            sessionId,
            Array.Empty<string>(),
            null,
            state.CurrentTurnPlayerId,
            gameOver,
            session.GameResult?.WinnerPlayerId,
            false,
            lobbyPot,
            opponentName,
            null,
            0,
            0,
            false,
            GameVariant.Kopo,
            kopo);
    }

    private static GameStateDto LobbyState(
        Guid sessionId,
        decimal lobbyPot,
        string? opponentName,
        bool waiting,
        bool gameOver = false,
        Guid? winner = null,
        bool isDraw = false) =>
        new(sessionId, Array.Empty<string>(), null, null, gameOver, winner, waiting, lobbyPot, opponentName,
            null, 0, 0, false, GameVariant.Kopo, null, null, null, null, null, isDraw);

    private async Task<string?> ResolveOpponentDisplayNameAsync(Guid viewerPlayerId, GameSession session, CancellationToken cancellationToken)
    {
        Guid? opponentId = viewerPlayerId == session.CreatorPlayerId
            ? session.OpponentPlayerId
            : session.OpponentPlayerId.HasValue && session.OpponentPlayerId.Value == viewerPlayerId
                ? session.CreatorPlayerId
                : null;
        if (opponentId == null)
            return null;
        var opponent = await playerRepository.GetByIdAsync(opponentId.Value, cancellationToken).ConfigureAwait(false);
        var name = opponent?.PlayerName?.Trim();
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private async Task FinalizeGameAsync(GameSession session, Guid winnerId, Guid loserId, CancellationToken cancellationToken)
    {
        var totalPot = session.BetAmount * 2;
        var commission = totalPot * CommissionRate;
        var winnerAmount = totalPot - commission;
        await walletService.SettleGameAsync(
            winnerId,
            loserId,
            session.BetAmount,
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

        await notificationService.NotifyGameResultAsync(winnerId, session.Id, won: true, winnerAmount, cancellationToken);
        await notificationService.NotifyGameResultAsync(loserId, session.Id, won: false, session.BetAmount, cancellationToken);
    }

    private async Task ReleaseBetsAsync(GameSession session, CancellationToken cancellationToken)
    {
        if (session.OpponentPlayerId is not { } opponentId)
            return;
        await walletService.ReleaseBetAsync(session.CreatorPlayerId, GameSessionService.ChargedAmount(session, session.CreatorPlayerId), cancellationToken);
        await walletService.ReleaseBetAsync(opponentId, GameSessionService.ChargedAmount(session, opponentId), cancellationToken);
        await influencerAttribution.DetachGameRedemptionsAsync(session.Id, cancellationToken);
    }

    private static Guid PickFirstTurn(Guid sessionId, Guid creatorId, Guid opponentId)
    {
        var seed = sessionId.GetHashCode() ^ 0x4B_50_50_4F;
        return new Random(seed).Next(2) == 0 ? creatorId : opponentId;
    }
}
