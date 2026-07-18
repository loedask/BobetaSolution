namespace Bobeta.Client.Presentation;

/// <summary>Matches <see cref="Bobeta.Application.Common.GameSessionErrorCodes"/> from the API.</summary>
public static class GameSessionClientCodes
{
    public const string TooManyLiveGames = "too_many_live_games";

    /// <summary>Must stay in sync with <c>GameSessionService.MaxConcurrentInProgressGames</c>.</summary>
    public const int MaxConcurrentInProgressGames = 3;
}
