using Bobeta.Application.Common;

namespace Bobeta.Application.Games.Kopo;

/// <summary>Authoritative Kopo (10×10 flying kings) move generation and validation.</summary>
public static class KopoRules
{
    public static KopoGameState CreateInitial(Guid creatorId, Guid opponentId, Guid firstTurn)
    {
        var state = new KopoGameState { CurrentTurnPlayerId = firstTurn, NextPieceId = 1 };
        for (var r = 0; r < 4; r++)
            PlaceRowPieces(state, r, opponentId);
        for (var r = 6; r < KopoBoard.Size; r++)
            PlaceRowPieces(state, r, creatorId);
        return state;
    }

    public static bool TryApplyMove(
        KopoGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        IReadOnlyList<(int Row, int Col)> path,
        out string? errorCode)
    {
        errorCode = null;
        if (state.CurrentTurnPlayerId != playerId)
        {
            errorCode = GameMoveErrorCodes.NotYourTurn;
            return false;
        }

        if (path.Count < 2)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        var startPiece = PieceAt(state, path[0].Row, path[0].Col);
        if (startPiece == null || startPiece.OwnerId != playerId)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        if (state.ChainPieceId is { } chainId && startPiece.Id != chainId)
        {
            errorCode = GameMoveErrorCodes.MustContinueChain;
            return false;
        }

        var maxSequences = EnumerateCaptureSequences(state, creatorId, playerId, state.ChainPieceId);
        var pathIsCapture = PathImpliesCapture(path, state, creatorId, playerId);

        if (maxSequences.Count > 0)
        {
            if (!pathIsCapture)
            {
                errorCode = GameMoveErrorCodes.MustCapture;
                return false;
            }

            var pathCaptures = path.Count - 1;
            var maxCaptures = maxSequences.Max(s => s.Count - 1);
            if (pathCaptures < maxCaptures)
            {
                errorCode = GameMoveErrorCodes.MustMaxCapture;
                return false;
            }
        }
        else if (pathIsCapture)
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        var working = Clone(state);
        if (!SimulatePath(working, creatorId, playerId, path, out var movingId, out var canContinue))
        {
            errorCode = GameMoveErrorCodes.InvalidMove;
            return false;
        }

        var moving = working.Pieces.First(p => p.Id == movingId);
        if (!canContinue)
            MaybePromote(moving, creatorId);

        state.Pieces = working.Pieces;
        if (canContinue)
        {
            state.ChainPieceId = movingId;
            state.CurrentTurnPlayerId = playerId;
            return true;
        }

        state.ChainPieceId = null;
        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    public static bool HasAnyLegalMove(KopoGameState state, Guid creatorId, Guid playerId)
    {
        if (state.ChainPieceId is { } chainId)
            return EnumerateCaptureSequences(state, creatorId, playerId, chainId).Count > 0;

        if (EnumerateCaptureSequences(state, creatorId, playerId, null).Count > 0)
            return true;

        foreach (var p in state.Pieces.Where(x => x.OwnerId == playerId))
        {
            if (GetQuietMoves(state, p, creatorId).Count > 0)
                return true;
        }

        return false;
    }

    public static (Guid? winnerId, Guid? loserId, bool isDraw) CheckOutcome(
        KopoGameState state,
        Guid creatorId,
        Guid opponentId)
    {
        var creatorPieces = state.Pieces.Count(p => p.OwnerId == creatorId);
        var opponentPieces = state.Pieces.Count(p => p.OwnerId == opponentId);
        if (creatorPieces == 0)
            return (opponentId, creatorId, false);
        if (opponentPieces == 0)
            return (creatorId, opponentId, false);

        var creatorCanMove = HasAnyLegalMove(state, creatorId, creatorId);
        var opponentCanMove = HasAnyLegalMove(state, creatorId, opponentId);
        if (!creatorCanMove && !opponentCanMove)
            return (null, null, true);
        if (!creatorCanMove)
            return (opponentId, creatorId, false);
        if (!opponentCanMove)
            return (creatorId, opponentId, false);
        return (null, null, false);
    }

    private static void PlaceRowPieces(KopoGameState state, int row, Guid ownerId)
    {
        for (var c = 0; c < KopoBoard.Size; c++)
        {
            if (!KopoBoard.IsPlayable(row, c))
                continue;
            state.Pieces.Add(new KopoPiece
            {
                Id = state.NextPieceId++,
                OwnerId = ownerId,
                Row = row,
                Col = c
            });
        }
    }

    private static bool PathImpliesCapture(
        IReadOnlyList<(int Row, int Col)> path,
        KopoGameState state,
        Guid creatorId,
        Guid playerId) =>
        path.Count > 2 || (path.Count == 2 && IsCaptureStep(state, path[0], path[1], creatorId, playerId));

    private static bool SimulatePath(
        KopoGameState state,
        Guid creatorId,
        Guid playerId,
        IReadOnlyList<(int Row, int Col)> path,
        out int movingPieceId,
        out bool canContinueChain)
    {
        movingPieceId = 0;
        canContinueChain = false;
        var piece = PieceAt(state, path[0].Row, path[0].Col);
        if (piece == null || piece.OwnerId != playerId)
            return false;
        movingPieceId = piece.Id;

        for (var i = 1; i < path.Count; i++)
        {
            var from = (piece.Row, piece.Col);
            var to = path[i];
            if (!IsValidStep(state, piece, from, to, creatorId, out var jumpedId))
                return false;
            if (jumpedId.HasValue)
                state.Pieces.RemoveAll(p => p.Id == jumpedId.Value);
            piece.Row = to.Row;
            piece.Col = to.Col;
        }

        canContinueChain = GetCaptureContinuations(state, piece, creatorId).Count > 0;
        return true;
    }

    private static bool IsValidStep(
        KopoGameState state,
        KopoPiece piece,
        (int Row, int Col) from,
        (int Row, int Col) to,
        Guid creatorId,
        out int? jumpedPieceId)
    {
        jumpedPieceId = null;
        if (!KopoBoard.IsPlayable(to.Row, to.Col) || !IsEmpty(state, to.Row, to.Col))
            return false;

        var dr = to.Row - from.Row;
        var dc = to.Col - from.Col;
        if (Math.Abs(dr) != Math.Abs(dc) || dr == 0)
            return false;

        if (piece.IsKing)
            return IsValidKingStep(state, piece, from, to, creatorId, out jumpedPieceId);

        if (Math.Abs(dr) == 1)
        {
            if (!KopoBoard.IsForwardMove(from.Row, to.Row, piece.OwnerId, creatorId))
                return false;
            jumpedPieceId = null;
            return true;
        }

        if (Math.Abs(dr) != 2)
            return false;

        var midR = from.Row + dr / 2;
        var midC = from.Col + dc / 2;
        var jumped = PieceAt(state, midR, midC);
        if (jumped == null || jumped.OwnerId == piece.OwnerId)
            return false;
        jumpedPieceId = jumped.Id;
        return true;
    }

    private static bool IsValidKingStep(
        KopoGameState state,
        KopoPiece piece,
        (int Row, int Col) from,
        (int Row, int Col) to,
        Guid creatorId,
        out int? jumpedPieceId)
    {
        jumpedPieceId = null;
        var dr = Math.Sign(to.Row - from.Row);
        var dc = Math.Sign(to.Col - from.Col);
        var r = from.Row + dr;
        var c = from.Col + dc;
        int? enemyId = null;
        var enemyCount = 0;

        while (r != to.Row || c != to.Col)
        {
            if (!KopoBoard.IsPlayable(r, c))
                return false;
            var occupant = PieceAt(state, r, c);
            if (occupant != null)
            {
                if (occupant.OwnerId == piece.OwnerId || enemyCount > 0)
                    return false;
                enemyId = occupant.Id;
                enemyCount = 1;
            }
            else if (enemyCount == 1)
            {
                // landing beyond enemy — keep scanning to (to)
            }

            r += dr;
            c += dc;
        }

        if (enemyCount == 1)
        {
            jumpedPieceId = enemyId;
            return true;
        }

        // quiet slide
        return IsEmptyAlongRay(state, from, to);
    }

    private static bool IsEmptyAlongRay(KopoGameState state, (int Row, int Col) from, (int Row, int Col) to)
    {
        var dr = Math.Sign(to.Row - from.Row);
        var dc = Math.Sign(to.Col - from.Col);
        var r = from.Row + dr;
        var c = from.Col + dc;
        while (r != to.Row || c != to.Col)
        {
            if (!KopoBoard.IsPlayable(r, c) || PieceAt(state, r, c) != null)
                return false;
            r += dr;
            c += dc;
        }

        return true;
    }

    private static bool IsCaptureStep(
        KopoGameState state,
        (int Row, int Col) from,
        (int Row, int Col) to,
        Guid creatorId,
        Guid playerId)
    {
        var piece = PieceAt(state, from.Row, from.Col);
        return piece != null && IsValidStep(state, piece, from, to, creatorId, out var jumped) && jumped.HasValue;
    }

    private static void MaybePromote(KopoPiece piece, Guid creatorId)
    {
        if (piece.IsKing)
            return;
        if (piece.Row == KopoBoard.KingRow(piece.OwnerId, creatorId))
            piece.IsKing = true;
    }

    private static List<List<(int Row, int Col)>> EnumerateCaptureSequences(
        KopoGameState state,
        Guid creatorId,
        Guid playerId,
        int? onlyPieceId)
    {
        var results = new List<List<(int Row, int Col)>>();
        var pieces = state.Pieces.Where(p => p.OwnerId == playerId && (onlyPieceId == null || p.Id == onlyPieceId)).ToList();
        foreach (var piece in pieces)
        {
            var start = new List<(int, int)> { (piece.Row, piece.Col) };
            DfsCaptures(Clone(state), piece.Id, creatorId, start, results);
        }

        return results;
    }

    private static void DfsCaptures(
        KopoGameState state,
        int pieceId,
        Guid creatorId,
        List<(int Row, int Col)> path,
        List<List<(int Row, int Col)>> results)
    {
        var piece = state.Pieces.First(p => p.Id == pieceId);
        var continuations = GetCaptureContinuations(state, piece, creatorId);
        if (continuations.Count == 0)
        {
            if (path.Count > 1)
                results.Add(new List<(int, int)>(path));
            return;
        }

        foreach (var land in continuations)
        {
            var clone = Clone(state);
            var moving = clone.Pieces.First(p => p.Id == pieceId);
            var from = (moving.Row, moving.Col);
            if (!IsValidStep(clone, moving, from, land, creatorId, out var jumped) || !jumped.HasValue)
                continue;
            clone.Pieces.RemoveAll(p => p.Id == jumped.Value);
            moving.Row = land.Row;
            moving.Col = land.Col;
            var nextPath = new List<(int, int)>(path) { land };
            DfsCaptures(clone, pieceId, creatorId, nextPath, results);
        }
    }

    private static List<(int Row, int Col)> GetCaptureContinuations(KopoGameState state, KopoPiece piece, Guid creatorId)
    {
        var lands = new List<(int, int)>();
        for (var r = 0; r < KopoBoard.Size; r++)
        for (var c = 0; c < KopoBoard.Size; c++)
        {
            if (!KopoBoard.IsPlayable(r, c))
                continue;
            var to = (r, c);
            if (IsValidStep(state, piece, (piece.Row, piece.Col), to, creatorId, out var jumped) && jumped.HasValue)
                lands.Add(to);
        }

        return lands;
    }

    private static List<(int Row, int Col)> GetQuietMoves(KopoGameState state, KopoPiece piece, Guid creatorId)
    {
        var moves = new List<(int, int)>();
        for (var r = 0; r < KopoBoard.Size; r++)
        for (var c = 0; c < KopoBoard.Size; c++)
        {
            var to = (r, c);
            if (IsValidStep(state, piece, (piece.Row, piece.Col), to, creatorId, out var jumped) && !jumped.HasValue)
                moves.Add(to);
        }

        return moves;
    }

    private static KopoPiece? PieceAt(KopoGameState state, int row, int col) =>
        state.Pieces.FirstOrDefault(p => p.Row == row && p.Col == col);

    private static bool IsEmpty(KopoGameState state, int row, int col) => PieceAt(state, row, col) == null;

    private static KopoGameState Clone(KopoGameState state) => new()
    {
        CurrentTurnPlayerId = state.CurrentTurnPlayerId,
        ChainPieceId = state.ChainPieceId,
        NextPieceId = state.NextPieceId,
        Pieces = state.Pieces.Select(p => new KopoPiece
        {
            Id = p.Id,
            OwnerId = p.OwnerId,
            Row = p.Row,
            Col = p.Col,
            IsKing = p.IsKing
        }).ToList()
    };

}
