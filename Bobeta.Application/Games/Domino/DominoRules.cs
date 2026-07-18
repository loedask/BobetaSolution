using Bobeta.Application.Common;

namespace Bobeta.Application.Games.Domino;

/// <summary>Authoritative rules for 1v1 Domino (double-six draw / avec pioche).</summary>
public static class DominoRules
{
    public const int MaxPip = 6;
    public const int HandSize = 7;
    public const string ActionPlay = "play";
    public const string ActionDraw = "draw";
    public const string ActionPass = "pass";
    public const string EndLeft = "left";
    public const string EndRight = "right";

    public static DominoGameState CreateInitial(Guid sessionId, Guid creatorId, Guid opponentId)
    {
        var tiles = BuildDoubleSixSet();
        Shuffle(tiles, sessionId.GetHashCode() ^ 0x44_4F_4D_49);

        var creatorHand = tiles.GetRange(0, HandSize);
        var opponentHand = tiles.GetRange(HandSize, HandSize);
        var boneyard = tiles.GetRange(HandSize * 2, tiles.Count - HandSize * 2);

        var (starterId, openingTile) = ResolveOpening(creatorId, opponentId, creatorHand, opponentHand);
        return new DominoGameState
        {
            CreatorHand = creatorHand,
            OpponentHand = opponentHand,
            Boneyard = boneyard,
            CurrentTurnPlayerId = starterId,
            IsOpening = true,
            OpeningTile = openingTile
        };
    }

    public static bool TryApplyAction(
        DominoGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        string action,
        int? high,
        int? low,
        string? end,
        out string? errorCode)
    {
        errorCode = null;
        if (state.CurrentTurnPlayerId != playerId)
        {
            errorCode = GameMoveErrorCodes.NotYourTurn;
            return false;
        }

        var normalized = action.Trim().ToLowerInvariant();
        return normalized switch
        {
            ActionPlay => TryPlay(state, creatorId, opponentId, playerId, high, low, end, out errorCode),
            ActionDraw => TryDraw(state, creatorId, playerId, out errorCode),
            ActionPass => TryPass(state, creatorId, opponentId, playerId, out errorCode),
            _ => Fail(out errorCode, GameMoveErrorCodes.InvalidMove)
        };
    }

    public static bool HasLegalPlay(DominoGameState state, Guid playerId, Guid creatorId)
    {
        var hand = HandOf(state, playerId, creatorId);
        if (state.IsOpening)
            return hand.Contains(state.OpeningTile!);

        return hand.Any(tile =>
            CanAttach(tile, state.LeftEnd!.Value, out _) || CanAttach(tile, state.RightEnd!.Value, out _));
    }

    public static bool MustDraw(DominoGameState state, Guid playerId, Guid creatorId) =>
        !HasLegalPlay(state, playerId, creatorId) && state.Boneyard.Count > 0;

    public static bool MustPass(DominoGameState state, Guid playerId, Guid creatorId) =>
        !HasLegalPlay(state, playerId, creatorId) && state.Boneyard.Count == 0;

    public static int PipCount(IEnumerable<string> hand) =>
        hand.Sum(tile =>
        {
            var (a, b) = Parse(tile);
            return a + b;
        });

    /// <summary>
    /// After a successful action, detect win (empty hand), mutual block (both pass), or continue.
    /// </summary>
    public static (Guid? WinnerId, Guid? LoserId, bool IsDraw) EvaluateAfterAction(
        DominoGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid actingPlayerId,
        string action)
    {
        var hand = HandOf(state, actingPlayerId, creatorId);
        if (hand.Count == 0)
        {
            var loser = actingPlayerId == creatorId ? opponentId : creatorId;
            return (actingPlayerId, loser, false);
        }

        if (!string.Equals(action, ActionPass, StringComparison.OrdinalIgnoreCase))
            return (null, null, false);

        var otherId = actingPlayerId == creatorId ? opponentId : creatorId;
        if (HasLegalPlay(state, otherId, creatorId) || state.Boneyard.Count > 0)
            return (null, null, false);

        // Both blocked with empty boneyard: lowest remaining pips wins.
        var creatorPips = PipCount(state.CreatorHand);
        var opponentPips = PipCount(state.OpponentHand);
        if (creatorPips == opponentPips)
            return (null, null, true);
        return creatorPips < opponentPips
            ? (creatorId, opponentId, false)
            : (opponentId, creatorId, false);
    }

    public static string NormalizeTile(int a, int b) =>
        a >= b ? $"{a}-{b}" : $"{b}-{a}";

    public static (int High, int Low) Parse(string tile)
    {
        var parts = tile.Split('-');
        var a = int.Parse(parts[0]);
        var b = int.Parse(parts[1]);
        return a >= b ? (a, b) : (b, a);
    }

    private static bool TryPlay(
        DominoGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        int? high,
        int? low,
        string? end,
        out string? errorCode)
    {
        errorCode = null;
        if (high is null or < 0 or > MaxPip || low is null or < 0 or > MaxPip)
            return Fail(out errorCode, GameMoveErrorCodes.InvalidMove);

        if (MustDraw(state, playerId, creatorId) || MustPass(state, playerId, creatorId))
            return Fail(out errorCode, GameMoveErrorCodes.InvalidMove);

        var tile = NormalizeTile(high.Value, low.Value);
        var hand = HandOf(state, playerId, creatorId);
        if (!hand.Contains(tile))
            return Fail(out errorCode, GameMoveErrorCodes.InvalidMove);

        if (state.IsOpening)
        {
            if (tile != state.OpeningTile)
                return Fail(out errorCode, GameMoveErrorCodes.InvalidMove);

            hand.Remove(tile);
            state.Chain.Add(tile);
            var (a, b) = Parse(tile);
            state.LeftEnd = a;
            state.RightEnd = b;
            state.IsOpening = false;
            state.OpeningTile = null;
            state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
            return true;
        }

        var endKey = (end ?? string.Empty).Trim().ToLowerInvariant();
        if (endKey is not (EndLeft or EndRight))
            return Fail(out errorCode, GameMoveErrorCodes.InvalidMove);

        var attachValue = endKey == EndLeft ? state.LeftEnd!.Value : state.RightEnd!.Value;
        if (!CanAttach(tile, attachValue, out var exposed))
            return Fail(out errorCode, GameMoveErrorCodes.InvalidMove);

        hand.Remove(tile);
        if (endKey == EndLeft)
        {
            state.Chain.Insert(0, tile);
            state.LeftEnd = exposed;
        }
        else
        {
            state.Chain.Add(tile);
            state.RightEnd = exposed;
        }

        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    private static bool TryDraw(
        DominoGameState state,
        Guid creatorId,
        Guid playerId,
        out string? errorCode)
    {
        errorCode = null;
        if (!MustDraw(state, playerId, creatorId))
            return Fail(out errorCode, GameMoveErrorCodes.InvalidMove);

        var drawn = state.Boneyard[^1];
        state.Boneyard.RemoveAt(state.Boneyard.Count - 1);
        HandOf(state, playerId, creatorId).Add(drawn);
        // Same player keeps the turn after drawing.
        return true;
    }

    private static bool TryPass(
        DominoGameState state,
        Guid creatorId,
        Guid opponentId,
        Guid playerId,
        out string? errorCode)
    {
        errorCode = null;
        if (!MustPass(state, playerId, creatorId))
            return Fail(out errorCode, GameMoveErrorCodes.InvalidMove);

        state.CurrentTurnPlayerId = playerId == creatorId ? opponentId : creatorId;
        return true;
    }

    private static bool CanAttach(string tile, int endValue, out int exposed)
    {
        var (high, low) = Parse(tile);
        if (high == endValue)
        {
            exposed = low;
            return true;
        }

        if (low == endValue)
        {
            exposed = high;
            return true;
        }

        exposed = 0;
        return false;
    }

    private static List<string> HandOf(DominoGameState state, Guid playerId, Guid creatorId) =>
        playerId == creatorId ? state.CreatorHand : state.OpponentHand;

    private static (Guid StarterId, string OpeningTile) ResolveOpening(
        Guid creatorId,
        Guid opponentId,
        List<string> creatorHand,
        List<string> opponentHand)
    {
        var creatorBest = BestOpeningTile(creatorHand);
        var opponentBest = BestOpeningTile(opponentHand);
        var cmp = CompareTiles(creatorBest, opponentBest);
        if (cmp > 0)
            return (creatorId, creatorBest);
        if (cmp < 0)
            return (opponentId, opponentBest);
        return (creatorId, creatorBest);
    }

    private static string BestOpeningTile(IReadOnlyList<string> hand)
    {
        var doubles = hand.Where(t =>
        {
            var (h, l) = Parse(t);
            return h == l;
        }).ToList();
        if (doubles.Count > 0)
            return doubles.OrderByDescending(t => Parse(t).High).First();
        return hand.OrderByDescending(t => Parse(t).High)
            .ThenByDescending(t => Parse(t).Low)
            .First();
    }

    private static int CompareTiles(string a, string b)
    {
        var (ah, al) = Parse(a);
        var (bh, bl) = Parse(b);
        var aDouble = ah == al;
        var bDouble = bh == bl;
        if (aDouble != bDouble)
            return aDouble ? 1 : -1;
        if (ah != bh)
            return ah.CompareTo(bh);
        return al.CompareTo(bl);
    }

    private static List<string> BuildDoubleSixSet()
    {
        var tiles = new List<string>(28);
        for (var high = 0; high <= MaxPip; high++)
        {
            for (var low = 0; low <= high; low++)
                tiles.Add(NormalizeTile(high, low));
        }

        return tiles;
    }

    private static void Shuffle(List<string> tiles, int seed)
    {
        var rng = new Random(seed);
        for (var i = tiles.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }
    }

    private static bool Fail(out string? errorCode, string code)
    {
        errorCode = code;
        return false;
    }
}
