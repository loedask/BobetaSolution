using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Services;

/// <summary>
/// Makopa card game engine. Server-authoritative and deterministic.
/// Rules: 52-card deck; 6 cards each; best-of-3 hands per match (first to 2 hand wins settles the stakes);
/// follow suit if possible; if follower cannot match suit they may discard and the trick goes to whoever played the highest card of the led suit (discard cannot win);
/// ties on rank go to the lead; shuffle is seeded per hand for fairness; random first leader each hand via session/hand index seed.
/// </summary>
public class GameEngineService(
    IGameSessionRepository sessionRepository,
    IGameMoveRepository moveRepository,
    IGameResultRepository resultRepository,
    IWalletService walletService,
    IPlayerRepository playerRepository) : IGameEngineService
{
    private const decimal CommissionRate = 0.25m;
    private const int DeckSize = 52;
    private const int CardsPerPlayer = 6;
    private const int RoundsToWinMatch = 2;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IGameMoveRepository _moveRepository = moveRepository;
    private readonly IGameResultRepository _resultRepository = resultRepository;
    private readonly IWalletService _walletService = walletService;
    private readonly IPlayerRepository _playerRepository = playerRepository;

    public async Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Game session not found.");
        if (session.Status != GameStatus.Waiting || session.OpponentPlayerId == null)
            throw new InvalidOperationException("Game is not ready to start.");
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId.Value;
        var state = new MakopaGameState
        {
            CreatorRoundWins = 0,
            OpponentRoundWins = 0
        };
        BeginHand(state, creatorId, opponentId, sessionId, completedHandsPrior: 0);
        session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);
        session.Status = GameStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    /// <summary>Server-authoritative play: validates turn, card in hand, and follow-suit rule before applying move.</summary>
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

        // Drop prior trick result once a new trick begins (first card on an empty trick).
        if (state.TrickPlays.Count == 0)
            state.LastTrickWinnerPlayerId = null;

        hand.Remove(cardStr);
        state.TrickPlays.Add(new PlayedInTrick { PlayerId = playerId, Card = cardStr });
        state.TrickSuit ??= cardStr.Split('_')[0];

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
            var winner = ResolveTrick(state.TrickPlays[0], state.TrickPlays[1], state.TrickSuit!);
            state.LastTrickWinnerPlayerId = winner;
            state.LeadPlayerId = winner;
            state.CurrentTurnPlayerId = winner;
            state.TrickSuit = null;
            state.TrickPlays.Clear();
            var winnerHandCount = winner == creatorId ? state.CreatorHand.Count : state.OpponentHand.Count;

            // Hand ends when trick winner emptied their hand; match ends at first-to-2 hand wins.
            if (winnerHandCount == 0)
            {
                if (winner == creatorId)
                    state.CreatorRoundWins++;
                else
                    state.OpponentRoundWins++;

                if (state.CreatorRoundWins >= RoundsToWinMatch || state.OpponentRoundWins >= RoundsToWinMatch)
                {
                    var (winnerId, loserId) = state.CreatorRoundWins >= RoundsToWinMatch ? (creatorId, opponentId) : (opponentId, creatorId);
                    await FinalizeGameAsync(session, winnerId, loserId, cancellationToken);
                    session.GameStateJson = null;
                    session.Status = GameStatus.Finished;
                    session.FinishedAt = DateTime.UtcNow;
                }
                else
                {
                    BeginHand(state, creatorId, opponentId, sessionId, state.CreatorRoundWins + state.OpponentRoundWins);
                }
            }
        }
        else
            state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;

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

        var (myRounds, opponentRounds) = playerId == creatorId
            ? (state.CreatorRoundWins, state.OpponentRoundWins)
            : (state.OpponentRoundWins, state.CreatorRoundWins);

        return new GameStateDto(sessionId, myHand, lastCard, state.CurrentTurnPlayerId, gameOver, winnerId, false, lobbyPot, opponentName, state.LastTrickWinnerPlayerId, myRounds, opponentRounds);
    }

    /// <summary>Deals cards and assigns first leader deterministically.</summary>
    private static void BeginHand(MakopaGameState state, Guid creatorId, Guid opponentId, Guid sessionId, int completedHandsPrior)
    {
        var deck = BuildDeck();
        if (deck.Count != DeckSize)
            throw new InvalidOperationException($"Deck must have {DeckSize} cards.");
        Shuffle(deck, sessionId, completedHandsPrior);
        state.CreatorHand = deck.Take(CardsPerPlayer).Select(CardToString).ToList();
        state.OpponentHand = deck.Skip(CardsPerPlayer).Take(CardsPerPlayer).Select(CardToString).ToList();
        state.TrickPlays = new List<PlayedInTrick>();
        state.TrickSuit = null;
        state.LastTrickWinnerPlayerId = null;
        var firstLeader = PickFirstLeader(sessionId, completedHandsPrior, creatorId, opponentId);
        state.LeadPlayerId = firstLeader;
        state.CurrentTurnPlayerId = firstLeader;
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

    /// <summary>Highest ranking card played of the led suit wins; follower who lacks the suit counts as lowest (effective rank 0 on lead).</summary>
    private static Guid ResolveTrick(PlayedInTrick first, PlayedInTrick second, string leadSuit)
    {
        var s1 = first.Card.Split('_', 2, StringSplitOptions.None);
        var s2 = second.Card.Split('_', 2, StringSplitOptions.None);
        var r1OnLead = GetEffectiveRankOfPlay(s1, leadSuit);
        var r2OnLead = GetEffectiveRankOfPlay(s2, leadSuit);
        return r1OnLead >= r2OnLead ? first.PlayerId : second.PlayerId;
    }

    private static int GetEffectiveRankOfPlay(string[] suitRankParts, string leadSuit)
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

    private static int MixSeed(Guid sessionId, int handSequence)
    {
        Span<byte> buf = stackalloc byte[16];
        sessionId.TryWriteBytes(buf);
        var hi = BitConverter.ToInt32(buf[..4]);
        var lo = BitConverter.ToInt32(buf.Slice(4, 4));
        unchecked
        {
            return hi ^ lo ^ handSequence * 0x5851F42D ^ (handSequence + 41) * 0x27D4EB4D;
        }
    }

    private static void Shuffle<T>(IList<T> list, Guid sessionId, int handSequence)
    {
        var seed = MixSeed(sessionId, handSequence);
        var rng = new Random(seed);
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static Guid PickFirstLeader(Guid sessionId, int handSequence, Guid creatorId, Guid opponentId)
        => new Random(MixSeed(sessionId, unchecked(handSequence * 31 + 7))).Next(2) == 0 ? creatorId : opponentId;

    private static string CardToString(CardSuit s, CardRank r) => $"{s}_{(int)r}";
    private static string CardToString((CardSuit Suit, CardRank Rank) c) => CardToString(c.Suit, c.Rank);
}
