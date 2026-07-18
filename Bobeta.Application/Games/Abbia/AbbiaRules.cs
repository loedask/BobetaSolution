using Bobeta.Application.Common;

namespace Bobeta.Application.Games.Abbia;

/// <summary>Authoritative rules for simplified 1v1 Abbia (token-flip chance game).</summary>
public static class AbbiaRules
{
    public const int TokenCount = 5;
    public const string ActionThrow = "throw";

    public static AbbiaGameState CreateInitial(Guid sessionId, Guid creatorId, Guid opponentId)
    {
        var first = PickFirstTurn(sessionId, creatorId, opponentId);
        return new AbbiaGameState { CurrentTurnPlayerId = first };
    }

    public static bool TryApplyThrow(
        AbbiaGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        Guid sessionId,
        int moveOrder,
        out string? errorCode)
    {
        errorCode = null;
        if (state.CurrentTurnPlayerId != playerId)
        {
            errorCode = GameMoveErrorCodes.NotYourTurn;
            return false;
        }

        var isCreator = playerId == creatorId;
        if (isCreator ? state.CreatorThrown : state.OpponentThrown)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        var tokens = FlipTokens(sessionId, playerId, moveOrder);
        if (isCreator)
        {
            state.CreatorTokens = tokens;
            state.CreatorThrown = true;
        }
        else
        {
            state.OpponentTokens = tokens;
            state.OpponentThrown = true;
        }

        if (state.CreatorThrown && state.OpponentThrown)
        {
            state.CurrentTurnPlayerId = null;
            return true;
        }

        state.CurrentTurnPlayerId = isCreator ? opponentId : creatorId;
        return true;
    }

    /// <summary>
    /// After both seats have thrown, higher carved-up count wins. Equal counts are a draw.
    /// </summary>
    public static (Guid? WinnerId, Guid? LoserId, bool IsDraw) Evaluate(
        AbbiaGameState state,
        Guid creatorId,
        Guid opponentId)
    {
        if (!state.CreatorThrown || !state.OpponentThrown)
            return (null, null, false);

        var creatorScore = CarvedUpCount(state.CreatorTokens);
        var opponentScore = CarvedUpCount(state.OpponentTokens);
        if (creatorScore == opponentScore)
            return (null, null, true);
        return creatorScore > opponentScore
            ? (creatorId, opponentId, false)
            : (opponentId, creatorId, false);
    }

    public static int CarvedUpCount(IEnumerable<bool> tokens) => tokens.Count(t => t);

    public static string FormatMoveMarker(int carvedUp) => $"A:throw:{carvedUp}";

    public static List<bool> FlipTokens(Guid sessionId, Guid playerId, int moveOrder)
    {
        var seed = HashCode.Combine(sessionId, playerId, moveOrder, 0x41_42_42_49);
        var rng = new Random(seed);
        var tokens = new List<bool>(TokenCount);
        for (var i = 0; i < TokenCount; i++)
            tokens.Add(rng.Next(2) == 1);
        return tokens;
    }

    private static Guid PickFirstTurn(Guid sessionId, Guid creatorId, Guid opponentId)
    {
        var rng = new Random(sessionId.GetHashCode() ^ 0x41_42_42_49);
        return rng.Next(2) == 0 ? creatorId : opponentId;
    }
}
