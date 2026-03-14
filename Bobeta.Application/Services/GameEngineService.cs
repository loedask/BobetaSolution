using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Services;

public class GameEngineService(
    IGameSessionRepository sessionRepository,
    IGameMoveRepository moveRepository,
    IGameResultRepository resultRepository,
    IWalletService walletService) : IGameEngineService
{
    private const decimal CommissionRate = 0.25m;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IGameMoveRepository _moveRepository = moveRepository;
    private readonly IGameResultRepository _resultRepository = resultRepository;
    private readonly IWalletService _walletService = walletService;

    public async Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Game session not found.");
        if (session.Status != GameStatus.Waiting || session.OpponentPlayerId == null)
            throw new InvalidOperationException("Game is not ready to start.");
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId.Value;
        var deck = BuildDeck();
        Shuffle(deck);
        var creatorHand = deck.Take(4).Select(CardToString).ToList();
        var opponentHand = deck.Skip(4).Take(4).Select(CardToString).ToList();
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
        if (state.CurrentTurnPlayerId != playerId || !hand.Contains(cardStr))
            return null;

        bool mustFollowSuit = state.TrickSuit != null && hand.Any(c => c.StartsWith(state.TrickSuit!));
        if (mustFollowSuit && !cardStr.StartsWith(state.TrickSuit!))
            return null;

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
            var winner = ResolveTrick(state.TrickPlays[0], state.TrickPlays[1], state.TrickSuit);
            state.LeadPlayerId = winner;
            state.CurrentTurnPlayerId = winner;
            state.TrickSuit = null;
            state.TrickPlays.Clear();
            nextTurn = winner;
            var gameOver = state.CreatorHand.Count == 0 && state.OpponentHand.Count == 0;
            if (gameOver)
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
        if (session?.GameStateJson == null)
            return null;
        var state = JsonSerializer.Deserialize<MakopaGameState>(session.GameStateJson, JsonOptions)!;
        var creatorId = session.CreatorPlayerId;
        var opponentId = session.OpponentPlayerId;
        var myHand = playerId == creatorId ? state.CreatorHand : state.OpponentHand;
        var lastCard = state.TrickPlays.Count > 0 ? state.TrickPlays[^1].Card : null;
        var gameOver = session.Status == GameStatus.Finished;
        var winnerId = session.GameResult?.WinnerPlayerId;
        return new GameStateDto(sessionId, myHand, lastCard, state.CurrentTurnPlayerId, gameOver, winnerId);
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

    private static Guid ResolveTrick(PlayedInTrick first, PlayedInTrick second, string leadSuit)
    {
        var s1 = first.Card.Split('_');
        var s2 = second.Card.Split('_');
        var rank1 = int.Parse(s1[1]);
        var rank2 = int.Parse(s2[1]);
        var suit1 = s1[0];
        var suit2 = s2[0];
        if (suit2 != leadSuit) return first.PlayerId;
        if (suit1 != leadSuit) return second.PlayerId;
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

    private static void Shuffle<T>(IList<T> list)
    {
        var rng = new Random();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static string CardToString(CardSuit s, CardRank r) => $"{s}_{(int)r}";
    private static string CardToString((CardSuit Suit, CardRank Rank) c) => CardToString(c.Suit, c.Rank);
}
