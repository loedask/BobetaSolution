using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Bobeta.Application.Services;

/// <summary>
/// Makopa (authoritative rules): 2 players, 4 cards each from a shuffled 52-card deck; remaining cards are unused (no drawing ever).
/// Trick = lead + response. Follow suit when possible; void → Take only (lead card to responder's hand; leader leads again; Take is not a card play).
/// Completed trick: compare led suit only; higher rank wins; ties → leader wins; both cards leave play; winner leads next.
/// Win (strict): player wins instantly when they have exactly one card and they are the current leader (their turn).
/// Instant loss (before trick resolution): responder plays a suit matching the other player's only card while that player holds exactly one card.
/// </summary>
public class GameEngineService(
    IGameSessionRepository sessionRepository,
    IGameMoveRepository moveRepository,
    IGameResultRepository resultRepository,
    IWalletService walletService,
    IPlayerRepository playerRepository,
    ILogger<GameEngineService> logger) : IGameEngineService
{
    private const decimal CommissionRate = 0.25m;
    private const int DeckSize = 52;
    private const int CardsPerPlayer = 4;

    internal const string VoidFollowMoveMarker = "VoidFollow_Draw";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IGameMoveRepository _moveRepository = moveRepository;
    private readonly IGameResultRepository _resultRepository = resultRepository;
    private readonly IWalletService _walletService = walletService;
    private readonly IPlayerRepository _playerRepository = playerRepository;
    private readonly ILogger<GameEngineService> _logger = logger;

    public async Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Game session not found.");
        if (session.Status != GameStatus.Waiting || session.OpponentPlayerId == null)
            throw new InvalidOperationException("Game is not ready to start.");
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId.Value;
        var deck = BuildDeck();
        if (deck.Count != DeckSize)
            throw new InvalidOperationException($"Deck must have {DeckSize} cards.");
        Shuffle(deck, sessionId, 0);
        var creatorHand = deck.Take(CardsPerPlayer).Select(c => CardToString(c.Suit, c.Rank)).ToList();
        var opponentHand = deck.Skip(CardsPerPlayer).Take(CardsPerPlayer).Select(c => CardToString(c.Suit, c.Rank)).ToList();
        // Remaining cards are never used — Makopa does not draw from a stock.
        var state = new MakopaGameState
        {
            CreatorHand = creatorHand,
            OpponentHand = opponentHand,
            TrickPlays = new List<PlayedInTrick>(),
            TrickSuit = null,
            LastTrickWinnerPlayerId = null,
            CreatorRoundWins = 0,
            OpponentRoundWins = 0
        };
        var firstLeader = PickFirstLeader(sessionId, creatorId, opponentId);
        state.LeadPlayerId = firstLeader;
        state.CurrentTurnPlayerId = firstLeader;
        session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);
        session.Status = GameStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    public async Task<GameStateDto?> VoidFollowDrawAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.GameStateJson == null || session.Status != GameStatus.InProgress)
            return null;
        var state = JsonSerializer.Deserialize<MakopaGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId!.Value;
        // Take is not playing a card; turn stays with leader after this (they lead again).
        if (state.CurrentTurnPlayerId != playerId)
            return null;
        if (state.TrickPlays.Count != 1 || state.TrickSuit == null)
            return null;

        var lead = state.TrickPlays[0];
        var leaderId = lead.PlayerId;
        if (playerId == leaderId)
            return null;

        var responderHand = playerId == creatorId ? state.CreatorHand : state.OpponentHand;
        if (MakopaRules.HandContainsLedSuit(state.TrickSuit, responderHand))
            return null;

        responderHand.Add(lead.Card);
        state.TrickPlays.Clear();
        state.TrickSuit = null;
        state.LastTrickWinnerPlayerId = null;

        state.LeadPlayerId = leaderId;
        state.CurrentTurnPlayerId = leaderId;

        var moveOrder = await _moveRepository.GetCountByGameSessionIdAsync(sessionId, cancellationToken);
        await _moveRepository.AddAsync(new GameMove
        {
            Id = Guid.NewGuid(),
            GameSessionId = sessionId,
            PlayerId = playerId,
            CardSuitRank = VoidFollowMoveMarker,
            MoveOrder = moveOrder,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await TryFinalizeLeaderWinAsync(
            session, state, creatorId, opponentId, cancellationToken,
            context: "void-follow", logTrickEndSnapshot: true);

        session.GameStateJson = session.Status == GameStatus.Finished
            ? null
            : JsonSerializer.Serialize(state, JsonOptions);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return await GetGameStateAsync(playerId, sessionId, cancellationToken);
    }

    public async Task<GameStateDto?> PlayCardAsync(Guid playerId, Guid sessionId, Card card, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session?.GameStateJson == null || session.Status != GameStatus.InProgress)
            return null;
        var state = JsonSerializer.Deserialize<MakopaGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId!.Value;
        var hand = playerId == creatorId ? state.CreatorHand : state.OpponentHand;
        var cardStr = CardToString(card.Suit, card.Rank);

        if (state.CurrentTurnPlayerId != playerId) return null;
        if (!hand.Contains(cardStr)) return null;

        if (!MakopaRules.IsLegalFollowSuit(cardStr, state.TrickSuit, hand))
            return null;

        // Void: responder has no card in the led suit — must use Take (VoidFollow), not play a card.
        if (state.TrickPlays.Count == 1
            && !string.IsNullOrEmpty(state.TrickSuit)
            && !MakopaRules.HandContainsLedSuit(state.TrickSuit!, hand))
            return null;

        // Instant loss (evaluated before trick resolution): singleton holder wins if responder matches their suit.
        if (state.TrickPlays.Count == 1 && TryInstantLossSingletonSuit(
                playerId, creatorId, opponentId, state, cardStr,
                out var penaltyWinnerId, out var penaltyLoserId))
        {
            hand.Remove(cardStr);
            var mo = await _moveRepository.GetCountByGameSessionIdAsync(sessionId, cancellationToken);
            await _moveRepository.AddAsync(new GameMove
            {
                Id = Guid.NewGuid(),
                GameSessionId = sessionId,
                PlayerId = playerId,
                CardSuitRank = cardStr,
                MoveOrder = mo,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            ReturnPendingLeadCardToLeaderHand(state.TrickPlays, creatorId, state);
            state.TrickPlays.Clear();
            state.TrickSuit = null;
            state.LastTrickWinnerPlayerId = null;
            await FinalizeGameAsync(session, penaltyWinnerId, penaltyLoserId, cancellationToken);
            session.GameStateJson = null;
            session.Status = GameStatus.Finished;
            session.FinishedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            return await GetGameStateAsync(playerId, sessionId, cancellationToken);
        }

        if (state.TrickPlays.Count == 0)
            state.LastTrickWinnerPlayerId = null;

        hand.Remove(cardStr);
        state.TrickPlays.Add(new PlayedInTrick { PlayerId = playerId, Card = cardStr });
        state.TrickSuit ??= cardStr.Split('_')[0];
        if (state.TrickPlays.Count == 1)
            state.LeadPlayerId = playerId;

        var moveOrder = await _moveRepository.GetCountByGameSessionIdAsync(sessionId, cancellationToken);
        await _moveRepository.AddAsync(new GameMove
        {
            Id = Guid.NewGuid(),
            GameSessionId = sessionId,
            PlayerId = playerId,
            CardSuitRank = cardStr,
            MoveOrder = moveOrder,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        if (state.TrickPlays.Count == 2)
        {
            // Completed trick only: both cards leave hands; compare led suit; tie → leader (first play) wins.
            var winner = ResolveTrick(state.TrickPlays[0], state.TrickPlays[1], state.TrickSuit!);
            state.LastTrickWinnerPlayerId = winner;
            state.TrickPlays.Clear();
            state.TrickSuit = null;

            state.LeadPlayerId = winner;
            state.CurrentTurnPlayerId = winner;

            await TryFinalizeLeaderWinAsync(
                session, state, creatorId, opponentId, cancellationToken,
                context: "trick-resolved", logTrickEndSnapshot: true);
        }
        else
        {
            // After lead only: switch turn to responder (no switch after Take — that path never reaches PlayCard).
            state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
            await TryFinalizeLeaderWinAsync(
                session, state, creatorId, opponentId, cancellationToken,
                context: "lead-card-played");
        }

        if (session.Status == GameStatus.Finished)
            session.GameStateJson = null;
        else
            session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return await GetGameStateAsync(playerId, sessionId, cancellationToken);
    }

    public async Task<GameStateDto?> GetGameStateAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
            return null;

        var isParticipant = playerId == session.CreatorPlayerId
            || (session.OpponentPlayerId.HasValue && session.OpponentPlayerId.Value == playerId);
        if (!isParticipant)
            return null;

        var lobbyPot = session.BetAmount * 2m;
        var opponentName = await ResolveOpponentDisplayNameAsync(playerId, session, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(session.GameStateJson))
        {
            if (session.Status == GameStatus.Waiting)
                return new GameStateDto(sessionId, Array.Empty<string>(), null, null, false, null, true, lobbyPot, opponentName, null, 0, 0);
            if (session.Status == GameStatus.Finished && session.GameResult != null)
            {
                var w = session.GameResult.WinnerPlayerId;
                return new GameStateDto(sessionId, Array.Empty<string>(), null, null, true, w, false, lobbyPot, opponentName, null, 0, 0);
            }

            return null;
        }

        var state = JsonSerializer.Deserialize<MakopaGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var myHand = playerId == creatorId ? state.CreatorHand : state.OpponentHand;
        var lastCard = state.TrickPlays.Count > 0 ? state.TrickPlays[^1].Card : null;
        var gameOver = session.Status == GameStatus.Finished;
        var winnerId = session.GameResult?.WinnerPlayerId;

        var (myRounds, opponentRounds) = (0, 0);

        var mustFollowLedSuit = state.TrickPlays.Count == 1
            && state.TrickPlays[0].PlayerId != playerId
            && state.CurrentTurnPlayerId == playerId;

        return new GameStateDto(sessionId, myHand, lastCard, state.CurrentTurnPlayerId, gameOver, winnerId, false, lobbyPot, opponentName, state.LastTrickWinnerPlayerId, myRounds, opponentRounds, mustFollowLedSuit);
    }

    /// <summary>Instant loss: exactly one player has a singleton; they are not the responder; responder&apos;s card suit matches that singleton.</summary>
    private static bool TryInstantLossSingletonSuit(
        Guid responderId,
        Guid creatorId,
        Guid opponentId,
        MakopaGameState state,
        string responderCardStr,
        out Guid winnerPid,
        out Guid loserPid)
    {
        winnerPid = loserPid = Guid.Empty;
        Guid? soloOwnerId = null;
        string soloSuit = "";
        foreach (var (pid, cards) in new[] { (creatorId, state.CreatorHand), (opponentId, state.OpponentHand) })
        {
            if (cards.Count != 1)
                continue;
            if (soloOwnerId.HasValue)
                return false;
            soloOwnerId = pid;
            soloSuit = SuitPrefix(cards[0]) ?? "";
            if (string.IsNullOrEmpty(soloSuit))
                return false;
        }

        if (soloOwnerId == null || soloOwnerId == responderId)
            return false;
        var playedSuit = SuitPrefix(responderCardStr);
        if (!string.Equals(playedSuit, soloSuit, StringComparison.Ordinal))
            return false;
        winnerPid = soloOwnerId.Value;
        loserPid = responderId;
        return true;
    }

    private static string? SuitPrefix(string card)
    {
        var sep = card.IndexOf('_');
        return sep <= 0 ? null : card[..sep];
    }

    private static void ReturnPendingLeadCardToLeaderHand(List<PlayedInTrick> trickPlays, Guid creatorId, MakopaGameState state)
    {
        if (trickPlays.Count < 1)
            return;
        var pending = trickPlays[0];
        var lh = pending.PlayerId == creatorId ? state.CreatorHand : state.OpponentHand;
        lh.Add(pending.Card);
    }

    private async Task<string?> ResolveOpponentDisplayNameAsync(Guid viewerPlayerId, GameSession session, CancellationToken cancellationToken)
    {
        Guid? opponentId = viewerPlayerId == session.CreatorPlayerId
            ? session.OpponentPlayerId
            : session.OpponentPlayerId.HasValue && session.OpponentPlayerId.Value == viewerPlayerId
                ? session.CreatorPlayerId
                : null;
        if (opponentId == null)
            return null;
        var opponent = await _playerRepository.GetByIdAsync(opponentId.Value, cancellationToken).ConfigureAwait(false);
        var name = opponent?.PlayerName?.Trim();
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private async Task FinalizeGameAsync(GameSession session, Guid winnerId, Guid loserId, CancellationToken cancellationToken)
    {
        var totalPot = session.BetAmount * 2;
        var commission = totalPot * CommissionRate;
        var winnerAmount = totalPot - commission;
        await _walletService.SettleGameAsync(winnerId, loserId, session.BetAmount, cancellationToken);
        await _resultRepository.AddAsync(new GameResult
        {
            Id = Guid.NewGuid(),
            GameSessionId = session.Id,
            WinnerPlayerId = winnerId,
            LoserPlayerId = loserId,
            TotalPot = totalPot,
            WinnerAmount = winnerAmount,
            PlatformCommission = commission,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    /// <summary>
    /// Strict win condition (engine-authoritative): leader wins instantly when they have exactly one card and it is their turn.
    /// Must be called after every state-changing action and after leader/turn assignments are finalized.
    /// </summary>
    private async Task TryFinalizeLeaderWinAsync(
        GameSession session,
        MakopaGameState state,
        Guid creatorId,
        Guid opponentId,
        CancellationToken cancellationToken,
        string context,
        bool logTrickEndSnapshot = false)
    {
        if (session.Status == GameStatus.Finished)
            return;

        var leaderId = state.LeadPlayerId;
        var currentTurn = state.CurrentTurnPlayerId;
        if (!leaderId.HasValue || !currentTurn.HasValue || leaderId.Value != currentTurn.Value)
            return;

        var leaderIsCreator = leaderId.Value == creatorId;
        var leaderHandCount = leaderIsCreator ? state.CreatorHand.Count : state.OpponentHand.Count;
        var opponentHandCount = leaderIsCreator ? state.OpponentHand.Count : state.CreatorHand.Count;
        var triggered = leaderHandCount == 1;

        if (logTrickEndSnapshot)
        {
            _logger.LogInformation(
                "Makopa trick end | context={Context} | leader={LeaderId} | leaderHand={LeaderHandCount} | opponentHand={OpponentHandCount} | winTriggered={WinTriggered}",
                context, leaderId.Value, leaderHandCount, opponentHandCount, triggered);
        }

        if (!triggered)
            return;

        var loserId = leaderIsCreator ? opponentId : creatorId;
        await FinalizeGameAsync(session, leaderId.Value, loserId, cancellationToken);
        session.GameStateJson = null;
        session.Status = GameStatus.Finished;
        session.FinishedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Makopa win finalized immediately | context={Context} | winner={WinnerId} | loser={LoserId} | winnerHand={WinnerHandCount}",
            context, leaderId.Value, loserId, leaderHandCount);
    }

    /// <summary>Winning seat: higher rank on <paramref name="leadSuit"/>; equal ranks → leader (<paramref name="first"/>).</summary>
    private static Guid ResolveTrick(PlayedInTrick first, PlayedInTrick second, string leadSuit)
    {
        var s1 = first.Card.Split('_', 2, StringSplitOptions.None);
        var s2 = second.Card.Split('_', 2, StringSplitOptions.None);
        var r1 = RankOnLeadCard(s1, leadSuit);
        var r2 = RankOnLeadCard(s2, leadSuit);
        return r1 >= r2 ? first.PlayerId : second.PlayerId;
    }

    private static int RankOnLeadCard(string[] suitRankParts, string leadSuit)
    {
        if (suitRankParts.Length < 2)
            return 0;
        if (!string.Equals(suitRankParts[0], leadSuit, StringComparison.Ordinal))
            return 0;
        return int.TryParse(suitRankParts[1], out var n) ? n : 0;
    }

    private static List<(CardSuit Suit, CardRank Rank)> BuildDeck()
    {
        var deck = new List<(CardSuit, CardRank)>();
        foreach (CardSuit s in Enum.GetValues(typeof(CardSuit)))
            foreach (CardRank r in Enum.GetValues(typeof(CardRank)))
                deck.Add((s, r));
        return deck;
    }

    private static int MixSeed(Guid sessionId, int salt)
    {
        Span<byte> buf = stackalloc byte[16];
        sessionId.TryWriteBytes(buf);
        var hi = BitConverter.ToInt32(buf[..4]);
        var lo = BitConverter.ToInt32(buf.Slice(4, 4));
        unchecked
        {
            return hi ^ lo ^ salt * 0x5851F42D ^ (salt + 41) * 0x27D4EB4D;
        }
    }

    private static void Shuffle<T>(IList<T> list, Guid sessionId, int salt)
    {
        var rng = new Random(MixSeed(sessionId, salt));
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static Guid PickFirstLeader(Guid sessionId, Guid creatorId, Guid opponentId)
        => new Random(MixSeed(sessionId, 7)).Next(2) == 0 ? creatorId : opponentId;

    private static string CardToString(CardSuit s, CardRank r) => $"{s}_{(int)r}";
}
