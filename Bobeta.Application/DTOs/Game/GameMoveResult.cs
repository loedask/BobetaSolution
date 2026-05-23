namespace Bobeta.Application.DTOs.Game;

/// <summary>Outcome of a play-card or void-follow (take) attempt.</summary>
public sealed class GameMoveResult
{
    public GameStateDto? State { get; init; }
    public string? ErrorCode { get; init; }

    public bool IsSuccess => State != null && string.IsNullOrEmpty(ErrorCode);

    public static GameMoveResult Ok(GameStateDto state) => new() { State = state };

    public static GameMoveResult Fail(string errorCode) => new() { ErrorCode = errorCode };
}
