namespace Bobeta.Application.DTOs.Game;

public record NgolaStateDto(
    int PitsPerPlayer,
    IReadOnlyList<int> MyPits,
    IReadOnlyList<int> OpponentPits,
    int MyScore,
    int OpponentScore);

public record NgolaMoveRequest(Guid SessionId, int PitIndex);
