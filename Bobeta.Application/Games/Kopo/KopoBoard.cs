namespace Bobeta.Application.Games.Kopo;

/// <summary>10×10 Kopo board: playable squares are dark; (9,9) is light (bottom-right).</summary>
public static class KopoBoard
{
    public const int Size = 10;

    /// <summary>Dark / playable cell.</summary>
    public static bool IsPlayable(int row, int col) =>
        row is >= 0 and < Size && col is >= 0 and < Size && (row + col) % 2 == 1;

    public static int ForwardRowDelta(Guid ownerId, Guid creatorId) =>
        ownerId == creatorId ? -1 : 1;

    public static int KingRow(Guid ownerId, Guid creatorId) =>
        ownerId == creatorId ? 0 : Size - 1;

    public static bool IsForwardMove(int fromRow, int toRow, Guid ownerId, Guid creatorId) =>
        (toRow - fromRow) * ForwardRowDelta(ownerId, creatorId) > 0;
}
