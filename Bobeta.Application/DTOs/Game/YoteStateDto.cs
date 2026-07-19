namespace Bobeta.Application.DTOs.Game;

/// <summary>Viewer-relative Yoté board. Occupancy: 0 empty, 1 mine, 2 opponent.</summary>
public record YoteStateDto(
    int Rows,
    int Cols,
    int PiecesPerPlayer,
    IReadOnlyList<int> Occupancy,
    int MyInHand,
    int OpponentInHand,
    IReadOnlyList<int> LegalPlaceCells,
    IReadOnlyList<YoteEdgeDto> LegalSlides,
    IReadOnlyList<YoteCaptureDto> LegalCaptures,
    bool CanAct);

public record YoteEdgeDto(int From, int To);

public record YoteCaptureDto(int From, int To, int Jumped);

/// <summary>
/// Place when <see cref="FromCell"/> is null.
/// Slide or capture when both cells are set; for captures with leftover opponent stones,
/// <see cref="ExtraRemoveCell"/> is required.
/// </summary>
public record YoteMoveRequest(Guid SessionId, int? FromCell, int ToCell, int? ExtraRemoveCell = null);
