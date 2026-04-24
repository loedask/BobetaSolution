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
/// Rules: 52-card deck; 4 cards per player; must follow suit if possible; if the follower has no led suit, they win the trick;
/// same suit = higher rank wins; winner leads next round; cards removed after each trick;
/// game ends when a player has one card left and it is their turn (that player wins).
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
    private const int CardsPerPlayer = 4;
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
        var deck = BuildDeck();
        if (deck.Count != DeckSize)
            throw new InvalidOperationException($"Deck must have {DeckSize} cards.");
        Shuffle(deck, sessionId);
        var creatorHand = deck.Take(CardsPerPlayer).Select(CardToString).ToList();
        var opponentHand = deck.Skip(CardsPerPlayer).Take(CardsPerPlayer).Select(CardToString).ToList();
        var state = new MakopaGameState
        {
            CreatorHand = creatorHand,
            OpponentHand = opponentHand,
            CurrentTurnPlayerId = creatorId,
            LeadPlayerId = creatorId
        };
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

        // Must follow suit if possible (server-authoritative rule).
        var leadSuit = state.TrickSuit;
        if (leadSuit != null)
        {
            var hasLeadSuit = hand.Any(c => c.StartsWith(leadSuit, StringComparison.Ordinal));
            if (hasLeadSuit && !cardStr.StartsWith(leadSuit, StringComparison.Ordinal))
                return null;
        }

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

        Guid? nextTurn;
        if (state.TrickPlays.Count == 2)
        {
            var winner = ResolveTrick(state.TrickPlays[0], state.TrickPlays[1], state.TrickSuit!);
            state.LastTrickWinnerPlayerId = winner;
            state.LeadPlayerId = winner;
            state.CurrentTurnPlayerId = winner;
            state.TrickSuit = null;
            state.TrickPlays.Clear();
            nextTurn = winner;
            // Game ends when a player has one card left and it is their turn (winner of trick leads next and has 1 card).
            var winnerHandCount = winner == creatorId ? state.CreatorHand.Count : state.OpponentHand.Count;
            if (winnerHandCount == 1)
            {
                var (winnerId, loserId) = winner == creatorId ? (creatorId, opponentId) : (opponentId, creatorId);
                await FinalizeGameAsync(session, winnerId, loserId, cancellationToken);
                session.GameStateJson = null;
                session.Status = GameStatus.Finished;
                session.FinishedAt = DateTime.UtcNow;
            }
        }
        else
        {
            nextTurn = playerId == creatorId ? opponentId : creatorId;
            state.CurrentTurnPlayerId = nextTurn;
        }

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
                return new GameStateDto(sessionId, Array.Empty<string>(), null, null, false, null, true, lobbyPot, opponentName);
            // Finished games may clear serialized state after persist; still expose outcome for reload/history UIs.
            if (session.Status == GameStatus.Finished && session.GameResult != null)
            {
                var w = session.GameResult.WinnerPlayerId;
                return new GameStateDto(sessionId, Array.Empty<string>(), null, null, true, w, false, lobbyPot, opponentName);
            }

            return null;
        }

        var state = JsonSerializer.Deserialize<MakopaGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var myHand = playerId == creatorId ? state.CreatorHand : state.OpponentHand;
        var lastCard = state.TrickPlays.Count > 0 ? state.TrickPlays[^1].Card : null;
        var gameOver = session.Status == GameStatus.Finished;
        var winnerId = session.GameResult?.WinnerPlayerId;
        return new GameStateDto(sessionId, myHand, lastCard, state.CurrentTurnPlayerId, gameOver, winnerId, false, lobbyPot, opponentName, state.LastTrickWinnerPlayerId);
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

    /// <summary>Resolves trick: if the follower has no led suit they play any card and win the trick; if both follow led suit, higher rank wins; lead wins ties.</summary>
    private static Guid ResolveTrick(PlayedInTrick first, PlayedInTrick second, string leadSuit)
    {
        var s2 = second.Card.Split('_', 2, StringSplitOptions.None);
        var suit2 = s2[0];
        var rank2 = int.Parse(s2[1]);
        if (!string.Equals(suit2, leadSuit, StringComparison.Ordinal))
            return second.PlayerId;
        var s1 = first.Card.Split('_', 2, StringSplitOptions.None);
        var rank1 = int.Parse(s1[1]);
        return rank1 >= rank2 ? first.PlayerId : second.PlayerId;
    }

    private static List<(CardSuit Suit, CardRank Rank)> BuildDeck()
    {
        var deck = new List<(CardSuit, CardRank)>();
        foreach (CardSuit s in Enum.GetValues(typeof(CardSuit)))
            foreach (CardRank r in Enum.GetValues(typeof(CardRank)))
                deck.Add((s, r));
        return deck;
    }

    /// <summary>Deterministic Fisher-Yates shuffle seeded by sessionId so the same game always gets the same deal.</summary>
    private static void Shuffle<T>(IList<T> list, Guid sessionId)
    {
        var seed = BitConverter.ToInt32(sessionId.ToByteArray(), 0);
        var rng = new Random(seed);
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static string CardToString(CardSuit s, CardRank r) => $"{s}_{(int)r}";
    private static string CardToString((CardSuit Suit, CardRank Rank) c) => CardToString(c.Suit, c.Rank);
}
