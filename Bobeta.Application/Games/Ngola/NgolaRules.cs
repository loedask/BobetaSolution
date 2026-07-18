using Bobeta.Application.Common;

namespace Bobeta.Application.Games.Ngola;

/// <summary>Authoritative rules for Ngola's two rows of eight pits.</summary>
public static class NgolaRules
{
    public const int PitsPerPlayer = 8;
    public const int TotalPits = PitsPerPlayer * 2;
    public const int InitialSeedsPerPit = 4;

    public static NgolaGameState CreateInitial(Guid firstTurn) => new()
    {
        Pits = Enumerable.Repeat(InitialSeedsPerPit, TotalPits).ToArray(),
        CurrentTurnPlayerId = firstTurn
    };

    public static bool TryApplyMove(
        NgolaGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        int pitIndex,
        out string? errorCode)
    {
        errorCode = null;
        if (state.CurrentTurnPlayerId != playerId)
        {
            errorCode = GameMoveErrorCodes.NotYourTurn;
            return false;
        }

        if (pitIndex is < 0 or >= PitsPerPlayer)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        var canonicalPit = ToCanonicalPit(playerId, creatorId, pitIndex);
        var seeds = state.Pits[canonicalPit];
        if (seeds < 2)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        state.Pits[canonicalPit] = 0;
        var landingPit = canonicalPit;
        for (var seed = 0; seed < seeds; seed++)
        {
            landingPit = (landingPit + 1) % TotalPits;
            state.Pits[landingPit]++;
        }

        if (IsOwnedBy(landingPit, playerId == creatorId ? opponentId : creatorId, creatorId)
            && state.Pits[landingPit] > 1)
        {
            var captured = state.Pits[landingPit];
            state.Pits[landingPit] = 0;
            if (playerId == creatorId)
                state.CreatorScore += captured;
            else
                state.OpponentScore += captured;
        }

        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    public static bool HasLegalMove(NgolaGameState state, Guid playerId, Guid creatorId)
    {
        for (var localPit = 0; localPit < PitsPerPlayer; localPit++)
        {
            if (state.Pits[ToCanonicalPit(playerId, creatorId, localPit)] >= 2)
                return true;
        }

        return false;
    }

    public static (Guid? WinnerId, Guid? LoserId, bool IsDraw) CompleteIfBlocked(
        NgolaGameState state,
        Guid creatorId,
        Guid opponentId)
    {
        if (state.CurrentTurnPlayerId is not { } next || HasLegalMove(state, next, creatorId))
            return (null, null, false);

        state.CreatorScore += state.Pits.Take(PitsPerPlayer).Sum();
        state.OpponentScore += state.Pits.Skip(PitsPerPlayer).Sum();
        Array.Clear(state.Pits);

        if (state.CreatorScore == state.OpponentScore)
            return (null, null, true);
        return state.CreatorScore > state.OpponentScore
            ? (creatorId, opponentId, false)
            : (opponentId, creatorId, false);
    }

    public static int[] PitsForViewer(NgolaGameState state, Guid viewerId, Guid creatorId, bool ownRow)
    {
        var viewerIsCreator = viewerId == creatorId;
        var canonicalOwnerIsCreator = ownRow == viewerIsCreator;
        var source = canonicalOwnerIsCreator
            ? state.Pits.Take(PitsPerPlayer)
            : state.Pits.Skip(PitsPerPlayer);
        return (canonicalOwnerIsCreator ? source : source.Reverse()).ToArray();
    }

    private static int ToCanonicalPit(Guid playerId, Guid creatorId, int localPit) =>
        playerId == creatorId ? localPit : TotalPits - 1 - localPit;

    private static bool IsOwnedBy(int canonicalPit, Guid playerId, Guid creatorId) =>
        (canonicalPit < PitsPerPlayer) == (playerId == creatorId);
}
