using Bobeta.Application.Common;

namespace Bobeta.Application.Games.Yote;

/// <summary>
/// Bobeta house rules for Yoté: West African 5×6 capture game.
/// Place from hand, slide orthogonally, or jump-capture (jumped piece plus one chosen extra).
/// </summary>
public static class YoteRules
{
    public const int Rows = 5;
    public const int Cols = 6;
    public const int CellCount = Rows * Cols;
    public const int PiecesPerPlayer = 12;
    public const int DrawBoardThreshold = 3;

    private static readonly (int Dr, int Dc)[] Ortho = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    public static YoteGameState CreateInitial(Guid firstTurn) => new()
    {
        CurrentTurnPlayerId = firstTurn
    };

    public static int Index(int row, int col) => row * Cols + col;

    public static (int Row, int Col) Coord(int index) => (index / Cols, index % Cols);

    public static bool TryApply(
        YoteGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        int? fromCell,
        int toCell,
        int? extraRemoveCell,
        out string? errorCode)
    {
        errorCode = null;
        if (state.CurrentTurnPlayerId != playerId)
        {
            errorCode = GameMoveErrorCodes.NotYourTurn;
            return false;
        }

        if (toCell is < 0 or >= CellCount)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        if (fromCell == null)
            return TryPlace(state, creatorId, opponentId, playerId, toCell, out errorCode);

        if (fromCell is < 0 or >= CellCount)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        if (IsOrthogonalStep(fromCell.Value, toCell))
            return TrySlide(state, creatorId, opponentId, playerId, fromCell.Value, toCell, out errorCode);

        if (IsOrthogonalJump(fromCell.Value, toCell, out var jumped))
            return TryCapture(
                state, creatorId, opponentId, playerId, fromCell.Value, toCell, jumped, extraRemoveCell, out errorCode);

        errorCode = GameMoveErrorCodes.InvalidMove;
        return false;
    }

    public static (Guid? WinnerId, Guid? LoserId, bool IsDraw) EvaluateAfterMove(
        YoteGameState state,
        Guid creatorId,
        Guid opponentId)
    {
        var creatorTotal = TotalPieces(state, creatorId, creatorId);
        var opponentTotal = TotalPieces(state, opponentId, creatorId);

        if (opponentTotal == 0)
            return (creatorId, opponentId, false);
        if (creatorTotal == 0)
            return (opponentId, creatorId, false);

        if (creatorTotal <= DrawBoardThreshold && opponentTotal <= DrawBoardThreshold)
            return (null, null, true);

        if (state.CurrentTurnPlayerId is { } next && !HasAnyLegalAction(state, next, creatorId))
        {
            var nextTotal = TotalPieces(state, next, creatorId);
            var other = next == creatorId ? opponentId : creatorId;
            var otherTotal = TotalPieces(state, other, creatorId);
            if (otherTotal == nextTotal)
                return (null, null, true);
            return otherTotal > nextTotal ? (other, next, false) : (next, other, false);
        }

        return (null, null, false);
    }

    public static int PiecesOnBoard(YoteGameState state, Guid playerId) =>
        state.Cells.Count(c => c == playerId);

    public static int InHand(YoteGameState state, Guid playerId, Guid creatorId) =>
        playerId == creatorId ? state.CreatorInHand : state.OpponentInHand;

    public static int TotalPieces(YoteGameState state, Guid playerId, Guid creatorId) =>
        PiecesOnBoard(state, playerId) + InHand(state, playerId, creatorId);

    public static bool HasAnyLegalAction(YoteGameState state, Guid playerId, Guid creatorId) =>
        LegalPlaceCells(state, playerId, creatorId).Count > 0
        || LegalSlides(state, playerId).Count > 0
        || LegalCaptures(state, playerId, creatorId).Count > 0;

    public static IReadOnlyList<int> LegalPlaceCells(YoteGameState state, Guid playerId, Guid creatorId)
    {
        if (InHand(state, playerId, creatorId) <= 0)
            return Array.Empty<int>();
        var list = new List<int>();
        for (var i = 0; i < CellCount; i++)
        {
            if (state.Cells[i] == null)
                list.Add(i);
        }

        return list;
    }

    public static IReadOnlyList<(int From, int To)> LegalSlides(YoteGameState state, Guid playerId)
    {
        var list = new List<(int From, int To)>();
        for (var from = 0; from < CellCount; from++)
        {
            if (state.Cells[from] != playerId)
                continue;
            foreach (var to in OrthogonalNeighbors(from))
            {
                if (state.Cells[to] == null)
                    list.Add((from, to));
            }
        }

        return list;
    }

    public static IReadOnlyList<(int From, int To, int Jumped)> LegalCaptures(
        YoteGameState state,
        Guid playerId,
        Guid creatorId)
    {
        _ = creatorId;
        var list = new List<(int From, int To, int Jumped)>();
        for (var from = 0; from < CellCount; from++)
        {
            if (state.Cells[from] != playerId)
                continue;
            foreach (var (dr, dc) in Ortho)
            {
                var (fr, fc) = Coord(from);
                var midR = fr + dr;
                var midC = fc + dc;
                var landR = fr + 2 * dr;
                var landC = fc + 2 * dc;
                if (!InBounds(midR, midC) || !InBounds(landR, landC))
                    continue;
                var jumped = Index(midR, midC);
                var to = Index(landR, landC);
                if (state.Cells[jumped] is { } victim
                    && victim != playerId
                    && state.Cells[to] == null)
                    list.Add((from, to, jumped));
            }
        }

        return list;
    }

    public static IReadOnlyList<int> OrthogonalNeighbors(int cell)
    {
        var (r, c) = Coord(cell);
        var list = new List<int>(4);
        foreach (var (dr, dc) in Ortho)
        {
            var nr = r + dr;
            var nc = c + dc;
            if (InBounds(nr, nc))
                list.Add(Index(nr, nc));
        }

        return list;
    }

    public static bool IsOrthogonalStep(int from, int to)
    {
        var (fr, fc) = Coord(from);
        var (tr, tc) = Coord(to);
        return Math.Abs(fr - tr) + Math.Abs(fc - tc) == 1;
    }

    public static bool IsOrthogonalJump(int from, int to, out int jumped)
    {
        jumped = -1;
        var (fr, fc) = Coord(from);
        var (tr, tc) = Coord(to);
        var dr = tr - fr;
        var dc = tc - fc;
        if (Math.Abs(dr) + Math.Abs(dc) != 2)
            return false;
        if (dr != 0 && dc != 0)
            return false;
        var midR = fr + dr / 2;
        var midC = fc + dc / 2;
        jumped = Index(midR, midC);
        return true;
    }

    public static string FormatPlaceMarker(int to) => $"Y:place:{to}";

    public static string FormatSlideMarker(int from, int to) => $"Y:slide:{from}:{to}";

    public static string FormatCaptureMarker(int from, int to, int jumped, int? extra) =>
        extra == null ? $"Y:cap:{from}:{to}:{jumped}" : $"Y:cap:{from}:{to}:{jumped}:{extra}";

    private static bool TryPlace(
        YoteGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        int toCell,
        out string? errorCode)
    {
        errorCode = null;
        if (InHand(state, playerId, creatorId) <= 0 || state.Cells[toCell] != null)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        state.Cells[toCell] = playerId;
        if (playerId == creatorId)
            state.CreatorInHand--;
        else
            state.OpponentInHand--;

        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    private static bool TrySlide(
        YoteGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        int fromCell,
        int toCell,
        out string? errorCode)
    {
        errorCode = null;
        if (state.Cells[fromCell] != playerId || state.Cells[toCell] != null)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        state.Cells[fromCell] = null;
        state.Cells[toCell] = playerId;
        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    private static bool TryCapture(
        YoteGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        int fromCell,
        int toCell,
        int jumped,
        int? extraRemoveCell,
        out string? errorCode)
    {
        errorCode = null;
        if (state.Cells[fromCell] != playerId
            || state.Cells[toCell] != null
            || state.Cells[jumped] is not { } victim
            || victim == playerId)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        var remainingExtras = new List<int>();
        for (var i = 0; i < CellCount; i++)
        {
            if (i != jumped && state.Cells[i] == victim)
                remainingExtras.Add(i);
        }

        if (remainingExtras.Count > 0)
        {
            if (extraRemoveCell == null
                || !remainingExtras.Contains(extraRemoveCell.Value))
            {
                errorCode = GameMoveErrorCodes.InvalidMove;
                return false;
            }
        }
        else if (extraRemoveCell != null)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        state.Cells[fromCell] = null;
        state.Cells[jumped] = null;
        state.Cells[toCell] = playerId;
        if (extraRemoveCell != null)
            state.Cells[extraRemoveCell.Value] = null;

        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    private static bool InBounds(int row, int col) =>
        row is >= 0 and < Rows && col is >= 0 and < Cols;
}
