namespace Bobeta.Application.DTOs.Game;

/// <summary>
/// Viewer-relative Nzengué board. Occupancy: 0 empty, 1 mine, 2 opponent.
/// </summary>
public record NzengueStateDto(
    int PointCount,
    int PiecesPerPlayer,
    string Phase,
    IReadOnlyList<int> Occupancy,
    int MyPiecesToPlace,
    int OpponentPiecesToPlace,
    IReadOnlyList<int> LegalPlacePoints,
    IReadOnlyList<NzengueEdgeDto> LegalMoves,
    bool CanAct);

public record NzengueEdgeDto(int From, int To);

/// <summary>Place when <see cref="FromPoint"/> is null; move when both indices are set.</summary>
public record NzengueMoveRequest(Guid SessionId, int? FromPoint, int ToPoint);
