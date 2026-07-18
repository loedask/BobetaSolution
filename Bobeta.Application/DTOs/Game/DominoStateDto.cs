namespace Bobeta.Application.DTOs.Game;

public record DominoStateDto(
    IReadOnlyList<string> MyHand,
    int OpponentHandCount,
    int BoneyardCount,
    IReadOnlyList<string> Chain,
    int? LeftEnd,
    int? RightEnd,
    bool IsOpening,
    string? OpeningTile,
    bool MustDraw,
    bool MustPass);

/// <param name="Action">play | draw | pass</param>
/// <param name="High">Higher pip when Action is play.</param>
/// <param name="Low">Lower pip when Action is play.</param>
/// <param name="End">left | right when attaching after the opening tile.</param>
public record DominoMoveRequest(
    Guid SessionId,
    string Action,
    int? High = null,
    int? Low = null,
    string? End = null);
