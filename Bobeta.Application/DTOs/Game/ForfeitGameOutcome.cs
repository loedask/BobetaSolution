using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Game;

/// <summary>Outcome of a player forfeiting an in-progress match.</summary>
public sealed record ForfeitGameOutcome(
    Guid SessionId,
    Guid WinnerPlayerId,
    Guid LoserPlayerId,
    decimal WinnerAmount,
    GameVariant Variant);
