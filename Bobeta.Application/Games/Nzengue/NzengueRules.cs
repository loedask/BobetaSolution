using Bobeta.Application.Common;

namespace Bobeta.Application.Games.Nzengue;

/// <summary>
/// Bobeta house rules for Nzengué: Congo alignment game on the documented 9-point board
/// (square, midpoints, center, diagonals), with a move phase after both seats place three stones.
/// </summary>
public static class NzengueRules
{
    public const int PointCount = 9;
    public const int PiecesPerPlayer = 3;
    public const string PhasePlace = "place";
    public const string PhaseMove = "move";

    /// <summary>
    /// Board indices:
    /// 0 1 2
    /// 3 4 5
    /// 6 7 8
    /// </summary>
    public static readonly int[][] Lines =
    [
        [0, 1, 2],
        [3, 4, 5],
        [6, 7, 8],
        [0, 3, 6],
        [1, 4, 7],
        [2, 5, 8],
        [0, 4, 8],
        [2, 4, 6]
    ];

    private static readonly int[][] Adjacent =
    [
        [1, 3, 4],       // 0
        [0, 2, 4],       // 1
        [1, 4, 5],       // 2
        [0, 4, 6],       // 3
        [0, 1, 2, 3, 5, 6, 7, 8], // 4 center
        [2, 4, 8],       // 5
        [3, 4, 7],       // 6
        [4, 6, 8],       // 7
        [4, 5, 7]        // 8
    ];

    public static NzengueGameState CreateInitial(Guid firstTurn) => new()
    {
        CurrentTurnPlayerId = firstTurn
    };

    public static string PhaseOf(NzengueGameState state) =>
        state.CreatorPiecesToPlace > 0 || state.OpponentPiecesToPlace > 0
            ? PhasePlace
            : PhaseMove;

    public static bool TryApplyPlace(
        NzengueGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        int toPoint,
        out string? errorCode)
    {
        errorCode = null;
        if (PhaseOf(state) != PhasePlace)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        if (state.CurrentTurnPlayerId != playerId)
        {
            errorCode = GameMoveErrorCodes.NotYourTurn;
            return false;
        }

        if (toPoint is < 0 or >= PointCount || state.Points[toPoint] != null)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        var remaining = RemainingToPlace(state, playerId, creatorId);
        if (remaining <= 0)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        state.Points[toPoint] = playerId;
        if (playerId == creatorId)
            state.CreatorPiecesToPlace--;
        else
            state.OpponentPiecesToPlace--;

        if (HasLine(state, playerId))
        {
            state.CurrentTurnPlayerId = null;
            return true;
        }

        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    public static bool TryApplyMove(
        NzengueGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        int fromPoint,
        int toPoint,
        out string? errorCode)
    {
        errorCode = null;
        if (PhaseOf(state) != PhaseMove)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        if (state.CurrentTurnPlayerId != playerId)
        {
            errorCode = GameMoveErrorCodes.NotYourTurn;
            return false;
        }

        if (fromPoint is < 0 or >= PointCount
            || toPoint is < 0 or >= PointCount
            || state.Points[fromPoint] != playerId
            || state.Points[toPoint] != null
            || !AreAdjacent(fromPoint, toPoint))
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        state.Points[fromPoint] = null;
        state.Points[toPoint] = playerId;

        if (HasLine(state, playerId))
        {
            state.CurrentTurnPlayerId = null;
            return true;
        }

        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    public static (Guid? WinnerId, Guid? LoserId, bool IsDraw) EvaluateAfterMove(
        NzengueGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid lastMoverId)
    {
        if (HasLine(state, lastMoverId))
        {
            var loser = lastMoverId == creatorId ? opponentId : creatorId;
            return (lastMoverId, loser, false);
        }

        if (PhaseOf(state) == PhaseMove
            && state.CurrentTurnPlayerId is { } next
            && !HasLegalMove(state, next))
        {
            // Current seat cannot move: treat as a draw (rare with 3 stones on 9 points).
            return (null, null, true);
        }

        return (null, null, false);
    }

    public static bool HasLine(NzengueGameState state, Guid playerId)
    {
        foreach (var line in Lines)
        {
            if (state.Points[line[0]] == playerId
                && state.Points[line[1]] == playerId
                && state.Points[line[2]] == playerId)
                return true;
        }

        return false;
    }

    public static bool HasLegalMove(NzengueGameState state, Guid playerId)
    {
        for (var from = 0; from < PointCount; from++)
        {
            if (state.Points[from] != playerId)
                continue;
            foreach (var to in Adjacent[from])
            {
                if (state.Points[to] == null)
                    return true;
            }
        }

        return false;
    }

    public static bool AreAdjacent(int from, int to) =>
        from is >= 0 and < PointCount && Adjacent[from].Contains(to);

    public static IReadOnlyList<int> LegalPlacePoints(NzengueGameState state)
    {
        var list = new List<int>();
        for (var i = 0; i < PointCount; i++)
        {
            if (state.Points[i] == null)
                list.Add(i);
        }

        return list;
    }

    public static IReadOnlyList<(int From, int To)> LegalMoves(NzengueGameState state, Guid playerId)
    {
        var list = new List<(int From, int To)>();
        for (var from = 0; from < PointCount; from++)
        {
            if (state.Points[from] != playerId)
                continue;
            foreach (var to in Adjacent[from])
            {
                if (state.Points[to] == null)
                    list.Add((from, to));
            }
        }

        return list;
    }

    public static string FormatPlaceMarker(int toPoint) => $"N:place:{toPoint}";

    public static string FormatMoveMarker(int fromPoint, int toPoint) => $"N:move:{fromPoint}:{toPoint}";

    private static int RemainingToPlace(NzengueGameState state, Guid playerId, Guid creatorId) =>
        playerId == creatorId ? state.CreatorPiecesToPlace : state.OpponentPiecesToPlace;
}
