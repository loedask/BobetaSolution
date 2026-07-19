using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Bobeta.Application.Games.Yote;

public sealed class YoteGameEngine(
    IGameSessionRepository sessionRepository,
    IGameMoveRepository moveRepository,
    IGameResultRepository resultRepository,
    IWalletService walletService,
    IPlayerRepository playerRepository,
    IGameRevenueService gameRevenueService,
    IInfluencerAttributionService influencerAttribution,
    INotificationService notificationService,
    ILogger<YoteGameEngine> logger) : IGameEngine
{
    private const decimal CommissionRate = 0.25m;
    private static JsonSerializerOptions JsonOptions => GameJson.Options;

    public GameVariant Variant => GameVariant.Yote;

    public async Task StartGameAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        if (session.Status != GameStatus.Waiting || session.OpponentPlayerId == null)
            throw new InvalidOperationException("Game is not ready to start.");

        var first = PickFirstTurn(session.Id, session.CreatorPlayerId, session.OpponentPlayerId.Value);
        session.GameStateJson = JsonSerializer.Serialize(YoteRules.CreateInitial(first), JsonOptions);
        session.Status = GameStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        await sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task<GameMoveResult> ApplyMoveAsync(
        Guid playerId,
        Guid sessionId,
        int? fromCell,
        int toCell,
        int? extraRemoveCell,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.GameStateJson == null || session.Status != GameStatus.InProgress
            || session.Variant != GameVariant.Yote || session.OpponentPlayerId == null)
            return GameMoveResult.Fail(GameMoveErrorCodes.InvalidState);

        var state = JsonSerializer.Deserialize<YoteGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId.Value;
        if (!YoteRules.TryApply(
                state, creatorId, opponentId, playerId, fromCell, toCell, extraRemoveCell, out var errorCode))
            return GameMoveResult.Fail(errorCode ?? GameMoveErrorCodes.InvalidMove);

        var marker = BuildMarker(fromCell, toCell, extraRemoveCell);
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

        var (winnerId, loserId, isDraw) = YoteRules.EvaluateAfterMove(state, creatorId, opponentId);
        if (isDraw)
        {
            logger.LogWarning("Yote draw session={SessionId} — releasing bets.", sessionId);
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

        var state = JsonSerializer.Deserialize<YoteGameState>(session.GameStateJson, JsonOptions)!;
        var yote = ToDto(state, playerId, session.CreatorPlayerId, session.OpponentPlayerId!.Value);
        return new GameStateDto(
            session.Id, Array.Empty<string>(), null, state.CurrentTurnPlayerId,
            session.Status == GameStatus.Finished, session.GameResult?.WinnerPlayerId,
            false, pot, opponentName, null, 0, 0, false,
            GameVariant.Yote, null, null, null, null, null, yote,
            IsDraw: session.Status == GameStatus.Finished && session.GameResult?.WinnerPlayerId == null);
    }

    internal static YoteStateDto ToDto(
        YoteGameState state,
        Guid viewerId,
        Guid creatorId,
        Guid opponentId)
    {
        var occupancy = new int[YoteRules.CellCount];
        for (var i = 0; i < YoteRules.CellCount; i++)
        {
            var owner = state.Cells[i];
            if (owner == null)
                occupancy[i] = 0;
            else if (owner == viewerId)
                occupancy[i] = 1;
            else
                occupancy[i] = 2;
        }

        var canAct = state.CurrentTurnPlayerId == viewerId;
        IReadOnlyList<int> places = Array.Empty<int>();
        IReadOnlyList<YoteEdgeDto> slides = Array.Empty<YoteEdgeDto>();
        IReadOnlyList<YoteCaptureDto> caps = Array.Empty<YoteCaptureDto>();
        if (canAct)
        {
            places = YoteRules.LegalPlaceCells(state, viewerId, creatorId);
            slides = YoteRules.LegalSlides(state, viewerId)
                .Select(s => new YoteEdgeDto(s.From, s.To))
                .ToList();
            caps = YoteRules.LegalCaptures(state, viewerId, creatorId)
                .Select(c => new YoteCaptureDto(c.From, c.To, c.Jumped))
                .ToList();
        }

        var opponentSeat = viewerId == creatorId ? opponentId : creatorId;
        return new YoteStateDto(
            YoteRules.Rows,
            YoteRules.Cols,
            YoteRules.PiecesPerPlayer,
            occupancy,
            YoteRules.InHand(state, viewerId, creatorId),
            YoteRules.InHand(state, opponentSeat, creatorId),
            places,
            slides,
            caps,
            canAct && (places.Count > 0 || slides.Count > 0 || caps.Count > 0));
    }

    private static string BuildMarker(int? from, int to, int? extra)
    {
        if (from == null)
            return YoteRules.FormatPlaceMarker(to);
        if (YoteRules.IsOrthogonalStep(from.Value, to))
            return YoteRules.FormatSlideMarker(from.Value, to);
        if (YoteRules.IsOrthogonalJump(from.Value, to, out var jumped))
            return YoteRules.FormatCaptureMarker(from.Value, to, jumped, extra);
        return $"Y:play:{from}:{to}";
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
            null, 0, 0, false, GameVariant.Yote, null, null, null, null, null, null, isDraw);

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
        var seed = sessionId.GetHashCode() ^ 0x59_4F_54_45;
        return new Random(seed).Next(2) == 0 ? creatorId : opponentId;
    }
}
