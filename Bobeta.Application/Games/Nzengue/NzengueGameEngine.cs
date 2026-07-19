using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Bobeta.Application.Games.Nzengue;

public sealed class NzengueGameEngine(
    IGameSessionRepository sessionRepository,
    IGameMoveRepository moveRepository,
    IGameResultRepository resultRepository,
    IWalletService walletService,
    IPlayerRepository playerRepository,
    IGameRevenueService gameRevenueService,
    IInfluencerAttributionService influencerAttribution,
    INotificationService notificationService,
    ILogger<NzengueGameEngine> logger) : IGameEngine
{
    private const decimal CommissionRate = 0.25m;
    private static JsonSerializerOptions JsonOptions => GameJson.Options;

    public GameVariant Variant => GameVariant.Nzengue;

    public async Task StartGameAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        if (session.Status != GameStatus.Waiting || session.OpponentPlayerId == null)
            throw new InvalidOperationException("Game is not ready to start.");

        var first = PickFirstTurn(session.Id, session.CreatorPlayerId, session.OpponentPlayerId.Value);
        session.GameStateJson = JsonSerializer.Serialize(NzengueRules.CreateInitial(first), JsonOptions);
        session.Status = GameStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        await sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task<GameMoveResult> ApplyMoveAsync(
        Guid playerId,
        Guid sessionId,
        int? fromPoint,
        int toPoint,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.GameStateJson == null || session.Status != GameStatus.InProgress
            || session.Variant != GameVariant.Nzengue || session.OpponentPlayerId == null)
            return GameMoveResult.Fail(GameMoveErrorCodes.InvalidState);

        var state = JsonSerializer.Deserialize<NzengueGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId.Value;

        string? errorCode;
        string marker;
        if (fromPoint == null)
        {
            if (!NzengueRules.TryApplyPlace(state, creatorId, opponentId, playerId, toPoint, out errorCode))
                return GameMoveResult.Fail(errorCode ?? GameMoveErrorCodes.InvalidMove);
            marker = NzengueRules.FormatPlaceMarker(toPoint);
        }
        else
        {
            if (!NzengueRules.TryApplyMove(
                    state, creatorId, opponentId, playerId, fromPoint.Value, toPoint, out errorCode))
                return GameMoveResult.Fail(errorCode ?? GameMoveErrorCodes.InvalidMove);
            marker = NzengueRules.FormatMoveMarker(fromPoint.Value, toPoint);
        }

        var moveOrder = await moveRepository.GetCountByGameSessionIdAsync(sessionId, cancellationToken);
        await moveRepository.AddAsync(new GameMove
        {
            Id = Guid.NewGuid(),
            GameSessionId = sessionId,
            PlayerId = playerId,
            CardSuitRank = marker,
            MoveOrder = moveOrder,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var (winnerId, loserId, isDraw) = NzengueRules.EvaluateAfterMove(
            state, creatorId, opponentId, playerId);
        if (isDraw)
        {
            logger.LogWarning("Nzengue draw session={SessionId} — releasing bets.", sessionId);
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

        var state = JsonSerializer.Deserialize<NzengueGameState>(session.GameStateJson, JsonOptions)!;
        var nzengue = ToDto(state, playerId, session.CreatorPlayerId);
        return new GameStateDto(
            session.Id, Array.Empty<string>(), null, state.CurrentTurnPlayerId,
            session.Status == GameStatus.Finished, session.GameResult?.WinnerPlayerId,
            false, pot, opponentName, null, 0, 0, false,
            GameVariant.Nzengue, null, null, null, null, nzengue,
            IsDraw: session.Status == GameStatus.Finished && session.GameResult?.WinnerPlayerId == null);
    }

    internal static NzengueStateDto ToDto(NzengueGameState state, Guid viewerId, Guid creatorId)
    {
        var viewerIsCreator = viewerId == creatorId;
        var occupancy = new int[NzengueRules.PointCount];
        for (var i = 0; i < NzengueRules.PointCount; i++)
        {
            var owner = state.Points[i];
            if (owner == null)
                occupancy[i] = 0;
            else if (owner == viewerId)
                occupancy[i] = 1;
            else
                occupancy[i] = 2;
        }

        var phase = NzengueRules.PhaseOf(state);
        var myToPlace = viewerIsCreator ? state.CreatorPiecesToPlace : state.OpponentPiecesToPlace;
        var oppToPlace = viewerIsCreator ? state.OpponentPiecesToPlace : state.CreatorPiecesToPlace;
        var canAct = state.CurrentTurnPlayerId == viewerId;
        IReadOnlyList<int> legalPlaces = Array.Empty<int>();
        IReadOnlyList<NzengueEdgeDto> legalMoves = Array.Empty<NzengueEdgeDto>();
        if (canAct && phase == NzengueRules.PhasePlace)
            legalPlaces = NzengueRules.LegalPlacePoints(state);
        else if (canAct && phase == NzengueRules.PhaseMove)
            legalMoves = NzengueRules.LegalMoves(state, viewerId)
                .Select(m => new NzengueEdgeDto(m.From, m.To))
                .ToList();

        return new NzengueStateDto(
            NzengueRules.PointCount,
            NzengueRules.PiecesPerPlayer,
            phase,
            occupancy,
            myToPlace,
            oppToPlace,
            legalPlaces,
            legalMoves,
            canAct && (legalPlaces.Count > 0 || legalMoves.Count > 0));
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
            null, 0, 0, false, GameVariant.Nzengue, null, null, null, null, null, isDraw);

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
        var seed = sessionId.GetHashCode() ^ 0x4E_5A_45_4E;
        return new Random(seed).Next(2) == 0 ? creatorId : opponentId;
    }
}
