using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;

namespace Bobeta.Client.Services;

internal static class GameStateMapper
{
    public static GameStateViewModel ToViewModel(GameStateDto dto) => new()
    {
        SessionId = dto.SessionId,
        Variant = dto.Variant,
        MyCards = dto.MyCards ?? new List<string>(),
        LastPlayedCard = dto.LastPlayedCard,
        CurrentTurnPlayerId = dto.CurrentTurnPlayerId,
        GameOver = dto.GameOver,
        WinnerPlayerId = dto.WinnerPlayerId,
        WaitingForGameStart = dto.WaitingForGameStart,
        LobbyPotAmount = (decimal)dto.LobbyPotAmount,
        OpponentDisplayName = string.IsNullOrEmpty(dto.OpponentDisplayName) ? null : dto.OpponentDisplayName,
        LastTrickWinnerPlayerId = dto.LastTrickWinnerPlayerId,
        MyRoundWins = dto.MyRoundWins,
        OpponentRoundWins = dto.OpponentRoundWins,
        MustFollowLedSuit = dto.MustFollowLedSuit,
        Kopo = dto.Kopo,
        Ngola = dto.Ngola,
        Domino = dto.Domino,
        IsDraw = dto.IsDraw
    };

    public static GameSessionViewModel ToViewModel(GameSessionDto dto) => new()
    {
        Id = dto.Id,
        CreatorPlayerId = dto.CreatorPlayerId,
        OpponentPlayerId = dto.OpponentPlayerId,
        BetAmount = (decimal)dto.BetAmount,
        Variant = dto.Variant,
        Status = dto.Status.ToString(),
        CreatedAt = dto.CreatedAt,
        StartedAt = dto.StartedAt,
        FinishedAt = dto.FinishedAt
    };
}
