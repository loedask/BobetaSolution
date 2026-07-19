using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Bobeta.Application.Games.Abbia;

public sealed class AbbiaGameEngine(
    IGameSessionRepository sessionRepository,
    IGameMoveRepository moveRepository,
    IGameResultRepository resultRepository,
    IWalletService walletService,
    IPlayerRepository playerRepository,
    IGameRevenueService gameRevenueService,
    IInfluencerAttributionService influencerAttribution,
    INotificationService notificationService,
    ILogger<AbbiaGameEngine> logger) : IGameEngine
{
    private const decimal CommissionRate = 0.25m;
    private static JsonSerializerOptions JsonOptions => GameJson.Options;

    public GameVariant Variant => GameVariant.Abbia;

    public async Task StartGameAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        if (session.Status != GameStatus.Waiting || session.OpponentPlayerId == null)
            throw new InvalidOperationException("Game is not ready to start.");

        var state = AbbiaRules.CreateInitial(
            session.Id, session.CreatorPlayerId, session.OpponentPlayerId.Value);
        session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);
        session.Status = GameStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        await sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task<GameMoveResult> ApplyThrowAsync(
        Guid playerId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.GameStateJson == null || session.Status != GameStatus.InProgress
            || session.Variant != GameVariant.Abbia || session.OpponentPlayerId == null)
            return GameMoveResult.Fail(GameMoveErrorCodes.InvalidState);

        var state = JsonSerializer.Deserialize<AbbiaGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId.Value;
        var moveOrder = await moveRepository.GetCountByGameSessionIdAsync(sessionId, cancellationToken);
        if (!AbbiaRules.TryApplyThrow(
                state, creatorId, opponentId, playerId, sessionId, moveOrder, out var errorCode))
            return GameMoveResult.Fail(errorCode ?? GameMoveErrorCodes.InvalidMove);

        var carvedUp = playerId == creatorId
            ? AbbiaRules.CarvedUpCount(state.CreatorTokens)
            : AbbiaRules.CarvedUpCount(state.OpponentTokens);
        await moveRepository.AddAsync(new GameMove
        {
            Id = Guid.NewGuid(),
            GameSessionId = sessionId,
            PlayerId = playerId,
            CardSuitRank = AbbiaRules.FormatMoveMarker(carvedUp),
            MoveOrder = moveOrder,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var (winnerId, loserId, isDraw) = AbbiaRules.Evaluate(state, creatorId, opponentId);
        if (isDraw)
        {
            logger.LogWarning("Abbia draw session={SessionId} — releasing bets.", sessionId);
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
            if (session.Status == GameStatus.Waiting)
                return LobbyState(session.Id, pot, opponentName, waiting: true);
            if (session.Status == GameStatus.Finished)
                return LobbyState(
                    session.Id, pot, opponentName, waiting: false, gameOver: true,
                    winner: session.GameResult?.WinnerPlayerId,
                    isDraw: session.GameResult?.WinnerPlayerId == null);
            if (session.Status == GameStatus.Cancelled)
                return LobbyState(session.Id, pot, opponentName, waiting: false, gameOver: true);
            return null;
        }

        var state = JsonSerializer.Deserialize<AbbiaGameState>(session.GameStateJson, JsonOptions)!;
        var viewerIsCreator = playerId == session.CreatorPlayerId;
        var iHaveThrown = viewerIsCreator ? state.CreatorThrown : state.OpponentThrown;
        var opponentHasThrown = viewerIsCreator ? state.OpponentThrown : state.CreatorThrown;
        var myTokens = iHaveThrown
            ? (IReadOnlyList<bool>)(viewerIsCreator ? state.CreatorTokens : state.OpponentTokens)
            : null;
        var opponentTokens = opponentHasThrown
            ? (IReadOnlyList<bool>)(viewerIsCreator ? state.OpponentTokens : state.CreatorTokens)
            : null;
        var abbia = new AbbiaStateDto(
            AbbiaRules.TokenCount,
            myTokens,
            opponentTokens,
            myTokens == null ? null : AbbiaRules.CarvedUpCount(myTokens),
            opponentTokens == null ? null : AbbiaRules.CarvedUpCount(opponentTokens),
            iHaveThrown,
            opponentHasThrown,
            state.CurrentTurnPlayerId == playerId && !iHaveThrown);
        return new GameStateDto(
            session.Id, Array.Empty<string>(), null, state.CurrentTurnPlayerId,
            session.Status == GameStatus.Finished, session.GameResult?.WinnerPlayerId,
            false, pot, opponentName, null, 0, 0, false,
            GameVariant.Abbia, null, null, null, abbia, null, null,
            IsDraw: session.Status == GameStatus.Finished && session.GameResult?.WinnerPlayerId == null);
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
            null, 0, 0, false, GameVariant.Abbia, null, null, null, null, null, null, isDraw);

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
}
